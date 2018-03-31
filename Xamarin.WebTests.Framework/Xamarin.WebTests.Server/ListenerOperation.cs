﻿//
// ListenerOperation.cs
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
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Runtime.ExceptionServices;
using Xamarin.AsyncTests;

namespace Xamarin.WebTests.Server
{
	using HttpFramework;
	using HttpHandlers;

	class ListenerOperation
	{
		public Listener Listener {
			get;
		}

		public HttpOperation Operation {
			get;
		}

		public ListenerHandler Handler {
			get;
		}

		public Uri Uri {
			get;
		}

		internal string ME {
			get;
		}

		static int nextID;
		public readonly int ID = Interlocked.Increment (ref nextID);

		TaskCompletionSource<object> serverInitTask;
		TaskCompletionSource<object> serverFinishedTask;
		TaskCompletionSource<ListenerContext> contextTask;
		TaskCompletionSource<object> finishedTask;
		ListenerOperation parentOperation;
		ExceptionDispatchInfo pendingError;
		bool hasInstrumentation;

		public ListenerOperation (
			Listener listener, HttpOperation operation,
			ListenerHandler handler, Uri uri)
		{
			Listener = listener;
			Operation = operation;
			Handler = handler;
			Uri = uri;

			ME = $"[{ID}:{GetType ().Name}:{operation.ME}]";
			serverInitTask = Listener.TaskSupport.CreateAsyncCompletionSource<object> ();
			serverFinishedTask = Listener.TaskSupport.CreateAsyncCompletionSource<object> ();
			contextTask = Listener.TaskSupport.CreateAsyncCompletionSource<ListenerContext> ();
			finishedTask = Listener.TaskSupport.CreateAsyncCompletionSource<object> ();
		}

		internal ListenerOperation TargetOperation {
			get;
			private set;
		}

		internal ListenerContext AssignedContext {
			get;
			private set;
		}

		internal ExceptionDispatchInfo PendingError => pendingError;

		internal void AssignContext (ListenerContext context)
		{
			hasInstrumentation = true;
			AssignedContext = context;
			context.AssignContext (this);
			contextTask.TrySetResult (context);
		}

		internal Task Wait ()
		{
			return finishedTask.Task;
		}

		internal Task<ListenerContext> WaitForContext ()
		{
			return contextTask.Task;
		}

		internal void Abort (Exception exception =  null)
		{
			if (hasInstrumentation) {
				hasInstrumentation = false;
				Listener.ReleaseInstrumentation (this);
			}
			if (exception != null) {
				finishedTask.TrySetException (exception);
				contextTask.TrySetException (exception);
				OnError (exception);
			} else {
				finishedTask.TrySetCanceled ();
				contextTask.TrySetCanceled ();
				OnCanceled ();
			}
		}

		internal void Finish ()
		{
			if (hasInstrumentation) {
				hasInstrumentation = false;
				Listener.ReleaseInstrumentation (this);
			}
			finishedTask.TrySetResult (null);
			contextTask.TrySetCanceled ();
		}

		public Task ServerInitTask => serverInitTask.Task;

		public Task ServerFinishedTask => serverFinishedTask.Task;

		internal async Task<HttpResponse> HandleRequest (
			TestContext ctx, ListenerContext context, HttpConnection connection,
			HttpRequest request, CancellationToken cancellationToken)
		{
			var me = $"{ME} HANDLE REQUEST";
			ctx.LogDebug (2, $"{me} {connection.ME} {request}");

			OnInit ();

			HttpResponse response;

			try {
				cancellationToken.ThrowIfCancellationRequested ();
				if (!Operation.HasAnyFlags (HttpOperationFlags.DontReadRequestBody)) {
					await request.Read (ctx, cancellationToken).ConfigureAwait (false);
					ctx.LogDebug (2, $"{me} REQUEST FULLY READ");
				} else {
					await request.ReadHeaders (ctx, cancellationToken).ConfigureAwait (false);
					ctx.LogDebug (2, $"{me} REQUEST HEADERS READ");
				}

				response = await HandleRequestInner (
					ctx, Operation, connection, request,
					Handler.RequestFlags, cancellationToken).ConfigureAwait (false);

				ctx.LogDebug (2, $"{me} HANDLE REQUEST DONE: {response}");
			} catch (OperationCanceledException) {
				OnCanceled ();
				throw;
			} catch (Exception ex) {
				OnError (ex);
				throw;
			}

			if (response.Redirect == null) {
				OnFinished ();
				return response;
			}

			response.Redirect.parentOperation = this;

			return response;
		}

