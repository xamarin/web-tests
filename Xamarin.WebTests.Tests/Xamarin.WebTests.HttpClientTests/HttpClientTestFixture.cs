//
// HttpClientTestFixture.cs
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

namespace Xamarin.WebTests.HttpClientTests
{
	using TestFramework;
	using TestAttributes;
	using HttpFramework;
	using HttpHandlers;
	using TestRunners;

	[AsyncTestFixture (Prefix = "HttpClientTests")]
	public abstract class HttpClientTestFixture : InstrumentationTestRunner
	{
		[AsyncTest]
		public static Task Run (
			TestContext ctx, CancellationToken cancellationToken,
			HttpServerProvider provider,
			HttpClientTestFixture fixture)
		{
			return fixture.Run (ctx, cancellationToken);
		}

		[AsyncTest]
		[Martin (null, UseFixtureName = true)]
		[HttpServerTestCategory (HttpServerTestCategory.MartinTest)]
		public static Task MartinTest (
			TestContext ctx, CancellationToken cancellationToken,
			HttpServerProvider provider,
			HttpClientTestFixture fixture)
		{
			return fixture.Run (ctx, cancellationToken);
		}

		public override bool HasRequestBody => throw new InvalidOperationException ();

		Handler handler;

		public sealed override RequestFlags RequestFlags => handler.Flags;

		protected sealed override void InitializeHandler (TestContext ctx)
		{
			handler = CreateHandler (ctx);
		}

		public abstract Handler CreateHandler (TestContext ctx);

		protected override Request CreateRequest (
			TestContext ctx, InstrumentationOperation operation,
			Uri uri)
		{
			return new HttpClientRequest (uri);
		}

		protected override void ConfigureRequest (
			TestContext ctx, InstrumentationOperation operation,
			Request request, Uri uri)
		{
			handler.ConfigureRequest (ctx, request, uri);
			base.ConfigureRequest (ctx, operation, request, uri);
		}

		protected override Task<Response> Run (
			TestContext ctx, Request request,
			CancellationToken cancellationToken)
		{
			return Run (ctx, (HttpClientRequest)request, cancellationToken);
		}

		public abstract Task<Response> Run (
			TestContext ctx, HttpClientRequest request,
			CancellationToken cancellationToken);

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
