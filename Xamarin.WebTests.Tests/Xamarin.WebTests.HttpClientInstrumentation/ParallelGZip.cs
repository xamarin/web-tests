//
// ParallelGZip.cs
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
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Xamarin.AsyncTests;
using Xamarin.AsyncTests.Constraints;

namespace Xamarin.WebTests.HttpClientInstrumentation
{
	using ConnectionFramework;
	using TestFramework;
	using TestAttributes;
	using HttpFramework;
	using HttpHandlers;
	using TestRunners;

	[HttpServerTestCategory (HttpServerTestCategory.GZipInstrumentation)]
	public class ParallelGZip : HttpClientInstrumentationFixture
	{
		public override HttpOperationFlags OperationFlags => HttpOperationFlags.DontReuseConnection;

		public override HttpContent ExpectedContent => HttpContent.TheQuickBrownFox;

		[AsyncTest]
		public ParallelGZip (bool runThirdRequest)
		{
			RunThirdRequest = runThirdRequest;
		}

		public bool RunThirdRequest {
			get;
		}

		protected override void ConfigureRequest (
			TestContext ctx, InstrumentationOperation operation,
			CustomRequest request)
		{
			request.Handler.AutomaticDecompression = true;
			if (operation.Type == InstrumentationOperationType.Primary) {
				var servicePoint = ServicePointManager.FindServicePoint (request.RequestUri);
				servicePoint.ConnectionLimit = 1;
			}
			base.ConfigureRequest (ctx, operation, request);
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
			await RunParallelOperation (
				ctx, HttpOperationFlags.None,
				cancellationToken).ConfigureAwait (false);
			return false;
		}

		protected override async Task<bool> SecondaryReadHandler (
			TestContext ctx, InstrumentationOperation operation,
			int bytesRead, CancellationToken cancellationToken)
		{
			if (!RunThirdRequest || ReadHandlerCalled != 2)
				return false;

			await RunParallelOperation (
				ctx, HttpOperationFlags.None,
				cancellationToken).ConfigureAwait (false);
			return false;
		}

		protected override Task<Response> Run (
			TestContext ctx, CustomRequest request,
			CancellationToken cancellationToken)
		{
			return request.GetString (ctx, cancellationToken);
		}

		public override Task<HttpResponse> HandleRequest (
			TestContext ctx, InstrumentationOperation operation,
			HttpConnection connection, HttpRequest request,
			RequestFlags effectiveFlags,
			CancellationToken cancellationToken)
		{
			AssertNotReusingConnection (ctx, operation, connection);
			var content = new GZipContent (ConnectionHandler.TheQuickBrownFoxBuffer);
			var response = new HttpResponse (HttpStatusCode.OK, content);
			return Task.FromResult (response);
		}

		protected override Task<HttpOperation> RunSecondary (
			TestContext ctx, CancellationToken cancellationToken)
		{
			if (RunThirdRequest)
				ctx.Assert (ReadHandlerCalled, 
				            Is.GreaterThanOrEqualTo (2),
				            "ReadHandler called twice");
			else
				ctx.Assert (ReadHandlerCalled,
				            Is.EqualTo (2),
				            "ReadHandler called twice");
			return Task.FromResult<HttpOperation> (null);
		}
	}
}
