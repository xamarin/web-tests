//
// InstrumentationListenerContext.cs
//
// Author:
//       Martin Baulig <mabaul@microsoft.com>
//
// Copyright (c) 2017 Xamarin Inc. (http://www.xamarin.com)
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
using System.Threading;
using System.Threading.Tasks;
using Xamarin.AsyncTests;

namespace Xamarin.WebTests.Server
{
	using HttpFramework;

	class InstrumentationListenerContext : ListenerContext
	{
		new public InstrumentationListener Listener => (InstrumentationListener)base.Listener;

		public InstrumentationListenerContext (Listener listener)
			: base (listener)
		{
			serverInitTask = new TaskCompletionSource<object> ();
			serverStartTask = new TaskCompletionSource<object> ();
		}

		public InstrumentationListenerContext (Listener listener, HttpConnection connection)
			: this (listener)
		{
			this.currentConnection = connection;
		}

		public override HttpConnection Connection {
			get { return currentConnection; }
		}

		HttpConnection redirectRequested;
		HttpOperation currentOperation;
		HttpConnection currentConnection;
		TaskCompletionSource<object> serverInitTask;
		TaskCompletionSource<object> serverStartTask;

		public bool StartOperation (HttpOperation operation)
		{
			return Interlocked.CompareExchange (ref currentOperation, operation, null) == null;
		}

		public override void Continue ()
		{
			currentOperation = null;
		}

		public override Task ServerInitTask => serverInitTask.Task;

		public override Task ServerStartTask => serverStartTask.Task;

		public override async Task Run (TestContext ctx, CancellationToken cancellationToken)
		{
			cancellationToken.ThrowIfCancellationRequested ();

			bool reused;
			HttpConnection connection;
			HttpOperation operation;

			lock (Listener) {
				operation = currentOperation;
				if (operation == null)
					throw new InvalidOperationException ();

				connection = currentConnection;
				if (connection == null) {
					connection = Listener.Backend.CreateConnection ();
					reused = false;
				} else {
					reused = true;
				}
			}

			var cncMe = Listener.FormatConnection (connection);
			ctx.LogDebug (2, $"{cncMe} LOOP: {reused}");

			while (true) {
				try {
					var (complete, success) = await Initialize ();
					if (!complete) {
						connection.Dispose ();
						connection = Listener.Backend.CreateConnection ();
						reused = false;
						continue;
					}
					serverInitTask.TrySetResult (success);
					if (!success) {
						connection.Dispose ();
						return;
					}
				} catch (OperationCanceledException) {
					connection.Dispose ();
					serverInitTask.TrySetCanceled ();
					throw;
				} catch (Exception ex) {
					connection.Dispose ();
					serverInitTask.TrySetException (ex);
					throw;
				}

				ctx.LogDebug (2, $"{cncMe} LOOP #1: {reused}");

				bool keepAlive;
				try {
					keepAlive = await Server.HandleConnection (
						ctx, operation, connection, cancellationToken).ConfigureAwait (false);
				} catch (Exception ex) {
					ctx.LogDebug (2, $"{cncMe} - ERROR {ex.Message}");
					connection.Dispose ();
					throw;
				}

				lock (Listener) {
					var redirect = Interlocked.Exchange (ref redirectRequested, null);
					ctx.LogDebug (2, $"{cncMe} SERVER LOOP #2: {keepAlive} {redirect?.ME}");

					if (redirect == null) {
						Listener.Continue (ctx, this, keepAlive);
						if (keepAlive)
							currentConnection = connection;
						else
							connection.Dispose ();
						return;
					}

					if (operation.HasAnyFlags (HttpOperationFlags.ClientDoesNotSendRedirect)) {
						connection.Dispose ();
						return;
					}

					reused = redirect == connection;
					if (!reused) {
						connection.Dispose ();
						connection = redirect;
					}
				}
			}

			async Task<(bool complete, bool success)> Initialize ()
			{
				if (reused) {
					if (!await ReuseConnection (ctx, connection, cancellationToken).ConfigureAwait (false))
						return (false, false);
					return (true, true);
				}

				if (!await InitConnection (ctx, operation, connection, cancellationToken).ConfigureAwait (false))
					return (true, false);
				return (true, true);
			}
		}

