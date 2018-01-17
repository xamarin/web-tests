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

		protected internal Handler Handler {
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

			requestTask = Listener.TaskSupport.CreateAsyncCompletionSource<Request> ();
			requestDoneTask = Listener.TaskSupport.CreateAsyncCompletionSource<Response> ();
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
			protected set {
				servicePoint = value;
			}
		}

		internal bool HasAnyFlags (params HttpOperationFlags[] flags)
		{
			return flags.Any (f => (Flags & f) != 0);
		}

		Request currentRequest;
		ServicePoint servicePoint;
		ListenerOperation listenerOperation;
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
				var response = await RunListener (ctx, linkedCts.Token).ConfigureAwait (false);
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

		async Task<Response> RunListener (TestContext ctx, CancellationToken cancellationToken)
		{
			var me = $"{ME} RUN LISTENER";
			ctx.LogDebug (1, me);

			var operation = Server.Listener.RegisterOperation (ctx, this, Handler, null);
			var request = CreateRequest (ctx, operation.Uri);

			listenerOperation = operation;
			currentRequest = request;

			if (request is TraditionalRequest traditionalRequest)
				servicePoint = traditionalRequest.RequestExt.ServicePoint;

			ConfigureRequest (ctx, operation.Uri, request);

			requestTask.SetResult (request);

			ctx.LogDebug (2, $"{me} #1: {operation.Uri} {request}");

			var response = await Server.Listener.RunWithContext (
				ctx, operation, request, RunInner, cancellationToken).ConfigureAwait (false);

			ctx.LogDebug (2, $"{me} DONE: {response}");

			await Handler.CheckResponse (ctx, response, cancellationToken, ExpectedStatus, ExpectedError).ConfigureAwait (false);

			return response;
		}

		internal ListenerOperation RegisterRedirect (TestContext ctx, Handler handler, string path = null)
		{
			return Server.Listener.RegisterOperation (ctx, this, handler, path);
		}

		internal async Task StartDelayedListener (TestContext ctx)
		{
			/*
			 * We previously called RunListener() with HttpOperationFlags.DelayedListenerContext,
			 * which called the RunInner() function before actually starting to listening for a connection.
			 * 
			 */
			var me = $"{ME} DELAYED LISTENER";
			if (listenerOperation == null)
				throw new InvalidOperationException ();
			if (!listenerOperation.Operation.HasAnyFlags (HttpOperationFlags.DelayedListenerContext))
				throw new InvalidOperationException ();
			var context = await Server.Listener.FindContext (ctx, listenerOperation, false).ConfigureAwait (false);
			ctx.LogDebug (2, $"{ME} GOT CONTEXT: {context.ID}");
			listenerOperation.AssignContext (context);
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
