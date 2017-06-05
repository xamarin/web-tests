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
	using ConnectionFramework;

	public abstract class DotNetConnection : Connection
	{
		public DotNetConnection (ConnectionProvider provider, ConnectionParameters parameters)
			: base (provider, parameters)
		{
		}

		Socket socket;
		Socket accepted;
		Socket innerSocket;
		Stream innerStream;
		IConnectionInstrumentation instrumentation;
		TaskCompletionSource<SslStream> tcs;
		int started;
		int shutdown;
		int closed;
		Exception destroyed;

		SslStream sslStream;

		public override Stream Stream {
			get { return sslStream; }
		}

		public override SslStream SslStream {
			get { return sslStream; }
		}

		protected abstract bool IsServer {
			get;
		}

		public bool HasFlag (SslStreamFlags flags)
		{
			return (Parameters.SslStreamFlags & flags) != 0;
		}

		protected abstract Task Start (TestContext ctx, SslStream sslStream, CancellationToken cancellationToken);

		void CreateSslStream (TestContext ctx)
		{
			if (instrumentation != null) {
				if (IsServer)
					innerStream = instrumentation.CreateServerStream (ctx, this, innerSocket);
				else
					innerStream = instrumentation.CreateClientStream (ctx, this, innerSocket);
				if (innerStream == null)
					innerStream = new NetworkStream (innerSocket, true);
			} else {
				innerStream = new NetworkStream (innerSocket, true);
			}

			sslStream = Provider.SslStreamProvider.CreateSslStream (ctx, innerStream, Parameters, IsServer);
		}

		public sealed override Task Start (TestContext ctx, IConnectionInstrumentation instrumentation, CancellationToken cancellationToken)
		{
			if (destroyed != null)
				throw destroyed;
			if (Interlocked.CompareExchange (ref started, 1, 0) != 0)
				throw new InvalidOperationException ("Duplicated call to Start().");

			this.instrumentation = instrumentation;

			if (IsServer)
				StartServer (ctx, cancellationToken);
			else
				StartClient (ctx, cancellationToken);
			return FinishedTask;
		}

		void StartServer (TestContext ctx, CancellationToken cancellationToken)
		{
			socket = new Socket (AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
			socket.Bind (EndPoint);
			socket.Listen (1);

			ctx.LogMessage ("Listening at {0}.", EndPoint);

			tcs = new TaskCompletionSource<SslStream> ();

			socket.BeginAccept (async ar => {
				try {
					accepted = socket.EndAccept (ar);
					cancellationToken.ThrowIfCancellationRequested ();
					ctx.LogMessage ("Accepted connection from {0}.", accepted.RemoteEndPoint);
					innerSocket = accepted;
					await Handshake (ctx, cancellationToken).ConfigureAwait (false);
					tcs.SetResult (sslStream);
				} catch (Exception ex) {
					tcs.SetException (ex);
				}
			}, null);
		}

		void StartClient (TestContext ctx, CancellationToken cancellationToken)
		{
			socket = new Socket (AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

			ctx.LogMessage ("Connecting to {0}.", EndPoint);

			tcs = new TaskCompletionSource<SslStream> ();

			socket.BeginConnect (EndPoint, async ar => {
				try {
					socket.EndConnect (ar);
					cancellationToken.ThrowIfCancellationRequested ();
					innerSocket = socket;
					await Handshake (ctx, cancellationToken).ConfigureAwait (false);
					tcs.SetResult (sslStream);
				} catch (Exception ex) {
					tcs.SetException (ex);
				}
			}, null);
		}

		async Task Handshake (TestContext ctx, CancellationToken cancellationToken)
		{
			cancellationToken.ThrowIfCancellationRequested ();

			CreateSslStream (ctx);

			if (instrumentation != null) {
				Task<bool> task;
				if (IsServer)
					task = instrumentation.ServerHandshake (ctx, TheHandshake, this);
				else
					task = instrumentation.ClientHandshake (ctx, TheHandshake, this);
				if (await task.ConfigureAwait (false))
					return;
			}

			cancellationToken.ThrowIfCancellationRequested ();

			await TheHandshake ().ConfigureAwait (false);

			Task TheHandshake ()
			{
				return Start (ctx, sslStream, cancellationToken);
			}
		}

		public sealed override Task WaitForConnection (TestContext ctx, CancellationToken cancellationToken)
		{
			return tcs.Task;
		}

		public sealed override async Task Shutdown (TestContext ctx, CancellationToken cancellationToken)
		{
			if (destroyed != null)
				throw destroyed;
			if (closed != 0 || Interlocked.CompareExchange (ref shutdown, 1, 0) != 0)
				throw new InvalidOperationException ("Cannot call Shutdown() after the connection has been closed.");

			if (SupportsCleanShutdown)
				await sslStream.ShutdownAsync ().ConfigureAwait (false);
		}

		public async Task Restart (TestContext ctx, CancellationToken cancellationToken)
		{
			if (destroyed != null)
				throw destroyed;
			if (closed == 0)
				throw new InvalidOperationException ("Cannot restart while having an active connection.");

			cancellationToken.ThrowIfCancellationRequested ();
			await Handshake (ctx, cancellationToken).ConfigureAwait (false);
		}

		public override void Close ()
		{
			if (Interlocked.CompareExchange (ref closed, 1, 0) != 0)
				return;
			if (destroyed != null)
				return;

			try {
				if (innerStream != null)
					innerStream.Dispose ();
			} catch {
				;
			} finally {
				innerStream = null;
			}
		}

		protected override void Destroy ()
		{
			var disposedEx = new ObjectDisposedException (GetType ().Name);
			if (Interlocked.CompareExchange (ref destroyed, disposedEx, null) != null)
				return;

			try {
				if (sslStream != null)
					sslStream.Dispose ();
			} catch {
				;
			} finally {
				sslStream = null;
			}
			innerSocket = accepted = socket = null;
			innerStream = null;
			instrumentation = null;
		}
	}
}

