//
// QueuedRequests.cs
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
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Xamarin.AsyncTests;
using Xamarin.AsyncTests.Constraints;

namespace Xamarin.WebTests.HttpInstrumentationTests
{
	using ConnectionFramework;
	using HttpFramework;
	using HttpHandlers;
	using TestRunners;

	public class QueuedRequests : HttpInstrumentationTestFixture
	{
		public QueuedRequestType Type {
			get;
		}

		public sealed override HttpOperationFlags OperationFlags {
			get;
		}

		public sealed override HttpStatusCode ExpectedStatus {
			get;
		}

		public sealed override WebExceptionStatus ExpectedError {
			get;
		}

		[AsyncTest]
		public QueuedRequests (QueuedRequestType type)
		{
			Type = type;

			switch (type) {
			case QueuedRequestType.Simple:
			case QueuedRequestType.CancelQueued:
				OperationFlags = HttpOperationFlags.None;
				ExpectedStatus = HttpStatusCode.OK;
				ExpectedError = WebExceptionStatus.Success;
				break;
			case QueuedRequestType.CancelMain:
				OperationFlags = HttpOperationFlags.ServerAbortsHandshake | HttpOperationFlags.AbortAfterClientExits;
				ExpectedStatus = HttpStatusCode.InternalServerError;
				ExpectedError = WebExceptionStatus.RequestCanceled;
				break;
			}
		}

		public enum QueuedRequestType {
			Simple,
			CancelQueued,
			CancelMain
		}

		public override RequestFlags RequestFlags => RequestFlags.CloseConnection;

		ServicePoint servicePoint;

		protected override void ConfigurePrimaryRequest (
			TestContext ctx, InstrumentationOperation operation,
			TraditionalRequest request)
		{
			ctx.Assert (servicePoint, Is.Null, "ServicePoint");
			servicePoint = ServicePointManager.FindServicePoint (request.Uri);
			servicePoint.ConnectionLimit = 1;
			base.ConfigurePrimaryRequest (ctx, operation, request);
		}

		protected override void ConfigureSecondaryRequest (
			TestContext ctx, InstrumentationOperation operation,
			TraditionalRequest request)
		{
			ctx.Assert (servicePoint, Is.Not.Null, "ServicePoint");
			ctx.Assert (servicePoint.CurrentConnections, Is.EqualTo (1), "ServicePoint.CurrentConnections");
			base.ConfigureSecondaryRequest (ctx, operation, request);
		}

		public override HttpResponse HandleRequest (
			TestContext ctx, InstrumentationOperation operation,
			HttpConnection connection, HttpRequest request)
		{
			return new HttpResponse (HttpStatusCode.OK, ExpectedContent);
		}

		protected override bool ConfigureNetworkStream (
			TestContext ctx, StreamInstrumentation instrumentation)
		{
			return true;
		}

		int primaryReadHandlerCalled;
		int secondaryReadHandlerCalled;

		protected override async Task<bool> PrimaryReadHandler (
			TestContext ctx, InstrumentationOperation operation,
			byte[] buffer, int offset, int size, int bytesRead,
			CancellationToken cancellationToken)
		{
			++primaryReadHandlerCalled;
			HttpOperation queuedOperation;
			Request queuedRequest;
			switch (Type) {
			case QueuedRequestType.Simple:
				StartQueuedOperation (
					HttpOperationFlags.None, HttpStatusCode.OK,
					WebExceptionStatus.Success);
				break;
			case QueuedRequestType.CancelQueued:
				queuedOperation = StartQueuedOperation (
					HttpOperationFlags.AbortAfterClientExits,
					HttpStatusCode.InternalServerError,
					WebExceptionStatus.RequestCanceled);
				queuedRequest = await queuedOperation.WaitForRequest ().ConfigureAwait (false);
				// Wait a bit to make sure the request has been queued.
				await Task.Delay (500).ConfigureAwait (false);
				queuedRequest.Abort ();
				break;
			case QueuedRequestType.CancelMain:
				queuedOperation = StartQueuedOperation (
					HttpOperationFlags.None, HttpStatusCode.OK,
					WebExceptionStatus.Success);
				queuedRequest = await queuedOperation.WaitForRequest ().ConfigureAwait (false);
				// Wait a bit to make sure the request has been queued.
				await Task.Delay (2500).ConfigureAwait (false);
				operation.Request.Abort ();
				break;
			}

			return false;

			HttpOperation StartQueuedOperation (
				HttpOperationFlags flags,
				HttpStatusCode expectedStatus,
				WebExceptionStatus expectedError)
			{
				var queued = CreateOperation (
					ctx, InstrumentationOperationType.Queued,
					flags, expectedStatus, expectedError);
				return StartOperation (
					ctx, cancellationToken, queued);
			}
		}

		protected override Task<bool> SecondaryReadHandler (
			TestContext ctx, InstrumentationOperation operation,
			int bytesRead, CancellationToken cancellationToken)
		{
			++secondaryReadHandlerCalled;
			return Task.FromResult (false);
		}

		protected override async Task<HttpOperation> RunSecondary (
			TestContext ctx, CancellationToken cancellationToken)
		{
			switch (Type) {
			case QueuedRequestType.Simple:
				ctx.Assert (QueuedOperation, Is.Not.Null, "have queued task");
				await QueuedOperation.WaitForCompletion ().ConfigureAwait (false);
				ctx.Assert (primaryReadHandlerCalled, Is.EqualTo (1), "PrimaryReadHandler called");
				ctx.Assert (secondaryReadHandlerCalled, Is.EqualTo (1), "SecondaryReadHandler called");
				break;
			}
			return null;
		}
	}
}
