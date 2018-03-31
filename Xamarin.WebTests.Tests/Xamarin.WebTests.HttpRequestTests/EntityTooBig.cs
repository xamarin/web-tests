//
// EntityTooBig.cs
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

namespace Xamarin.WebTests.HttpRequestTests
{
	using TestFramework;
	using TestAttributes;
	using HttpFramework;
	using HttpHandlers;
	using TestRunners;

	[HttpServerTestCategory (HttpServerTestCategory.NewWebStack)]
	public class EntityTooBig : RequestTestFixture
	{
		public override HttpStatusCode ExpectedStatus => HttpStatusCode.InternalServerError;

		public override WebExceptionStatus ExpectedError => WebExceptionStatus.AnyErrorStatus;

		public override HttpOperationFlags OperationFlags => HttpOperationFlags.AbortAfterClientExits | HttpOperationFlags.DontReadRequestBody;

		public override bool HasRequestBody => true;

		public override HttpContent ExpectedContent => throw new InvalidOperationException ();

		public override RequestFlags RequestFlags => RequestFlags.KeepAlive;

		protected override void ConfigureRequest (
			TestContext ctx, InstrumentationOperation operation,
			TraditionalRequest request)
		{
			request.Method = "POST";
			request.SetContentType ("text/plain");
			request.SetContentLength (16);
			base.ConfigureRequest (ctx, operation, request);
		}

		protected override async Task WriteRequestBody (
			TestContext ctx, TraditionalRequest request, Stream stream,
			CancellationToken cancellationToken)
		{
			await ctx.AssertException<ProtocolViolationException> (() =>
				stream.WriteAsync (ConnectionHandler.TheQuickBrownFoxBuffer, cancellationToken),
				"writing too many bytes").ConfigureAwait (false);
		}

		public override async Task<HttpResponse> HandleRequest (
			TestContext ctx, InstrumentationOperation operation,
			HttpConnection connection, HttpRequest request,
			RequestFlags effectiveFlags,
			CancellationToken cancellationToken)
		{
			await request.ReadHeaders (ctx, cancellationToken).ConfigureAwait (false);
			await ctx.AssertException<IOException> (() => request.Read (ctx, cancellationToken), "client doesn't send any body");
			return null;
		}
	}
}
