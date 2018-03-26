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
	using Xamarin.WebTests.ConnectionFramework;

	[New]
	[AsyncTestFixture (Prefix = "HttpClientTests")]
	public abstract class HttpClientTestFixture : IHttpClientTestFixture
	{
		public string Value => GetType ().FullName;

		public string FriendlyValue => GetType ().Name;

		internal string ME => FriendlyValue;

		[AsyncTest]
		public static Task Run (
			TestContext ctx, CancellationToken cancellationToken,
			HttpServerProvider provider,
			HttpClientTestFixture fixture,
			HttpClientTestRunner runner)
		{
			return runner.Run (ctx, cancellationToken);
		}

		[AsyncTest]
		[Martin (null, UseFixtureName = true)]
		[HttpServerTestCategory (HttpServerTestCategory.MartinTest)]
		public static Task MartinTest (
			TestContext ctx, CancellationToken cancellationToken,
			HttpServerProvider provider,
			HttpClientTestFixture fixture,
			HttpClientTestRunner runner)
		{
			return runner.Run (ctx, cancellationToken);
		}

		public virtual HttpStatusCode ExpectedStatus => HttpStatusCode.OK;

		public virtual WebExceptionStatus ExpectedError => WebExceptionStatus.Success;

		public virtual HttpOperationFlags OperationFlags => HttpOperationFlags.None;

		public abstract Handler CreateHandler (
			TestContext ctx, HttpClientTestRunner runner, bool primary);

		public abstract Request CreateRequest (
			TestContext ctx, Uri uri, Handler handler);

		public virtual void ConfigureRequest (
			TestContext ctx, Handler handler, Request request)
		{
		}

		public abstract Task<Response> Run (
			TestContext ctx, Request request,
			CancellationToken cancellationToken);

		public abstract HttpOperation RunSecondary (
			TestContext ctx, HttpClientTestRunner runner,
			CancellationToken cancellationToken);
	}
}
