//
// InstrumentationTestRunner.cs
//
// Author:
//       Martin Baulig <mabaul@microsoft.com>
//
// Copyright (c) 2018 Xamarin Inc. (http://www.xamarin.com)
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
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;
using Xamarin.AsyncTests;
using Xamarin.AsyncTests.Constraints;
using Xamarin.AsyncTests.Portable;

namespace Xamarin.WebTests.TestRunners
{
	using ConnectionFramework;
	using TestAttributes;
	using HttpFramework;
	using HttpHandlers;
	using TestFramework;
	using Server;
	using Resources;

	public abstract class InstrumentationTestRunner : AbstractConnection, ListenerHandler
	{
		public HttpServer Server {
			get;
			private set;
		}

		public string ME => GetType ().Name;

		ConnectionParameters GetParameters (TestContext ctx)
		{
			var certificateProvider = DependencyInjector.Get<ICertificateProvider> ();
			var acceptAll = certificateProvider.AcceptAll ();

			var parameters = new ConnectionParameters (ResourceManager.SelfSignedServerCertificate) {
				ClientCertificateValidator = acceptAll
			};

			CreateParameters (ctx, parameters);
			return parameters;
		}

		protected virtual void CreateParameters (TestContext ctx, ConnectionParameters parameters)
		{
		}

		InstrumentationOperation currentOperation;
		InstrumentationOperation queuedOperation;
		volatile int readHandlerCalled;

		protected InstrumentationOperation PrimaryOperation => currentOperation;
		protected HttpOperation QueuedOperation => queuedOperation;
		protected int ReadHandlerCalled => readHandlerCalled;

		public virtual HttpStatusCode ExpectedStatus => HttpStatusCode.OK;

		public virtual WebExceptionStatus ExpectedError => WebExceptionStatus.Success;

		public virtual HttpOperationFlags OperationFlags => HttpOperationFlags.None;

		public virtual HttpContent ExpectedContent {
			get => new StringContent (GetType ().Name);
		}

		public virtual RequestFlags RequestFlags => RequestFlags.KeepAlive;

		string ListenerHandler.Value => ME;

		protected abstract void InitializeHandler (TestContext ctx);

		protected internal abstract Request CreateRequest (
			TestContext ctx, InstrumentationOperation operation,
			Uri uri);

		protected internal virtual void ConfigureRequest (
			TestContext ctx, InstrumentationOperation operation,
			Request request, Uri uri)
		{
		}

		internal const string LogCategory = LogCategories.Instrumentation;

		public async Task Run (TestContext ctx, CancellationToken cancellationToken)
		{
			var me = $"{ME}.{nameof (Run)}()";
			ctx.LogDebug (LogCategory, 2, $"{me}");

			InitializeHandler (ctx);

			ctx.LogDebug (LogCategory, 2, $"{me}");

			currentOperation = new InstrumentationOperation (
				this, InstrumentationOperationType.Primary,
				OperationFlags);

			currentOperation.Start (ctx, cancellationToken);

			try {
				await currentOperation.WaitForCompletion ().ConfigureAwait (false);
				ctx.LogDebug (LogCategory, 2, $"{me} operation done");
			} catch (Exception ex) {
				ctx.LogDebug (LogCategory, 2, $"{me} operation failed: {ex.Message}");
				throw;
			}

			var secondOperation = await RunSecondary (
				ctx, cancellationToken).ConfigureAwait (false);

			if (secondOperation != null) {
				ctx.LogDebug (LogCategory, 2, $"{me} waiting for second operation.");
				try {
					await secondOperation.WaitForCompletion ().ConfigureAwait (false);
					ctx.LogDebug (LogCategory, 2, $"{me} done waiting for second operation.");
				} catch (Exception ex) {
					ctx.LogDebug (LogCategory, 2, $"{me} waiting for second operation failed: {ex.Message}.");
					throw;
				}
			}

			if (QueuedOperation != null) {
				ctx.LogDebug (LogCategory, 2, $"{me} waiting for queued operations.");
				try {
					await QueuedOperation.WaitForCompletion ().ConfigureAwait (false);
					ctx.LogDebug (LogCategory, 2, $"{me} done waiting for queued operations.");
				} catch (Exception ex) {
					ctx.LogDebug (LogCategory, 2, $"{me} waiting for queued operations failed: {ex.Message}.");
					throw;
				}
			}

			Server.CloseAll ();
		}

		protected internal abstract Task<Response> Run (
			TestContext ctx, Request request,
			CancellationToken cancellationToken);

