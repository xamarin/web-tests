//
// TraditionalTestFixture.cs
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

namespace Xamarin.WebTests.TraditionalTests
{
	using TestFramework;
	using TestAttributes;
	using HttpFramework;
	using HttpHandlers;
	using TestRunners;

	[Work]
	[AsyncTestFixture (Prefix = "TraditionalTests")]
	public abstract class TraditionalTestFixture : InstrumentationTestRunner
	{
		[AsyncTest]
		public static Task Run (
			TestContext ctx, CancellationToken cancellationToken,
			HttpServerProvider provider,
			TraditionalTestFixture fixture)
		{
			return fixture.Run (ctx, cancellationToken);
		}

		[AsyncTest]
		[Martin (null, UseFixtureName = true)]
		[HttpServerTestCategory (HttpServerTestCategory.MartinTest)]
		public static Task MartinTest (
			TestContext ctx, CancellationToken cancellationToken,
			HttpServerProvider provider,
			TraditionalTestFixture fixture)
		{
			return fixture.Run (ctx, cancellationToken);
		}

		public sealed override bool HasRequestBody => throw new InvalidOperationException ();

		Handler handler;

		public sealed override RequestFlags RequestFlags => handler.Flags;

		protected sealed override void InitializeHandler (TestContext ctx)
		{
			handler = CreateHandler (ctx);
		}

		protected abstract Handler CreateHandler (TestContext ctx);

		protected sealed override Request CreateRequest (
			TestContext ctx, InstrumentationOperation operation,
			Uri uri)
		{
			return new TraditionalRequest (uri);
		}

		protected sealed override void ConfigureRequest (
			TestContext ctx, InstrumentationOperation operation,
			Request request, Uri uri)
		{
			var traditionalRequest = (TraditionalRequest)request;
			handler.ConfigureRequest (ctx, request, uri);
			ConfigureRequest (ctx, operation, traditionalRequest);
		}

		protected virtual void ConfigureRequest (
			TestContext ctx, InstrumentationOperation operation,
			TraditionalRequest request)
		{
		}

		protected override Task<Response> Run (
			TestContext ctx, Request request,
			CancellationToken cancellationToken)
		{
			return ((TraditionalRequest)request).SendAsync (ctx, cancellationToken);
		}

		public sealed override Task<HttpResponse> HandleRequest (
			TestContext ctx, InstrumentationOperation operation,
			HttpConnection connection, HttpRequest request,
			RequestFlags effectiveFlags, CancellationToken cancellationToken)
		{
			return handler.HandleRequest (
				ctx, operation, connection, request,
				effectiveFlags, cancellationToken);
		}

		public sealed override bool CheckResponse (
			TestContext ctx, Response response)
		{
			return handler.CheckResponse (ctx, response);
		}
	}
}
