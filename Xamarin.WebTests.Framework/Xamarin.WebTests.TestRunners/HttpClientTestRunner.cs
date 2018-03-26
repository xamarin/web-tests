//
// HttpClientTestRunner.cs
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
using System.Linq;
using System.Net;
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
	using TestAttributes;
	using HttpClient;

	[HttpClientTestRunner]
	public class HttpClientTestRunner : InstrumentationTestRunner
	{
		public IHttpClientTestFixture Instance {
			get;
		}

		public HttpClientTestRunner (HttpServerProvider provider, IHttpClientTestFixture instance)
			: base (provider, instance.Value)
		{
			Instance = instance;
		}

		protected override async Task RunSecondary (TestContext ctx, CancellationToken cancellationToken)
		{
			var me = $"{ME}.{nameof (RunSecondary)}()";

			var secondOperation = Instance.RunSecondary (ctx, this, cancellationToken);
			if (secondOperation == null)
				return;

			ctx.LogDebug (2, $"{me} waiting for second operation.");
			try {
				await secondOperation.WaitForCompletion ().ConfigureAwait (false);
				ctx.LogDebug (2, $"{me} done waiting for second operation.");
			} catch (Exception ex) {
				ctx.LogDebug (2, $"{me} waiting for second operation failed: {ex.Message}.");
				throw;
			}
		}

		protected override (Handler handler, HttpOperationFlags flags) CreateHandler (TestContext ctx, bool primary)
		{
			var instanceHandler = Instance.CreateHandler (ctx, this, primary);
			return (instanceHandler, Instance.OperationFlags);
		}

		internal override Task<bool> PrimaryReadHandler (
			TestContext ctx, int bytesRead, CancellationToken cancellationToken)
		{
			return ((IInstrumentationCallbacks)Instance).PrimaryReadHandler (
				ctx, PrimaryOperation.Request, bytesRead, cancellationToken);
		}

		internal override Task<bool> SecondaryReadHandler (
			TestContext ctx, int bytesRead, CancellationToken cancellationToken)
		{
			return ((IInstrumentationCallbacks)Instance).SecondaryReadHandler (
				ctx, this, bytesRead, cancellationToken);
		}

		internal override InstrumentationOperation CreateOperation (
			TestContext ctx, Handler handler,
			InstrumentationOperationType type,
			HttpOperationFlags flags)
		{
			var expectedStatus = Instance.ExpectedStatus;
			var expectedError = Instance.ExpectedError;

			return new Operation (this, handler, type, flags, expectedStatus, expectedError);
		}

		public HttpOperation StartSequentialRequest (
			TestContext ctx, Handler handler, HttpOperationFlags flags,
			CancellationToken cancellationToken)
		{
			var operation = new Operation (
				this, handler, InstrumentationOperationType.Parallel, flags,
				HttpStatusCode.OK, WebExceptionStatus.Success);
			operation.Start (ctx, cancellationToken);
			return operation;
		}

		class Operation : InstrumentationOperation
		{
			new public HttpClientTestRunner Parent => (HttpClientTestRunner)base.Parent;

			public Operation (HttpClientTestRunner parent, Handler handler,
					  InstrumentationOperationType type, HttpOperationFlags flags,
					  HttpStatusCode expectedStatus, WebExceptionStatus expectedError)
				: base (parent, parent.ME,
					handler, type, flags, expectedStatus, expectedError)
			{
			}

			protected override Request CreateRequest (TestContext ctx, Uri uri)
			{
				return Parent.Instance.CreateRequest (ctx, uri, Handler);
			}

			protected override void ConfigureRequest (TestContext ctx, Uri uri, Request request)
			{
				Parent.Instance.ConfigureRequest (ctx, Handler, request);
				Handler.ConfigureRequest (ctx, request, uri);

				var proxy = Parent.Server.GetProxy ();
				if (proxy != null)
					request.SetProxy (proxy);
			}

			protected override Task<Response> RunInner (TestContext ctx, Request request, CancellationToken cancellationToken)
			{
				ctx.LogDebug (2, $"{ME} RUN INNER");

				return ctx.RunWithDisposableContext (
					innerCtx => Parent.Instance.Run (
						innerCtx, request, cancellationToken));
			}

			protected override void ConfigureNetworkStream (TestContext ctx, StreamInstrumentation instrumentation)
			{
				if (Parent.Instance is IInstrumentationCallbacks callbacks &&
				    callbacks.ConfigureNetworkStream (ctx, instrumentation))
					InstallReadHandler (ctx);
			}

			protected override void Destroy ()
			{
				;
			}
		}


	}
}
