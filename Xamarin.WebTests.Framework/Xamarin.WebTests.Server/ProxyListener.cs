﻿﻿//
// ProxyListener.cs
//
// Author:
//       Martin Baulig <martin.baulig@xamarin.com>
//
// Copyright (c) 2014 Xamarin Inc. (http://www.xamarin.com)
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
using System;
using System.IO;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Xamarin.AsyncTests;
using Xamarin.AsyncTests.Portable;

namespace Xamarin.WebTests.Server
{
	using HttpFramework;

	class ProxyListener : BuiltinSocketListener
	{
		public AuthenticationManager AuthenticationManager {
			get;
		}

		new public BuiltinProxyServer Server {
			get { return (BuiltinProxyServer)base.Server; }
		}

		public ProxyListener (TestContext ctx, BuiltinProxyServer server)
			: base (ctx, server)
		{
			if (Server.AuthenticationType != AuthenticationType.None)
				AuthenticationManager = new AuthenticationManager (Server.AuthenticationType, true);
		}

		protected override async Task<bool> HandleConnection (HttpConnection connection, CancellationToken cancellationToken)
		{
			var request = await connection.ReadRequest (TestContext, cancellationToken).ConfigureAwait (false);

			cancellationToken.ThrowIfCancellationRequested ();
			var remoteAddress = connection.RemoteEndPoint.Address;
			request.AddHeader ("X-Forwarded-For", remoteAddress);

			if (AuthenticationManager != null) {
				AuthenticationState state;
				var response = AuthenticationManager.HandleAuthentication (TestContext, connection, request, out state);
				if (response != null) {
					await connection.WriteResponse (TestContext, response, cancellationToken);
					return false;
				}

				// HACK: Mono rewrites chunked requests into non-chunked.
				request.AddHeader ("X-Mono-Redirected", "true");
			}

			if (request.Method.Equals ("CONNECT")) {
				await CreateTunnel (connection, ((SocketConnection)connection).Stream, request, cancellationToken);
				return false;
			}

			var targetSocket = new Socket (AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
			targetSocket.Connect (Server.Target.Uri.Host, Server.Target.Uri.Port);

			using (var targetConnection = new SocketConnection (Server, targetSocket)) {
				await targetConnection.Initialize (TestContext, cancellationToken);

				var copyResponseTask = CopyResponse (connection, targetConnection, cancellationToken);

				cancellationToken.ThrowIfCancellationRequested ();
				await targetConnection.WriteRequest (TestContext, request, cancellationToken);

				cancellationToken.ThrowIfCancellationRequested ();
				await copyResponseTask;
			}

			targetSocket.Close ();
			return false;
		}

		async Task CopyResponse (HttpConnection connection, HttpConnection targetConnection, CancellationToken cancellationToken)
		{
			await Task.Yield ();

			cancellationToken.ThrowIfCancellationRequested ();
			var response = await targetConnection.ReadResponse (TestContext, cancellationToken).ConfigureAwait (false);
			response.SetHeader ("Connection", "close");
			response.SetHeader ("Proxy-Connection", "close");

			cancellationToken.ThrowIfCancellationRequested ();
			await connection.WriteResponse (TestContext, response, cancellationToken);
		}

		IPEndPoint GetConnectEndpoint (HttpRequest request)
		{
			var pos = request.Path.IndexOf (':');
			if (pos < 0)
				return new IPEndPoint (IPAddress.Parse (request.Path), 443);

			var address = IPAddress.Parse (request.Path.Substring (0, pos));
			var port = int.Parse (request.Path.Substring (pos + 1));
			return new IPEndPoint (address, port);
		}

		async Task CreateTunnel (
			HttpConnection connection, Stream stream,
			HttpRequest request, CancellationToken cancellationToken)
		{
			var targetEndpoint = GetConnectEndpoint (request);
			var targetSocket = new Socket (AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
			targetSocket.Connect (targetEndpoint);
			targetSocket.NoDelay = true;

			var targetStream = new NetworkStream (targetSocket, true);

			var connectionEstablished = new HttpResponse (HttpStatusCode.OK, HttpProtocol.Http10, "Connection established");
			await connectionEstablished.Write (TestContext, stream, cancellationToken).ConfigureAwait (false);

			try {
				await RunTunnel (stream, targetStream, cancellationToken);
			} catch (OperationCanceledException) {
				throw;
			} catch (Exception ex) {
				System.Diagnostics.Debug.WriteLine ("ERROR: {0}", ex);
				cancellationToken.ThrowIfCancellationRequested ();
				throw;
			} finally {
				targetSocket.Dispose ();
			}
		}

		async Task RunTunnel (Stream input, Stream output, CancellationToken cancellationToken)
		{
			await Task.Yield ();

			bool doneSending = false;
			bool doneReading = false;
			Task<bool> inputTask = null;
			Task<bool> outputTask = null;

			while (!doneReading && !doneSending) {
				cancellationToken.ThrowIfCancellationRequested ();

				TestContext.LogDebug (5, "RUN TUNNEL: {0} {1} {2} {3}",
				                      doneReading, doneSending, inputTask != null, outputTask != null);

				if (!doneReading && inputTask == null)
					inputTask = Copy (input, output, cancellationToken);
				if (!doneSending && outputTask == null)
					outputTask = Copy (output, input, cancellationToken);

				var tasks = new List<Task<bool>> ();
				if (inputTask != null)
					tasks.Add (inputTask);
				if (outputTask != null)
					tasks.Add (outputTask); 

				TestContext.LogDebug (5, "RUN TUNNEL #1: {0}", tasks.Count);
				var result = await Task.WhenAny (tasks).ConfigureAwait (false);
				TestContext.LogDebug (5, "RUN TUNNEL #2: {0} {1} {2}", result, result == inputTask, result == outputTask);

				if (result.IsCanceled) {
					TestContext.LogDebug (5, "RUN TUNNEL - CANCEL");
					throw new TaskCanceledException ();
				}
				if (result.IsFaulted) {
					TestContext.LogDebug (5, "RUN TUNNEL - ERROR: {0}", result.Exception);
					throw result.Exception;
				}

				TestContext.LogDebug (5, "RUN TUNNEL #3: {0}", result.Result);

				if (result == inputTask) {
					if (!result.Result)
						doneReading = true;
					inputTask = null;
				} else if (result == outputTask) {
					if (!result.Result)
						doneSending = true;
					outputTask = null;
				} else {
					throw new NotSupportedException ();
				}
			}
		}

		async Task<bool> Copy (Stream input, Stream output, CancellationToken cancellationToken)
		{
			var buffer = new byte[4096];
			int ret;
			try {
				ret = await input.ReadAsync (buffer, 0, buffer.Length, cancellationToken).ConfigureAwait (false);
			} catch {
				cancellationToken.ThrowIfCancellationRequested ();
				throw;
			}
			if (ret == 0) {
				try {
					output.Dispose ();
				} catch {
					;
				}
				return false;
			}

			try {
				await output.WriteAsync (buffer, 0, ret, cancellationToken);
			} catch {
				cancellationToken.ThrowIfCancellationRequested ();
				throw;
			}
			return true;
		}
	}
}
