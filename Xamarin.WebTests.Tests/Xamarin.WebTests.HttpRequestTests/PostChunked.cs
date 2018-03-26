//
// PostChunked.cs
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
	using ConnectionFramework;
	using TestFramework;
	using HttpFramework;
	using HttpHandlers;

	public class PostChunked : CustomHandlerFixture
	{
		public override HttpServerTestCategory Category => HttpServerTestCategory.NewWebStack;

		public override bool HasRequestBody => true;

		public override HttpContent ExpectedContent => new StringContent (ME);

		public override HttpOperationFlags OperationFlags => HttpOperationFlags.DontReadRequestBody;

		public override bool CloseConnection => false;

		byte[] ContentBuffer => ConnectionHandler.TheQuickBrownFoxBuffer;

		protected override void ConfigureRequest (
			TestContext ctx, Uri uri,
			CustomHandler handler, TraditionalRequest request)
		{
			request.Method = "POST";
			request.SetContentType ("text/plain");
			request.SetContentLength (4096);
			request.SendChunked ();
		}

		protected override async Task WriteRequestBody (
			TestContext ctx, CustomHandler handler,
			TraditionalRequest request, Stream stream,
			CancellationToken cancellationToken)
		{
			await stream.WriteAsync (ContentBuffer, cancellationToken).ConfigureAwait (false);
			await stream.FlushAsync (cancellationToken);

			await AbstractConnection.WaitWithTimeout (ctx, 1500, handler.WaitUntilReady ());

			await stream.WriteAsync (ConnectionHandler.GetLargeTextBuffer (50), cancellationToken);
		}

		public async override Task<HttpResponse> HandleRequest (
			TestContext ctx, HttpOperation operation,
			HttpConnection connection, HttpRequest request,
			CustomHandler handler, CancellationToken cancellationToken)
		{
			var me = $"{ME}.{nameof (HandleRequest)}";
			ctx.LogDebug (3, $"{me}: {connection.RemoteEndPoint}");

			var firstChunk = await ChunkedContent.ReadChunk (ctx, request.Reader, cancellationToken).ConfigureAwait (false);
			ctx.LogDebug (3, $"{me} got first chunk: {firstChunk.Length}");

			ctx.Assert (firstChunk, Is.EqualTo (ConnectionHandler.TheQuickBrownFoxBuffer), "first chunk");

			handler.SetReady ();

			ctx.LogDebug (3, $"{me} reading remaining body");

			await ChunkedContent.Read (ctx, request.Reader, cancellationToken).ConfigureAwait (false);

			return new HttpResponse (HttpStatusCode.OK, ExpectedContent);
		}
	}
}
