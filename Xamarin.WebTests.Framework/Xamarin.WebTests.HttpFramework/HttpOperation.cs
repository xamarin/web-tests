//
// HttpOperation.cs
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
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Linq;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Runtime.ExceptionServices;
using Xamarin.AsyncTests;
using Xamarin.WebTests.HttpHandlers;
using Xamarin.WebTests.TestRunners;
using Xamarin.WebTests.Server;
using Xamarin.WebTests.ConnectionFramework;

namespace Xamarin.WebTests.HttpFramework
{
	public abstract class HttpOperation : IDisposable
	{
		public HttpServer Server {
			get;
		}

		public string ME {
			get;
		}

		public Handler Handler {
			get;
		}

		public HttpOperationFlags Flags {
			get;
		}

		public HttpStatusCode ExpectedStatus {
			get;
		}

		public WebExceptionStatus ExpectedError {
			get;
		}

		static int nextID;
		public readonly int ID = Interlocked.Increment (ref nextID);

		public HttpOperation (HttpServer server, string me, Handler handler, HttpOperationFlags flags,
		                      HttpStatusCode expectedStatus, WebExceptionStatus expectedError)
		{
			Server = server;
			Handler = handler;
			Flags = flags;
			ExpectedStatus = expectedStatus;
			ExpectedError = expectedError;

			ME = $"[{me}:{ID}]";

			serverInitTask = new TaskCompletionSource<bool> ();
			serverStartTask = new TaskCompletionSource<object> ();
			requestTask = new TaskCompletionSource<Request> ();
			requestDoneTask = new TaskCompletionSource<bool> ();
			cts = new CancellationTokenSource (); 
		}

		public Request Request {
			get {
				if (currentRequest == null)
					throw new InvalidOperationException ();
				return currentRequest;
			}
		}

		public ServicePoint ServicePoint {
			get {
				if (servicePoint == null)
					throw new InvalidOperationException ();
				return servicePoint;
			}
		}

		internal bool HasAnyFlags (params HttpOperationFlags[] flags)
		{
			return flags.Any (f => (Flags & f) != 0);
		}

		Request currentRequest;
		ServicePoint servicePoint;
		BuiltinListener listener;
		TaskCompletionSource<bool> serverInitTask;
		TaskCompletionSource<object> serverStartTask;
		TaskCompletionSource<Request> requestTask;
		TaskCompletionSource<bool> requestDoneTask;
		CancellationTokenSource cts;
		HttpConnection redirectRequested;
		int requestStarted;

		public bool HasRequest => currentRequest != null;

		protected abstract Request CreateRequest (TestContext ctx, Uri uri);

		protected abstract void ConfigureRequest (TestContext ctx, Uri uri, Request request);

		public async void Start (TestContext ctx, CancellationToken cancellationToken)
		{
			if (Interlocked.CompareExchange (ref requestStarted, 1, 0) != 0)
				throw new InternalErrorException ();

			var linkedCts = CancellationTokenSource.CreateLinkedTokenSource (cts.Token, cancellationToken);
			try {
				if ((Server.Flags & HttpServerFlags.NewListener) != 0)
					await RunNewListener (ctx, linkedCts.Token).ConfigureAwait (false);
				else
					await RunLegacy (ctx, linkedCts.Token).ConfigureAwait (false);
				requestDoneTask.TrySetResult (true);
			} catch (OperationCanceledException) {
				requestDoneTask.TrySetCanceled ();
			} catch (Exception ex) {
				requestDoneTask.TrySetException (ex);
			} finally {
				linkedCts.Dispose ();
			}
		}

		public Task<Request> WaitForRequest ()
		{
			return requestTask.Task;
		}

		public async Task<bool> WaitForCompletion (bool ignoreErrors = false)
		{
			try {
				await requestDoneTask.Task.ConfigureAwait (false);
				return true;
			} catch {
				if (ignoreErrors)
					return false;
				throw;
			}
		}

		async Task RunLegacy (TestContext ctx, CancellationToken cancellationToken)
		{
			ctx.LogDebug (1, $"{ME} RUN LEGACY");

			var uri = Handler.RegisterRequest (ctx, Server);
			var request = CreateRequest (ctx, uri);
			currentRequest = request;

			if (request is TraditionalRequest traditionalRequest)
				servicePoint = traditionalRequest.RequestExt.ServicePoint;

			ConfigureRequest (ctx, uri, request);

			requestTask.SetResult (request);

			ctx.LogDebug (2, $"{ME} RUN LEGACY #1: {uri} {request}");

			Response response;
			response = await Server.RunWithContext (
				ctx, (token) => RunInner (ctx, request, token), cancellationToken).ConfigureAwait (false);

			TestRunner.CheckResponse (ctx, ME, Handler, response, cancellationToken, ExpectedStatus, ExpectedError);
		}