		async Task<bool> ReuseConnection (TestContext ctx, HttpConnection connection, CancellationToken cancellationToken)
		{
			var me = $"{FormatConnection (connection)} REUSE";
			ctx.LogDebug (2, $"{me}");

			serverStartTask.TrySetResult (null);

			cancellationToken.ThrowIfCancellationRequested ();
			var reusable = await connection.ReuseConnection (ctx, cancellationToken).ConfigureAwait (false);

			ctx.LogDebug (2, $"{me} #1: {reusable}");
			return reusable;
		}

		async Task<bool> InitConnection (TestContext ctx, HttpOperation operation,
		                                 HttpConnection connection, CancellationToken cancellationToken)
		{
			var me = $"{FormatConnection (connection)} INIT";
			ctx.LogDebug (2, $"{me}");

			cancellationToken.ThrowIfCancellationRequested ();
			var acceptTask = connection.AcceptAsync (ctx, cancellationToken);

			serverStartTask.TrySetResult (null);

			await acceptTask.ConfigureAwait (false);

			ctx.LogDebug (2, $"{me} ACCEPTED {connection.RemoteEndPoint}");

			bool haveRequest;

			cancellationToken.ThrowIfCancellationRequested ();
			try {
				await connection.Initialize (ctx, operation, cancellationToken);
				ctx.LogDebug (2, $"{me} #1 {connection.RemoteEndPoint}");

				if (operation.HasAnyFlags (HttpOperationFlags.ServerAbortsHandshake))
					throw ctx.AssertFail ("Expected server to abort handshake.");

				/*
				 * There seems to be some kind of a race condition here.
				 *
				 * When the client aborts the handshake due the a certificate validation failure,
				 * then we either receive an exception during the TLS handshake or the connection
				 * will be closed when the handshake is completed.
				 *
				 */
				haveRequest = await connection.HasRequest (cancellationToken);
				ctx.LogDebug (2, $"{me} #2 {haveRequest}");

				if (operation.HasAnyFlags (HttpOperationFlags.ClientAbortsHandshake))
					throw ctx.AssertFail ("Expected client to abort handshake.");
			} catch (Exception ex) {
				if (operation.HasAnyFlags (HttpOperationFlags.ServerAbortsHandshake, HttpOperationFlags.ClientAbortsHandshake))
					return false;
				ctx.LogDebug (2, $"{me} FAILED: {ex.Message}\n{ex}");
				throw;
			}

			if (!haveRequest) {
				ctx.LogMessage ($"{me} got empty requets!");
				throw ctx.AssertFail ("Got empty request.");
			}

			if (Listener.Server.UseSSL) {
				ctx.Assert (connection.SslStream.IsAuthenticated, "server is authenticated");
				if (operation.HasAnyFlags (HttpOperationFlags.RequireClientCertificate))
					ctx.Assert (connection.SslStream.IsMutuallyAuthenticated, "server is mutually authenticated");
			}

			ctx.LogDebug (2, $"{me} DONE");
			return true;
		}

		public override void PrepareRedirect (TestContext ctx, HttpConnection connection, bool keepAlive)
		{
			lock (Listener) {
				var me = $"{FormatConnection (connection)} PREPARE REDIRECT";
				ctx.LogDebug (5, $"{me}: {keepAlive}");
				HttpConnection next;
				if (keepAlive)
					next = connection;
				else
					next = Listener.Backend.CreateConnection ();

				if (Interlocked.CompareExchange (ref redirectRequested, next, null) != null)
					throw new InvalidOperationException ();
			}
		}

		protected override void Close ()
		{
			if (currentConnection != null) {
				currentConnection.Dispose ();
				currentConnection = null;
			}
		}
	}
}
