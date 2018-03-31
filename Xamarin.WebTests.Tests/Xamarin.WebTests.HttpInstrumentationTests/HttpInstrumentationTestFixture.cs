//
// HttpInstrumentationTestFixture.cs
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

namespace Xamarin.WebTests.HttpInstrumentationTests
{
	using ConnectionFramework;
	using TestFramework;
	using TestAttributes;
	using HttpFramework;
	using HttpHandlers;
	using TestRunners;

	[Work]
	[HttpServerTestCategory (HttpServerTestCategory.Instrumentation)]
	[AsyncTestFixture (Prefix = "HttpInstrumentationTests")]
	public abstract class HttpInstrumentationTestFixture
		: InstrumentationTestRunner
	{
		[AsyncTest]
		public static Task Run (
			TestContext ctx, CancellationToken cancellationToken,
			HttpServerProvider provider,
			HttpInstrumentationTestFixture fixture)
		{
			return fixture.Run (ctx, cancellationToken);
		}

		[AsyncTest]
		[Martin (null, UseFixtureName = true)]
		[HttpServerTestCategory (HttpServerTestCategory.MartinTest)]
		public static Task MartinTest (
			TestContext ctx, CancellationToken cancellationToken,
			HttpServerProvider provider,
			HttpInstrumentationTestFixture fixture)
		{
			return fixture.Run (ctx, cancellationToken);
		}

		protected override void InitializeHandler (TestContext ctx)
		{
		}

		protected override Request CreateRequest (
			TestContext ctx, InstrumentationOperation operation,
			Uri uri)
		{
			return new TraditionalRequest (uri);
		}

		public override RequestFlags RequestFlags => RequestFlags.KeepAlive;

		public override bool HasRequestBody => false;

		protected sealed override void ConfigureRequest (
			TestContext ctx, InstrumentationOperation operation,
			Request request, Uri uri)
		{
			var traditionalRequest = (TraditionalRequest)request;
			if (operation.Type == InstrumentationOperationType.Primary)
				ConfigurePrimaryRequest (ctx, operation, traditionalRequest);
			else
				ConfigureSecondaryRequest (ctx, operation, traditionalRequest);
		}

		protected virtual void ConfigurePrimaryRequest (
			TestContext ctx, InstrumentationOperation operation,
			TraditionalRequest request)
		{
		}

		protected virtual void ConfigureSecondaryRequest (
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
	}
}