		async Task RunNewListener (TestContext ctx, CancellationToken cancellationToken)
		{
			var me = $"{ME} RUN";
			ctx.LogDebug (1, me);

			listener = ((BuiltinHttpServer)Server).Listener;

			var uri = Handler.RegisterRequest (ctx, Server);
			var request = CreateRequest (ctx, uri);
			currentRequest = request;

			if (request is TraditionalRequest traditionalRequest)
				servicePoint = traditionalRequest.RequestExt.ServicePoint;

			ConfigureRequest (ctx, uri, request);

			requestTask.SetResult (request);

			ctx.LogDebug (2, $"{me} #1: {uri} {request}");

			var serverTask = RunServer (ctx, cancellationToken);
			await serverStartTask.Task.ConfigureAwait (false);

			ctx.LogDebug (2, $"{me} #2");

			var clientTask = RunInner (ctx, request, cancellationToken);

			bool initDone = false, serverDone = false, clientDone = false;
			while (!initDone || !serverDone || !clientDone) {
				ctx.LogDebug (2, $"{me} #3: init={initDone} server={serverDone} client={clientDone}");

				if (clientDone) {
					if (HasAnyFlags (HttpOperationFlags.AbortAfterClientExits, HttpOperationFlags.ServerAbortsHandshake,
							 HttpOperationFlags.ClientAbortsHandshake)) {
						ctx.LogDebug (2, $"{me} #3 - ABORTING");
						break;
					}
					if (!initDone) {
						ctx.LogDebug (2, $"{me} #3 - ERROR: {clientTask.Result}");
						throw new ConnectionException ($"{ME} client exited before server accepted connection.");
					}
				}

				var tasks = new List<Task> ();
				if (!initDone)
					tasks.Add (serverInitTask.Task);
				if (!serverDone)
					tasks.Add (serverTask);
				if (!clientDone)
					tasks.Add (clientTask);
				var finished = await Task.WhenAny (tasks).ConfigureAwait (false);

				string which;
				if (finished == serverInitTask.Task) {
					which = "init";
					initDone = true;
				} else if (finished == serverTask) {
					which = "server";
					serverDone = true;
				} else if (finished == clientTask) {
					which = "client";
					clientDone = true;
				} else {
					throw new InvalidOperationException ();
				}

				ctx.LogDebug (2, $"{me} #4: {which} exited - {finished.Status}");
				if (finished.Status == TaskStatus.Faulted || finished.Status == TaskStatus.Canceled) {
					if (HasAnyFlags (HttpOperationFlags.ExpectServerException) &&
					    (finished == serverTask || finished == serverInitTask.Task))
						ctx.LogDebug (2, $"{me} #4 - EXPECTED EXCEPTION {finished.Exception.GetType ()}");
					else {
						ctx.LogDebug (2, $"{me} #4 FAILED: {finished.Exception.Message}");
						throw finished.Exception;
					}
				}
			}

			var response = clientTask.Result;

			ctx.LogDebug (2, $"{me} DONE: {response}");

			TestRunner.CheckResponse (ctx, ME, Handler, response, cancellationToken, ExpectedStatus, ExpectedError);
		}

		async Task RunServer (TestContext ctx, CancellationToken cancellationToken)
		{
			var me = $"{ME} RUN SERVER";
			ctx.LogDebug (2, $"{me}");

			cancellationToken.ThrowIfCancellationRequested ();

			var reusing = !HasAnyFlags (HttpOperationFlags.DontReuseConnection);
			var (connection, reused) = listener.CreateConnection (ctx, this, reusing);

			if (reused && HasAnyFlags (HttpOperationFlags.ClientUsesNewConnection)) {
				try {
					await connection.ReadRequest (ctx, cancellationToken).ConfigureAwait (false);
					throw ctx.AssertFail ("Expected client to use a new connection.");
				} catch (OperationCanceledException) {
					throw;
				} catch (Exception ex) {
					ctx.LogDebug (2, $"{me} EXPECTED EXCEPTION: {ex.GetType ()} {ex.Message}");
				}
				connection.Dispose ();
				(connection, reused) = listener.CreateConnection (ctx, this, false);
			}

			while (true) {
				ctx.LogDebug (2, $"{me} LOOP: {connection.ME} {reused}");
				try {
					if (!reused && !await InitConnection (ctx, connection, cancellationToken).ConfigureAwait (false)) {
						serverInitTask.TrySetResult (false);
						connection.Dispose ();
						return;
					}
					if (reused && !await ReuseConnection (ctx, connection, cancellationToken).ConfigureAwait (false)) {
						connection.Dispose ();
						(connection, reused) = listener.CreateConnection (ctx, this, false);
						continue;
					}
					serverInitTask.TrySetResult (true);
				} catch (OperationCanceledException) {
					connection.Dispose ();
					serverInitTask.TrySetCanceled ();
					throw;
				} catch (Exception ex) {
					connection.Dispose ();
					serverInitTask.TrySetException (ex);
					throw;
				}

				ctx.LogDebug (2, $"{me} LOOP #1: {connection.ME} {reused}");

				bool keepAlive;
				try {
					keepAlive = await Server.HandleConnection (ctx, this, connection, cancellationToken).ConfigureAwait (false);
				} catch (Exception ex) {
					ctx.LogDebug (2, $"{me} - ERROR {ex.Message}");
					connection.Dispose ();
					throw;
				}

				lock (listener) {
					var redirect = Interlocked.Exchange (ref redirectRequested, null);
					ctx.LogDebug (2, $"{me} SERVER LOOP #2: {keepAlive} {redirect?.ME}");

					if (redirect == null) {
						connection.Continue (ctx, keepAlive);
						return;
					}

					reused = redirect == connection;
					if (!reused) {
						connection.Dispose ();
						connection = redirect;
					}
				}
			}
		}

