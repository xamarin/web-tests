//
// CustomHandlerFixture.cs
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
	using HttpFramework;
	using HttpHandlers;
	using TestRunners;

	public abstract class CustomHandlerFixture : HttpClientTestFixture
	{
		public sealed override Handler CreateHandler (
			TestContext ctx, HttpClientTestRunner runner, bool primary)
		{
			return new CustomHandler (this, runner, primary);
		}

		public virtual HttpContent ExpectedContent {
			get => new StringContent (FriendlyValue);
		}

		CustomRequest primaryRequest;

		public sealed override Request CreateRequest (
			TestContext ctx, Uri uri, Handler handler)
		{
			var customHandler = (CustomHandler)handler;
			if (!customHandler.IsPrimary && ReusingHandler) {
				ctx.Assert (primaryRequest, Is.Not.Null, "has primary request");
				return new CustomRequest (customHandler, uri, primaryRequest);
			}
			var request = new CustomRequest (customHandler, uri);
			if (customHandler.IsPrimary) {
				var oldRequest = Interlocked.CompareExchange (ref primaryRequest, request, null);
				ctx.Assert (oldRequest, Is.Null, "duplicate request");
			}
			return request;
		}

		protected CustomRequest PrimaryRequest => primaryRequest;

		protected virtual bool ReusingHandler => false;

		public virtual bool CloseConnection => false;

		public sealed override void ConfigureRequest (
			TestContext ctx, Handler handler, Request request)
		{
			var customHandler = (CustomHandler)handler;
			var customRequest = (CustomRequest)request;
			ConfigureRequest (ctx, customHandler, customRequest);
		}

		protected virtual void ConfigureRequest (
			TestContext ctx, CustomHandler handler,
			CustomRequest request)
		{
		}

		public sealed override Task<Response> Run (
			TestContext ctx, Request request,
			CancellationToken cancellationToken)
		{
			return Run (ctx, (CustomRequest)request, cancellationToken);
		}

		public override HttpOperation RunSecondary (
			TestContext ctx, HttpClientTestRunner runner,
			CancellationToken cancellationToken)
		{
			return null;
		}

		protected abstract Task<Response> Run (
			TestContext ctx, CustomRequest request,
			CancellationToken cancellationToken);

		public virtual Task<HttpResponse> HandleRequest (
			TestContext ctx, HttpOperation operation,
			HttpConnection connection, HttpRequest request,
			CustomHandler handler, CancellationToken cancellationToken)
		{
			return Task.Run (() => HandleRequest (ctx, operation, request, handler));
		}

		public virtual HttpResponse HandleRequest (
			TestContext ctx, HttpOperation operation,
			HttpRequest request, CustomHandler handler)
		{
			throw ctx.AssertFail ("Must override this.");
		}

		public virtual bool CheckResponse (TestContext ctx, Response response)
		{
			if (!ctx.Expect (response.Content, Is.Not.Null, "response.Content != null"))
				return false;

			return HttpContent.Compare (ctx, response.Content, ExpectedContent, false, "response.Content");
		}
	}
}
