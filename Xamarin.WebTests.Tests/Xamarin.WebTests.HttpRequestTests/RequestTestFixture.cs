//
// RequestTestFixture.cs
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

namespace Xamarin.WebTests.HttpRequestTests
{
	using TestFramework;
	using TestAttributes;
	using HttpFramework;
	using HttpHandlers;
	using TestRunners;

	[Work]
	[AsyncTestFixture (Prefix = "HttpRequestTests")]
	public abstract class RequestTestFixture : IHttpRequestTestInstance
	{
		[FixtureParameter]
		public abstract HttpServerTestCategory Category {
			get;
		}

		public string Value => GetType ().FullName;

		public string FriendlyValue => GetType ().Name;

		protected string ME => FriendlyValue;

		[AsyncTest]
		public Task Run (TestContext ctx, CancellationToken cancellationToken,
				 HttpServerProvider provider,
				 HttpRequestTestRunner runner)
		{
			return runner.Run (ctx, cancellationToken);
		}

		[AsyncTest]
		[Martin (null, UseFixtureName = true)]
		[HttpServerTestCategory (HttpServerTestCategory.MartinTest)]
		public Task MartinTest (TestContext ctx, CancellationToken cancellationToken,
		                        HttpServerProvider provider,
		                        HttpRequestTestRunner runner)
		{
			return runner.Run (ctx, cancellationToken);
		}

		public virtual HttpStatusCode ExpectedStatus => HttpStatusCode.OK;

		public virtual WebExceptionStatus ExpectedError => WebExceptionStatus.Success;

		public virtual HttpOperationFlags OperationFlags => HttpOperationFlags.None;

		public abstract Handler CreateHandler (
			TestContext ctx, HttpRequestTestRunner runner);

		public abstract Request CreateRequest (
			TestContext ctx, Uri uri, Handler handler);

		public abstract void ConfigureRequest (
			TestContext ctx, Uri uri, Handler handler, Request request);
	}
}
