//
// DotNetConnection.cs
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
using System.Threading;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using System.Net.Security;
using System.Diagnostics;
using System.Collections.Generic;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using Xamarin.AsyncTests;
using Xamarin.AsyncTests.Portable;

namespace Xamarin.WebTests.ConnectionFramework
{
	using Providers;

	public abstract class DotNetConnection : Connection, ICommonConnection
	{
		public DotNetConnection (ConnectionProvider provider, ConnectionParameters parameters)
			: base (GetEndPoint (parameters), parameters)
		{
			this.provider = provider;
		}

		ConnectionProvider provider;
		Socket socket;
		Socket accepted;
		TaskCompletionSource<ISslStream> tcs;

		ISslStream sslStream;

		public Stream Stream {
			get { return sslStream.AuthenticatedStream; }
		}

		public ISslStream SslStream {
			get { return sslStream; }
		}

		public override bool SupportsCleanShutdown {
			get { return false; }
		}

		public override ProtocolVersions SupportedProtocols {
			get { return provider.SupportedProtocols; }
		}

		public ProtocolVersionCode ProtocolVersion {
			get { return SslStream.ProtocolVersion; }
		}

		protected abstract bool IsServer {
			get;
		}

		protected abstract Task<ISslStream> Start (TestContext ctx, Socket socket, CancellationToken cancellationToken);

		static IPortableEndPoint GetEndPoint (ConnectionParameters parameters)
		{
			if (parameters.EndPoint != null)
				return parameters.EndPoint;

			var support = DependencyInjector.Get<IPortableEndPointSupport> ();
			return support.GetLoopbackEndpoint (4433);
		}

		IPEndPoint GetEndPoint ()
		{
			if (EndPoint != null)
				return new IPEndPoint (IPAddress.Parse (EndPoint.Address), EndPoint.Port);
			else
				return new IPEndPoint (IPAddress.Loopback, 4433);
		}

		public sealed override Task Start (TestContext ctx, CancellationToken cancellationToken)
		{
			if (IsServer)
				StartServer (ctx, cancellationToken);
			else
				StartClient (ctx, cancellationToken);
			return FinishedTask;
		}

		void StartServer (TestContext ctx, CancellationToken cancellationToken)
		{
			var endpoint = GetEndPoint ();
			socket = new Socket (AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
			socket.Bind (endpoint);
			socket.Listen (1);

			ctx.LogMessage ("Listening at {0}.", endpoint);

			tcs = new TaskCompletionSource<ISslStream> ();

			socket.BeginAccept (async ar => {
				try {
					accepted = socket.EndAccept (ar);
					cancellationToken.ThrowIfCancellationRequested ();
					sslStream = await Start (ctx, accepted, cancellationToken);
					tcs.SetResult (sslStream);
				} catch (Exception ex) {
					tcs.SetException (ex);
				}
			}, null);
		}

		void StartClient (TestContext ctx, CancellationToken cancellationToken)
		{
			var endpoint = GetEndPoint ();
			socket = new Socket (AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

			tcs = new TaskCompletionSource<ISslStream> ();

			socket.BeginConnect (endpoint, async ar => {
				try {
					socket.EndConnect (ar);
					cancellationToken.ThrowIfCancellationRequested ();
					sslStream = await Start (ctx, socket, cancellationToken);
					tcs.SetResult (sslStream);
				} catch (Exception ex) {
					tcs.SetException (ex);
				}
			}, null);
		}

		public sealed override Task WaitForConnection (TestContext ctx, CancellationToken cancellationToken)
		{
			return tcs.Task;
		}

		protected virtual Task<bool> TryCleanShutdown (bool waitForReply)
		{
			throw new NotSupportedException ("Clean shutdown not supported yet.");
		}

		public sealed override async Task<bool> Shutdown (TestContext ctx, bool attemptCleanShutdown, bool waitForReply, CancellationToken cancellationToken)
		{
			if (attemptCleanShutdown)
				attemptCleanShutdown = await TryCleanShutdown (waitForReply);

			return attemptCleanShutdown;
		}

		protected override void Stop ()
		{
			if (accepted != null) {
				try {
					accepted.Shutdown (SocketShutdown.Both);
				} catch {
					;
				}
				try {
					accepted.Dispose ();
				} catch {
					;
				}
				accepted = null;
			}
			if (socket != null) {
				try {
					socket.Shutdown (SocketShutdown.Both);
				} catch {
					;
				}
				try {
					socket.Dispose ();
				} catch {
					;
				}
				socket = null;
			}
			try {
				if (sslStream != null) {
					sslStream.AuthenticatedStream.Dispose ();
					sslStream = null;
				}
			} catch {
				;
			}
		}

	}
}

