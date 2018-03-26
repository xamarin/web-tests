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

namespace Xamarin.WebTests.HttpRequestTests
{
	using HttpFramework;
	using HttpHandlers;
	using TestRunners;

	public abstract class CustomHandlerFixture : RequestTestFixture
	{
		public sealed override Handler CreateHandler (
			TestContext ctx, HttpRequestTestRunner runner)
		{
			return new CustomHandler (this, runner);
		}

		public sealed override Request CreateRequest (
			TestContext ctx, Uri uri, Handler handler)
		{
			return new CustomRequest ((CustomHandler)handler, uri);
		}

		public abstract bool CloseConnection {
			get;
		}

		public abstract bool HasRequestBody {
			get;
		}

		public virtual HttpContent ExpectedContent {
			get => new StringContent (FriendlyValue);
		}

		public sealed override void ConfigureRequest (
			TestContext ctx, Uri uri, Handler handler, Request request)
		{
			var customHandler = (CustomHandler)handler;
			var traditionalRequest = (TraditionalRequest)request;
			customHandler.ConfigureRequest (traditionalRequest);
			ConfigureRequest (ctx, uri, customHandler, traditionalRequest);
		}

		protected virtual void ConfigureRequest (
			TestContext ctx, Uri uri, CustomHandler handler,
			TraditionalRequest request)
		{
		}

		protected virtual Task<Response> SendRequest (
			TestContext ctx, TraditionalRequest request,
			CancellationToken cancellationToken)
		{
			return ((CustomRequest)request).DefaultSendAsync (ctx, cancellationToken);
		}

		protected virtual async Task WriteRequestBody (
			TestContext ctx, CustomHandler handler,
			TraditionalRequest request,
			CancellationToken cancellationToken)
		{
			using (var stream = await request.RequestExt.GetRequestStreamAsync ().ConfigureAwait (false)) {
				await WriteRequestBody (ctx, handler, request, stream, cancellationToken);
			}
		}

		protected virtual Task WriteRequestBody (
			TestContext ctx, CustomHandler handler,
			TraditionalRequest request, Stream stream,
			CancellationToken cancellationToken)
		{
			throw ctx.AssertFail ("Must override this.");
		}

		protected async virtual Task<TraditionalResponse> ReadResponse (
			TestContext ctx, TraditionalRequest request,
			CustomHandler handler, HttpWebResponse response,
			WebException error, CancellationToken cancellationToken)
		{
			cancellationToken.ThrowIfCancellationRequested ();

			HttpContent content;
			var status = response.StatusCode;

			using (var stream = response.GetResponseStream ()) {
				content = await ReadResponseBody (
					ctx, request, response, stream, cancellationToken).ConfigureAwait (false);
			}

			return new TraditionalResponse (request, response, content, error);
		}

		protected async virtual Task<HttpContent> ReadResponseBody (
			TestContext ctx, TraditionalRequest request,
			HttpWebResponse response, Stream stream,
			CancellationToken cancellationToken)
		{
			cancellationToken.ThrowIfCancellationRequested ();

			string body = null;
			using (var reader = new StreamReader (stream)) {
				if (!reader.EndOfStream)
					body = await reader.ReadToEndAsync ().ConfigureAwait (false);
			}

			return StringContent.CreateMaybeNull (body);
		}

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

		public virtual bool CheckResponse (
			TestContext ctx, TraditionalResponse response)
		{
			if (!ctx.Expect (response.Content, Is.Not.Null, "response.Content != null"))
				return false;

			return HttpContent.Compare (ctx, response.Content, ExpectedContent, false, "response.Content");
		}

		class CustomRequest : TraditionalRequest
		{
			public CustomHandler Handler {
				get;
			}

			public CustomRequest (CustomHandler handler, Uri uri)
				: base (uri)
			{
				Handler = handler;
			}

			public override bool HasContent => Handler.Fixture.HasRequestBody;

			internal Task<Response> DefaultSendAsync (TestContext ctx, CancellationToken cancellationToken)
			{
				return base.SendAsync (ctx, cancellationToken);
			}

			public override Task<Response> SendAsync (TestContext ctx, CancellationToken cancellationToken)
			{
				return Handler.Fixture.SendRequest (ctx, this, cancellationToken);
			}

			protected override Task WriteBody (
				TestContext ctx, CancellationToken cancellationToken)
			{
				return Handler.Fixture.WriteRequestBody (
					ctx, Handler, this, cancellationToken);
			}

			protected override Task<TraditionalResponse> GetResponseFromHttp (
				TestContext ctx, HttpWebResponse response,
				WebException error, CancellationToken cancellationToken)
			{
				return Handler.Fixture.ReadResponse (
					ctx, this, Handler, response, error, cancellationToken);
			}
		}
	}
}
