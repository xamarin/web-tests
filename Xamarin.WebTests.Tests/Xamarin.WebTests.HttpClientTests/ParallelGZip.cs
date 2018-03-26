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

namespace Xamarin.WebTests.HttpClientTests
{
	using ConnectionFramework;
	using TestFramework;
	using TestAttributes;
	using HttpFramework;
	using HttpHandlers;
	using TestRunners;

	[HttpServerTestCategory (HttpServerTestCategory.GZipInstrumentation)]
	public class ParallelGZip : InstrumentationFixture
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
			TestContext ctx, CustomHandler handler, CustomRequest request)
		{
			request.Handler.AutomaticDecompression = true;
			if (handler.IsPrimary) {
				var servicePoint = ServicePointManager.FindServicePoint (request.RequestUri);
				servicePoint.ConnectionLimit = 1;
			}
			base.ConfigureRequest (ctx, handler, request);
		}

		protected override bool ConfigureNetworkStream (
			TestContext ctx, StreamInstrumentation instrumentation)
		{
			return true;
		}

		protected override async Task<bool> PrimaryReadHandler (
			TestContext ctx, CustomRequest request, int bytesRead,
			CancellationToken cancellationToken)
		{
			var parallelHandler = new CustomHandler (
				this, request.Parent.TestRunner, false);
			await request.Parent.TestRunner.RunParallelOperation (
				ctx, parallelHandler, HttpOperationFlags.None,
				cancellationToken).ConfigureAwait (false);
			return false;
		}

		protected override async Task<bool> SecondaryReadHandler (
			TestContext ctx, HttpClientTestRunner runner,
			int bytesRead, CancellationToken cancellationToken)
		{
			if (!RunThirdRequest || runner.ReadHandlerCalled != 2)
				return false;

			var parallelHandler = new CustomHandler (this, runner, false);
			await runner.RunParallelOperation (
				ctx, parallelHandler, HttpOperationFlags.None,
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
			TestContext ctx, HttpOperation operation,
			HttpConnection connection, HttpRequest request,
			CustomHandler handler, CancellationToken cancellationToken)
		{
			handler.AssertNotReusingConnection (ctx, connection);
			var content = new GZipContent (ConnectionHandler.TheQuickBrownFoxBuffer);
			var response = new HttpResponse (HttpStatusCode.OK, content);
			return Task.FromResult (response);
		}

		public override HttpOperation RunSecondary (
			TestContext ctx, HttpClientTestRunner runner,
			CancellationToken cancellationToken)
		{
			if (RunThirdRequest)
				ctx.Assert (runner.ReadHandlerCalled, 
				            Is.GreaterThanOrEqualTo (2),
				            "ReadHandler called twice");
			else
				ctx.Assert (runner.ReadHandlerCalled,
				            Is.EqualTo (2),
				            "ReadHandler called twice");
			return null;
		}
	}
}
