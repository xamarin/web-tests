//
// PostContentLength.cs
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
using Xamarin.AsyncTests.Constraints;

namespace Xamarin.WebTests.HttpRequestTests
{
	using ConnectionFramework;
	using TestFramework;
	using HttpFramework;
	using HttpHandlers;
	using System.IO;

	public class PostContentLength : CustomHandlerFixture
	{
		public override HttpServerTestCategory Category => HttpServerTestCategory.Default;

		public override HttpOperationFlags OperationFlags => HttpOperationFlags.DontReadRequestBody;

		public override bool HasRequestBody => true;

		public override HttpContent ExpectedContent => new StringContent (ME);

		byte[] ContentBuffer => ConnectionHandler.TheQuickBrownFoxBuffer;

		public override bool CloseConnection => true;

		protected override void ConfigureRequest (
			TestContext ctx, Uri uri,
			CustomHandler handler, TraditionalRequest request)
		{
			request.Method = "POST";
			request.SetContentType ("text/plain");
			request.SetContentLength (ContentBuffer.Length);

			base.ConfigureRequest (ctx, uri, handler, request);
		}

		protected override async Task WriteRequestBody (
			TestContext ctx, CustomHandler handler,
			TraditionalRequest request, Stream stream,
			CancellationToken cancellationToken)
		{
			await AbstractConnection.WaitWithTimeout (ctx, 1500, handler.WaitUntilReady ()).ConfigureAwait (false);
			await stream.WriteAsync (ContentBuffer, cancellationToken);
			await stream.FlushAsync (cancellationToken);
		}

		public override async Task<HttpResponse> HandleRequest (
			TestContext ctx, HttpOperation operation,
			HttpConnection connection, HttpRequest request,
			CustomHandler handler, CancellationToken cancellationToken)
		{
			await request.ReadHeaders (ctx, cancellationToken).ConfigureAwait (false);
			ctx.Assert (request.ContentLength, Is.EqualTo (ContentBuffer.Length), "request.ContentLength");
			handler.SetReady ();
			await request.Read (ctx, cancellationToken);
			return new HttpResponse (HttpStatusCode.OK, ExpectedContent);
		}
	}
}