		async Task<bool> ReuseConnection (TestContext ctx, HttpConnection connection, CancellationToken cancellationToken)
		{
			var me = $"{ME}({connection.ME}) REUSE";
			ctx.LogDebug (2, $"{me}");

			serverStartTask.TrySetResult (null);

			cancellationToken.ThrowIfCancellationRequested ();
			var reusable = await connection.ReuseConnection (ctx, cancellationToken).ConfigureAwait (false);

			ctx.LogDebug (2, $"{me} #1: {reusable}");
			return reusable;
		}

		async Task<bool> InitConnection (TestContext ctx, HttpConnection connection, CancellationToken cancellationToken)
		{
			var me = $"{ME}({connection.ME}) INIT";
			ctx.LogDebug (2, $"{me}");

			cancellationToken.ThrowIfCancellationRequested ();
			var acceptTask = connection.AcceptAsync (ctx, cancellationToken);

			serverStartTask.TrySetResult (null);

			await acceptTask.ConfigureAwait (false);

			ctx.LogDebug (2, $"{me} ACCEPTED {connection.RemoteEndPoint}");

			bool haveRequest;

			cancellationToken.ThrowIfCancellationRequested ();
			try {
				await connection.Initialize (ctx, cancellationToken);
				ctx.LogDebug (2, $"{me} #1 {connection.RemoteEndPoint}");

				if (HasAnyFlags (HttpOperationFlags.ServerAbortsHandshake))
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

				if (HasAnyFlags (HttpOperationFlags.ClientAbortsHandshake))
					throw ctx.AssertFail ("Expected client to abort handshake.");
			} catch (Exception ex) {
				if (HasAnyFlags (HttpOperationFlags.ServerAbortsHandshake, HttpOperationFlags.ClientAbortsHandshake))
					return false;
				ctx.LogDebug (2, $"{me} FAILED: {ex.Message}");
				throw;
			}

			if (!haveRequest) {
				ctx.LogMessage ($"{me} got empty requets!");
				throw ctx.AssertFail ("Got empty request.");
			}

			if (Server.UseSSL) {
				ctx.Assert (connection.SslStream.IsAuthenticated, "server is authenticated");
				if (HasAnyFlags (HttpOperationFlags.RequireClientCertificate))
					ctx.Assert (connection.SslStream.IsMutuallyAuthenticated, "server is mutually authenticated");
			}

			ctx.LogDebug (2, $"{me} DONE");
			return true;
		}

		internal void PrepareRedirect (TestContext ctx, HttpConnection connection, bool keepAlive)
		{
			lock (listener) {
				ctx.LogDebug (5, $"{ME} PREPARE REDIRECT: {keepAlive}");
				HttpConnection next;
				if (keepAlive)
					next = connection;
				else
					(next, _) = listener.CreateConnection (ctx, this, false);

				if (Interlocked.CompareExchange (ref redirectRequested, next, null) != null)
					throw new InvalidOperationException ();
			}
		}

		protected abstract Task<Response> RunInner (TestContext ctx, Request request, CancellationToken cancellationToken);

		internal virtual Stream CreateNetworkStream (TestContext ctx, Socket socket, bool ownsSocket)
		{
			return null;
		}

		protected abstract void Destroy ();

		int disposed;

		public void Dispose ()
		{
			Dispose (true);
			GC.SuppressFinalize (this);
		}

		protected void Dispose (bool disposing)
		{
			if (Interlocked.CompareExchange (ref disposed, 1, 0) != 0)
				return;
			requestTask.TrySetCanceled ();
			requestDoneTask.TrySetCanceled ();
			cts.Cancel ();
			cts.Dispose ();
			Destroy ();
		}
	}
}
