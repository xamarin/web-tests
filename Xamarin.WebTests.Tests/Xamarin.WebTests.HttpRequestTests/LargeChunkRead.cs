//
// LargeChunkRead.cs
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
using System.Text;
using System.Collections.Generic;
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

	public class LargeChunkRead : CustomHandlerFixture
	{
		public override HttpServerTestCategory Category => HttpServerTestCategory.Default;

		public override HttpContent ExpectedContent => HttpContent.TheQuickBrownFox;

		public override bool HasRequestBody => false;

		public override bool CloseConnection => false;

		protected override void ConfigureRequest (
			TestContext ctx, Uri uri,
			CustomHandler handler, TraditionalRequest request)
		{
		}

		public override HttpResponse HandleRequest (
			TestContext ctx, HttpOperation operation,
			HttpRequest request, CustomHandler handler)
		{
			var chunks = new List<byte[]> ();
			chunks.Add (ConnectionHandler.TheQuickBrownFoxBuffer);
			chunks.Add (ConnectionHandler.GetLargeTextBuffer (50));
			var chunkedContent = new ChunkedContent (chunks);
			chunkedContent.WriteAsBlob = true;
			return new HttpResponse (HttpStatusCode.OK, chunkedContent) {
				WriteBodyAsBlob = true
			};
		}

		protected override async Task<HttpContent> ReadResponseBody (
			TestContext ctx, TraditionalRequest request,
			HttpWebResponse response, Stream stream,
			CancellationToken cancellationToken)
		{
			var buffer = new byte[43];
			var ret = await stream.ReadAsync (buffer, 0, buffer.Length).ConfigureAwait (false);
			ctx.Assert (ret, Is.EqualTo (ConnectionHandler.TheQuickBrownFox.Length), "#A1");
			var text = Encoding.UTF8.GetString (buffer, 0, ret);
			return new StringContent (text);
		}
	}
}
