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
	using HttpFramework;
	using HttpHandlers;
	using TestFramework;
	using Resources;

	public abstract class InstrumentationTestRunner : AbstractConnection
	{
		public HttpServerProvider Provider {
			get;
		}

		internal Uri Uri {
			get;
		}

		internal HttpServerFlags ServerFlags {
			get;
		}

		public HttpServer Server {
			get;
		}

		public string ME {
			get;
		}

		public InstrumentationTestRunner (HttpServerProvider provider, string identifier)
		{
			Provider = provider;
			ServerFlags = provider.ServerFlags | HttpServerFlags.InstrumentationListener;
			ME = $"{GetType ().Name}({identifier})";

			var endPoint = ConnectionTestHelper.GetEndPoint ();

			var proto = (ServerFlags & HttpServerFlags.NoSSL) != 0 ? "http" : "https";
			Uri = new Uri ($"{proto}://{endPoint.Address}:{endPoint.Port}/");

			var parameters = GetParameters (identifier);

			Server = new BuiltinHttpServer (
				Uri, endPoint, ServerFlags, parameters,
				provider.SslStreamProvider);
		}

		static ConnectionParameters GetParameters (string identifier)
		{
			var certificateProvider = DependencyInjector.Get<ICertificateProvider> ();
			var acceptAll = certificateProvider.AcceptAll ();

			return new ConnectionParameters (identifier, ResourceManager.SelfSignedServerCertificate) {
				ClientCertificateValidator = acceptAll
			};
		}

		InstrumentationOperation currentOperation;
		InstrumentationOperation queuedOperation;
		volatile int readHandlerCalled;

		internal InstrumentationOperation PrimaryOperation => currentOperation;
		internal InstrumentationOperation QueuedOperation => queuedOperation;
		protected int ReadHandlerCalled => readHandlerCalled;

		internal InstrumentationHandler PrimaryHandler => (InstrumentationHandler)PrimaryOperation.Handler;

		public async Task Run (TestContext ctx, CancellationToken cancellationToken)
		{
			var me = $"{ME}.{nameof (Run)}()";
			ctx.LogDebug (2, $"{me}");

			var (handler, flags) = CreateHandler (ctx, true);

			ctx.LogDebug (2, $"{me}");

			currentOperation = CreateOperation (
				ctx, handler, InstrumentationOperationType.Primary, flags);

			currentOperation.Start (ctx, cancellationToken);

			try {
				await currentOperation.WaitForCompletion ().ConfigureAwait (false);
				ctx.LogDebug (2, $"{me} operation done");
			} catch (Exception ex) {
				ctx.LogDebug (2, $"{me} operation failed: {ex.Message}");
				throw;
			}

			await RunSecondary (ctx, cancellationToken);

			if (QueuedOperation != null) {
				ctx.LogDebug (2, $"{me} waiting for queued operations.");
				try {
					await QueuedOperation.WaitForCompletion ().ConfigureAwait (false);
					ctx.LogDebug (2, $"{me} done waiting for queued operations.");
				} catch (Exception ex) {
					ctx.LogDebug (2, $"{me} waiting for queued operations failed: {ex.Message}.");
					throw;
				}
			}

			Server.CloseAll ();
		}

		protected virtual Task RunSecondary (TestContext ctx, CancellationToken cancellationToken)
		{
			return FinishedTask;
		}

		protected abstract (Handler handler, HttpOperationFlags flags) CreateHandler (TestContext ctx, bool primary);

		internal abstract InstrumentationOperation CreateOperation (
			TestContext ctx, Handler handler,
			InstrumentationOperationType type,
			HttpOperationFlags flags);

		internal InstrumentationOperation StartOperation (
			TestContext ctx, CancellationToken cancellationToken,
			Handler handler, InstrumentationOperationType type,
			HttpOperationFlags flags)
		{
			var operation = CreateOperation (ctx, handler, type, flags);
			if (type == InstrumentationOperationType.Queued) {
				if (Interlocked.CompareExchange (ref queuedOperation, operation, null) != null)
					throw new InvalidOperationException ("Invalid nested call.");
			}
			operation.Start (ctx, cancellationToken);
			return operation;
		}

		protected override async Task Initialize (TestContext ctx, CancellationToken cancellationToken)
		{
			await Server.Initialize (ctx, cancellationToken).ConfigureAwait (false);
		}

		protected override async Task Destroy (TestContext ctx, CancellationToken cancellationToken)
		{
			currentOperation?.Dispose ();
			currentOperation = null;
			queuedOperation?.Dispose ();
			queuedOperation = null;
			await Server.Destroy (ctx, cancellationToken).ConfigureAwait (false);
		}

		protected override async Task PreRun (TestContext ctx, CancellationToken cancellationToken)
		{
			await Server.PreRun (ctx, cancellationToken).ConfigureAwait (false);
		}

		protected override async Task PostRun (TestContext ctx, CancellationToken cancellationToken)
		{
			await Server.PostRun (ctx, cancellationToken).ConfigureAwait (false);
		}

		protected override void Stop ()
		{
		}

		internal Task<bool> ReadHandler (
			TestContext ctx, InstrumentationOperationType type,
			int bytesRead, CancellationToken cancellationToken)
		{
			Interlocked.Increment (ref readHandlerCalled);

			if (type == InstrumentationOperationType.Primary)
				return PrimaryReadHandler (ctx, bytesRead, cancellationToken);
			return SecondaryReadHandler (ctx, bytesRead, cancellationToken);
		}

		internal virtual Task<bool> PrimaryReadHandler (
			TestContext ctx, int bytesRead, CancellationToken cancellationToken)
		{
			return Task.FromResult (false);
		}

		internal virtual Task<bool> SecondaryReadHandler (
			TestContext ctx, int bytesRead, CancellationToken cancellationToken)
		{
			return Task.FromResult (false);
		}
	}
}