		protected virtual Task<HttpOperation> RunSecondary (
			TestContext ctx, CancellationToken cancellationToken)
		{
			return Task.FromResult<HttpOperation> (null);
		}

		public InstrumentationOperation CreateOperation (
			TestContext ctx, InstrumentationOperationType type,
			HttpOperationFlags? flags = null,
			HttpStatusCode? expectedStatus = null,
			WebExceptionStatus? expectedError = null)
		{
			return new InstrumentationOperation (
				this, type, flags, expectedStatus, expectedError);
		}

		public InstrumentationOperation StartOperation (
			TestContext ctx, CancellationToken cancellationToken,
			InstrumentationOperationType type, HttpOperationFlags flags)
		{
			var operation = CreateOperation (ctx, type, flags);
			return StartOperation (ctx, cancellationToken, operation);
		}

		public InstrumentationOperation StartOperation (
			TestContext ctx, CancellationToken cancellationToken,
			InstrumentationOperation operation)
		{
			if (operation.Type == InstrumentationOperationType.Queued) {
				if (Interlocked.CompareExchange (ref queuedOperation, operation, null) != null)
					throw new InvalidOperationException ("Invalid nested call.");
			}
			operation.Start (ctx, cancellationToken);
			return operation;
		}

		protected Task RunParallelOperation (
			TestContext ctx, HttpOperationFlags flags,
			CancellationToken cancellationToken)
		{
			return StartParallelOperation (
				ctx, flags, cancellationToken).WaitForCompletion ();
		}

		protected HttpOperation StartParallelOperation (
			TestContext ctx, HttpOperationFlags flags,
			CancellationToken cancellationToken)
		{
			return StartOperation (
				ctx, cancellationToken,
				InstrumentationOperationType.Parallel, flags);
		}

		protected HttpOperation StartSequentialRequest (
			TestContext ctx, HttpOperationFlags flags,
			CancellationToken cancellationToken)
		{
			var operation = CreateOperation (
				ctx, InstrumentationOperationType.Parallel, flags,
				HttpStatusCode.OK, WebExceptionStatus.Success);
			operation.Start (ctx, cancellationToken);
			return operation;
		}

		protected Task StartDelayedSecondaryOperation (TestContext ctx)
		{
			return QueuedOperation.StartDelayedListener (ctx);
		}

		protected sealed override async Task Initialize (TestContext ctx, CancellationToken cancellationToken)
		{
			var provider = ctx.GetParameter<HttpServerProvider> ();

			var serverFlags = provider.ServerFlags | HttpServerFlags.InstrumentationListener;

			var endPoint = ConnectionTestHelper.GetEndPoint ();

			var proto = (serverFlags & HttpServerFlags.NoSSL) != 0 ? "http" : "https";
			var uri = new Uri ($"{proto}://{endPoint}/");

			var parameters = GetParameters (ctx);

			Server = new BuiltinHttpServer (
				uri, endPoint, serverFlags, parameters,
				provider.SslStreamProvider);

			await Server.Initialize (ctx, cancellationToken).ConfigureAwait (false);
		}

		protected sealed override async Task Destroy (TestContext ctx, CancellationToken cancellationToken)
		{
			currentOperation?.Dispose ();
			currentOperation = null;
			queuedOperation?.Dispose ();
			queuedOperation = null;
			await Server.Destroy (ctx, cancellationToken).ConfigureAwait (false);
		}

		protected sealed override async Task PreRun (TestContext ctx, CancellationToken cancellationToken)
		{
			await Server.PreRun (ctx, cancellationToken).ConfigureAwait (false);
		}

		protected sealed override async Task PostRun (TestContext ctx, CancellationToken cancellationToken)
		{
			await Server.PostRun (ctx, cancellationToken).ConfigureAwait (false);
		}

		protected sealed override void Stop ()
		{
		}

		Task<HttpResponse> ListenerHandler.HandleRequest (
			TestContext ctx, HttpOperation operation,
			HttpConnection connection, HttpRequest request,
			RequestFlags effectiveFlags,
			CancellationToken cancellationToken) => HandleRequest (
				ctx, (InstrumentationOperation)operation,
				connection, request, effectiveFlags, cancellationToken);

		public virtual Task<HttpResponse> HandleRequest (
			TestContext ctx, InstrumentationOperation operation,
			HttpConnection connection, HttpRequest request,
			RequestFlags effectiveFlags,
			CancellationToken cancellationToken)
		{
			return Task.Run (() => HandleRequest (
				ctx, operation, connection, request));
		}