		void DumpHeaders (TestContext ctx, HttpMessage message)
		{
			var sb = new StringBuilder ();
			foreach (var header in message.Headers) {
				sb.AppendFormat ("  {0} = {1}", header.Key, header.Value);
				sb.AppendLine ();
			}
			ctx.LogDebug (2, sb.ToString ());
		}

		async Task<HttpResponse> HandleRequestInner (
			TestContext ctx, HttpOperation operation,
			HttpConnection connection, HttpRequest request,
			RequestFlags effectiveFlags,
			CancellationToken cancellationToken)
		{
			Exception originalError;
			HttpResponse response;

			if (operation == null)
				throw new ArgumentNullException (nameof (operation));
			if (connection == null)
				throw new ArgumentNullException (nameof (connection));

			var expectServerError = operation.HasAnyFlags (HttpOperationFlags.ExpectServerException);

			try {
				ctx.LogDebug (1, $"HANDLE REQUEST: {connection.RemoteEndPoint}");
				DumpHeaders (ctx, request);
				connection.Server.CheckEncryption (ctx, connection.SslStream);
				response = await Handler.HandleRequest (
					ctx, operation, connection, request,
					effectiveFlags, cancellationToken);

				if (response == null)
					response = HttpResponse.CreateSuccess ();
				if ((effectiveFlags & RequestFlags.CloseConnection) != 0)
					response.CloseConnection = true;
				if (!response.KeepAlive.HasValue && ((effectiveFlags & RequestFlags.KeepAlive) != 0))
					response.KeepAlive = true;
				response.ResolveHeaders ();

				ctx.LogDebug (1, $"HANDLE REQUEST DONE: {connection.RemoteEndPoint}", response);
				DumpHeaders (ctx, response);
				return response;
			} catch (AssertionException ex) {
				originalError = ex;
				response = HttpResponse.CreateError (ex.Message);
			} catch (OperationCanceledException) {
				throw;
			} catch (Exception ex) {
				originalError = ex;
				response = HttpResponse.CreateError ("Caught unhandled exception", ex);
			}

			if (ctx.IsCanceled || cancellationToken.IsCancellationRequested) {
				ctx.LogDebug (1, "HANDLE REQUEST - CANCELED");
				throw new OperationCanceledException ();
			}

			if (originalError is AssertionException)
				ctx.LogDebug (1, "HANDLE REQUEST - ASSERTION FAILED", originalError);
			else if (expectServerError)
				ctx.LogDebug (1, "HANDLE REQUEST - EXPECTED ERROR", originalError.GetType ());
			else
				ctx.LogDebug (1, "HANDLE REQUEST - ERROR", originalError);

			return response;
		}

		internal void RegisterProxyAuth (ListenerOperation redirect)
		{
			redirect.parentOperation = this;
		}

		internal ListenerOperation CreateProxy (Listener listener)
		{
			var proxy = new ListenerOperation (listener, Operation, Handler, Uri);
			proxy.TargetOperation = this;
			parentOperation = proxy;
			return proxy;
		}

		void OnInit ()
		{
			serverInitTask.TrySetResult (null);
			parentOperation?.OnInit ();
		}

		void OnFinished ()
		{
			serverFinishedTask.TrySetResult (null);
			parentOperation?.OnFinished ();
		}

		void OnCanceled ()
		{
			var error = new OperationCanceledException ();
			var captured = ExceptionDispatchInfo.Capture (error);
			Interlocked.CompareExchange (ref pendingError, captured, null);
			serverInitTask.TrySetCanceled ();
			serverFinishedTask.TrySetCanceled ();
			parentOperation?.OnCanceled ();
		}

		internal void OnError (Exception error)
		{
			var captured = ExceptionDispatchInfo.Capture (error);
			Interlocked.CompareExchange (ref pendingError, captured, null);
			serverInitTask.TrySetException (error);
			serverFinishedTask.TrySetException (error);
			parentOperation?.OnCanceled ();
		}
	}
}
