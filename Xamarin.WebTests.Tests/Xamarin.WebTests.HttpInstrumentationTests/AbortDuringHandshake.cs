//
// AbortDuringHandshake.cs
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

	public class AbortDuringHandshake : HttpInstrumentationTestFixture
	{
		public enum ApiType {
			Sync,
			BeginEndAsync,
			TaskAsync
		}

		public ApiType Type {
			get;
		}

		[AsyncTest]
		public AbortDuringHandshake (ApiType type)
		{
			Type = type;
		}

		public override HttpOperationFlags OperationFlags => HttpOperationFlags.ServerAbortsHandshake | HttpOperationFlags.AbortAfterClientExits;

		public override HttpStatusCode ExpectedStatus => HttpStatusCode.InternalServerError;

		public override WebExceptionStatus ExpectedError => WebExceptionStatus.RequestCanceled;

		protected override Request CreateRequest (TestContext ctx, InstrumentationOperation operation, Uri uri)
		{
			return new InstrumentationRequest (this, uri);
		}

		protected override Task<Response> SendRequest (TestContext ctx, TraditionalRequest request, CancellationToken cancellationToken)
		{
			var instrumentationRequest = (InstrumentationRequest)request;
			switch (Type) {
			case ApiType.Sync:
				return instrumentationRequest.Send (ctx, cancellationToken);
			case ApiType.TaskAsync:
				return base.SendRequest (ctx, request, cancellationToken);
			case ApiType.BeginEndAsync:
				return instrumentationRequest.BeginEndSend (ctx, cancellationToken);
			default:
				throw ctx.AssertFail (Type);
			}
			return base.SendRequest (ctx, request, cancellationToken);
		}

		protected override bool ConfigureNetworkStream (
			TestContext ctx, StreamInstrumentation instrumentation)
		{
			return true;
		}

		protected override async Task<bool> PrimaryReadHandler (
			TestContext ctx, InstrumentationOperation operation,
			byte[] buffer, int offset, int size, int bytesRead,
			CancellationToken cancellationToken)
		{
			operation.Request.Abort ();
			// Wait until the client request finished, to make sure we are actually aboring.
			await operation.WaitForCompletion ().ConfigureAwait (false);
			return false;
		}
	}
}