		public virtual HttpResponse HandleRequest (
			TestContext ctx, InstrumentationOperation operation,
			HttpConnection connection, HttpRequest request)
		{
			throw ctx.AssertFail ("Must override this.");
		}

		public abstract bool HasRequestBody {
			get;
		}

		protected internal virtual async Task WriteRequestBody (
			TestContext ctx, TraditionalRequest request,
			CancellationToken cancellationToken)
		{
			using (var stream = await request.RequestExt.GetRequestStreamAsync ().ConfigureAwait (false)) {
				await WriteRequestBody (ctx, request, stream, cancellationToken);
			}
		}

		protected internal virtual Task WriteRequestBody (
			TestContext ctx, TraditionalRequest request, Stream stream,
			CancellationToken cancellationToken)
		{
			throw ctx.AssertFail ("Must override this.");
		}

		protected internal virtual Task<Response> SendRequest (
			TestContext ctx, TraditionalRequest request,
			CancellationToken cancellationToken)
		{
			return ((InstrumentationRequest)request).DefaultSendAsync (ctx, cancellationToken);
		}

		protected internal async virtual Task<TraditionalResponse> ReadResponse (
			TestContext ctx, TraditionalRequest request,
			HttpWebResponse response,
			WebException error, CancellationToken cancellationToken)
		{
			cancellationToken.ThrowIfCancellationRequested ();

			HttpContent content;
			var status = response.StatusCode;

			using (var stream = response.GetResponseStream ()) {
				content = await ReadResponseBody (
					ctx, request, response, stream, cancellationToken).ConfigureAwait (false);
			}

			return new TraditionalResponse (request, response, content, error);
		}

		protected async virtual Task<HttpContent> ReadResponseBody (
			TestContext ctx, TraditionalRequest request,
			HttpWebResponse response, Stream stream,
			CancellationToken cancellationToken)
		{
			cancellationToken.ThrowIfCancellationRequested ();

			string body = null;
			using (var reader = new StreamReader (stream)) {
				if (!reader.EndOfStream)
					body = await reader.ReadToEndAsync ().ConfigureAwait (false);
			}

			return StringContent.CreateMaybeNull (body);
		}

		public virtual bool CheckResponse (TestContext ctx, Response response)
		{
			if (!ctx.Expect (response.Content, Is.Not.Null, "response.Content != null"))
				return false;

			return HttpContent.Compare (ctx, response.Content, ExpectedContent, false, "response.Content");
		}

		protected void AssertNotReusingConnection (
			TestContext ctx, InstrumentationOperation operation,
			HttpConnection connection)
		{
			ctx.LogDebug (LogCategory, 2, $"{ME}: {operation == PrimaryOperation} {connection.RemoteEndPoint}");
			if (operation == PrimaryOperation)
				return;
			ctx.Assert (connection.RemoteEndPoint,
				    Is.Not.Null.And.NotEqualTo (PrimaryOperation.RemoteEndPoint),
				    "RemoteEndPoint");
		}

		protected void AssertReusingConnection (
			TestContext ctx, InstrumentationOperation operation,
			HttpConnection connection)
		{
			ctx.LogDebug (LogCategory, 2, $"{ME}: {operation == PrimaryOperation} {connection.RemoteEndPoint}");
			if (operation == PrimaryOperation)
				return;
			ctx.Assert (connection.RemoteEndPoint,
				    Is.Not.Null.And.EqualTo (PrimaryOperation.RemoteEndPoint),
				    "RemoteEndPoint");
		}

		internal Task<bool> ReadHandler (
			TestContext ctx, InstrumentationOperation operation,
			byte[] buffer, int offset, int size, int bytesRead,
			CancellationToken cancellationToken)
		{
			Interlocked.Increment (ref readHandlerCalled);

			if (operation.Type == InstrumentationOperationType.Primary)
				return PrimaryReadHandler (
					ctx, operation, buffer, offset, size,
					bytesRead, cancellationToken);
			return SecondaryReadHandler (
				ctx, operation, bytesRead, cancellationToken);
		}

		protected internal virtual bool ConfigureNetworkStream (
			TestContext ctx, StreamInstrumentation instrumentation)
		{
			return false;
		}

		protected virtual Task<bool> PrimaryReadHandler (
			TestContext ctx, InstrumentationOperation operation,
			byte[] buffer, int offset, int size, int bytesRead,
			CancellationToken cancellationToken)
		{
			return Task.FromResult (false);
		}

		protected virtual Task<bool> SecondaryReadHandler (
			TestContext ctx, InstrumentationOperation operation,
			int bytesRead, CancellationToken cancellationToken)
		{
			return Task.FromResult (false);
		}
	}
}
