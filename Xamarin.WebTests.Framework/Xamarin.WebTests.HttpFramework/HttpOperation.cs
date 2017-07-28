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
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Linq;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xamarin.AsyncTests;
using Xamarin.AsyncTests.Constraints;
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

			ME = $"[{GetType ().Name}({ID}:{me})]";

			requestTask = new TaskCompletionSource<Request> ();
			requestDoneTask = new TaskCompletionSource<Response> ();
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
		ListenerContext listenerContext;
		InstrumentationListener instrumentationListener;
		ParallelListener parallelListener;
		TaskCompletionSource<Request> requestTask;
		TaskCompletionSource<Response> requestDoneTask;
		CancellationTokenSource cts;
		int requestStarted;

		string FormatConnection (HttpConnection connection)
		{
			return $"[{ME}:{connection.ME}]";
		}

		public bool HasRequest => currentRequest != null;

		protected abstract Request CreateRequest (TestContext ctx, Uri uri);

		protected abstract void ConfigureRequest (TestContext ctx, Uri uri, Request request);

		public async Task Run (TestContext ctx, CancellationToken cancellationToken)
		{
			cancellationToken.ThrowIfCancellationRequested ();
			Start (ctx, cancellationToken);

			await WaitForCompletion ().ConfigureAwait (false);
		}

		public async void Start (TestContext ctx, CancellationToken cancellationToken)
		{
			if (Interlocked.CompareExchange (ref requestStarted, 1, 0) != 0)
				throw new InternalErrorException ();

			var linkedCts = CancellationTokenSource.CreateLinkedTokenSource (cts.Token, cancellationToken);
			try {
				Response response;
				if ((Server.Flags & HttpServerFlags.NewListener) != 0)
					response = await RunNewListener (ctx, linkedCts.Token).ConfigureAwait (false);
				else
					response = await RunListener (ctx, linkedCts.Token).ConfigureAwait (false);
				requestDoneTask.TrySetResult (response);
			} catch (OperationCanceledException) {
				requestDoneTask.TrySetCanceled ();
			} catch (Exception ex) {
				ctx.LogDebug (5, $"{ME} FAILED: {ex.Message}");
				requestDoneTask.TrySetException (ex);
			} finally {
				linkedCts.Dispose ();
			}
		}

		public Task<Request> WaitForRequest ()
		{
			return requestTask.Task;
		}

		public Task<Response> WaitForCompletion ()
		{
			return requestDoneTask.Task;
		}

		async Task<Response> RunListener (TestContext ctx, CancellationToken cancellationToken)
		{
			var me = $"{ME} RUN";
			ctx.LogDebug (1, me);

			instrumentationListener = (InstrumentationListener)Server.Listener;

			var uri = Handler.RegisterRequest (ctx, Server);
			var request = CreateRequest (ctx, uri);
			currentRequest = request;

			if (request is TraditionalRequest traditionalRequest)
				servicePoint = traditionalRequest.RequestExt.ServicePoint;

			ConfigureRequest (ctx, uri, request);

			requestTask.SetResult (request);

			ctx.LogDebug (2, $"{me} #1: {uri} {request}");

			listenerContext = await instrumentationListener.CreateContext (ctx, this, cancellationToken).ConfigureAwait (false);

			var serverTask = listenerContext.Run (ctx, cancellationToken);
			await listenerContext.ServerStartTask.ConfigureAwait (false);

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
					tasks.Add (listenerContext.ServerInitTask);
				if (!serverDone)
					tasks.Add (serverTask);
				if (!clientDone)
					tasks.Add (clientTask);
				var finished = await Task.WhenAny (tasks).ConfigureAwait (false);

				string which;
				if (finished == listenerContext.ServerInitTask) {
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
					    (finished == serverTask || finished == listenerContext.ServerInitTask))
						ctx.LogDebug (2, $"{me} #4 - EXPECTED EXCEPTION {finished.Exception.GetType ()}");
					else {
						ctx.LogDebug (2, $"{me} #4 FAILED: {finished.Exception.Message}");
						throw finished.Exception;
					}
				}
			}

			var response = clientTask.Result;

			ctx.LogDebug (2, $"{me} DONE: {response}");

			CheckResponse (ctx, Handler, response, cancellationToken, ExpectedStatus, ExpectedError);

			return response;
		}

		internal void PrepareRedirect (TestContext ctx, HttpConnection connection, bool keepAlive)
		{
			listenerContext.PrepareRedirect (ctx, connection, keepAlive);
		}

		protected abstract Task<Response> RunInner (TestContext ctx, Request request, CancellationToken cancellationToken);

		internal virtual Stream CreateNetworkStream (TestContext ctx, Socket socket, bool ownsSocket)
		{
			return null;
		}

		void Debug (TestContext ctx, int level, string message, params object[] args)
		{
			var sb = new StringBuilder ();
			sb.AppendFormat ("{0}: {1}", ME, message);
			for (int i = 0; i < args.Length; i++) {
				sb.Append (" ");
				sb.Append (args[i] != null ? args[i].ToString () : "<null>");
			}

			ctx.LogDebug (level, sb.ToString ());
		}

		void CheckResponse (
			TestContext ctx, Handler handler, Response response, CancellationToken cancellationToken,
			HttpStatusCode expectedStatus = HttpStatusCode.OK, WebExceptionStatus expectedError = WebExceptionStatus.Success)
		{
			Debug (ctx, 1, "GOT RESPONSE", response.Status, response.IsSuccess, response.Error?.Message);

			if (ctx.HasPendingException)
				return;

			if (cancellationToken.IsCancellationRequested) {
				ctx.OnTestCanceled ();
				return;
			}

			if (expectedError != WebExceptionStatus.Success) {
				ctx.Expect (response.Error, Is.Not.Null, "expecting exception");
				ctx.Expect (response.Status, Is.EqualTo (expectedStatus));
				var wexc = response.Error as WebException;
				ctx.Expect (wexc, Is.Not.Null, "WebException");
				if (expectedError != WebExceptionStatus.AnyErrorStatus)
					ctx.Expect ((WebExceptionStatus)wexc.Status, Is.EqualTo (expectedError));
				return;
			}

			if (response.Error != null) {
				if (response.Content != null)
					ctx.OnError (new WebException (response.Content.AsString (), response.Error));
				else
					ctx.OnError (response.Error);
			} else {
				var ok = ctx.Expect (expectedStatus, Is.EqualTo (response.Status), "status code");
				if (ok)
					ok &= ctx.Expect (response.IsSuccess, Is.True, "success status");

				if (ok)
					ok &= handler.CheckResponse (ctx, response);
			}

			if (response.Content != null)
				Debug (ctx, 5, "GOT RESPONSE BODY", response.Content);
		}

		async Task<Response> RunNewListener (TestContext ctx, CancellationToken cancellationToken)
		{
			var me = $"{ME} NEW LISTENER";
			ctx.LogDebug (1, me);

			parallelListener = (ParallelListener)((BuiltinHttpServer)Server).Listener;

			var operation = parallelListener.RegisterOperation (ctx, this);
			var request = CreateRequest (ctx, operation.Uri);
			currentRequest = request;

			if (request is TraditionalRequest traditionalRequest)
				servicePoint = traditionalRequest.RequestExt.ServicePoint;

			ConfigureRequest (ctx, operation.Uri, request);

			requestTask.SetResult (request);

			ctx.LogDebug (2, $"{me} #1: {operation.Uri} {request}");

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
					tasks.Add (operation.ServerInitTask);
				if (!serverDone)
					tasks.Add (operation.ServerStartTask);
				if (!clientDone)
					tasks.Add (clientTask);
				var finished = await Task.WhenAny (tasks).ConfigureAwait (false);

				string which;
				if (finished == operation.ServerInitTask) {
					which = "init";
					initDone = true;
				} else if (finished == operation.ServerStartTask) {
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
					    (finished == operation.ServerStartTask || finished == operation.ServerInitTask))
						ctx.LogDebug (2, $"{me} #4 - EXPECTED EXCEPTION {finished.Exception.GetType ()}");
					else {
						ctx.LogDebug (2, $"{me} #4 FAILED: {finished.Exception.Message}");
						throw finished.Exception;
					}
				}
			}

			var response = clientTask.Result;

			ctx.LogDebug (2, $"{me} DONE: {response}");

			CheckResponse (ctx, Handler, response, cancellationToken, ExpectedStatus, ExpectedError);

			return response;
		}

		internal async Task HandleRequest (TestContext ctx, HttpConnection connection,
		                                   HttpRequest request, CancellationToken cancellationToken)
		{
			var me = $"{ME} HANDLE REQUEST";
			ctx.LogDebug (2, $"{me} {connection.ME} {request}");

			cancellationToken.ThrowIfCancellationRequested ();
			await request.Read (ctx, cancellationToken).ConfigureAwait (false);

			ctx.LogDebug (2, $"{me} REQUEST FULLY READ");
			var ret = await Handler.HandleRequest (ctx, this, connection, request, cancellationToken);
			ctx.LogDebug (2, $"{me} HANDLE REQUEST DONE: {ret}");
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
