//
// ResponseStreamCheckLength.cs
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
using System.Threading;
using System.Threading.Tasks;
using Xamarin.AsyncTests;
using Xamarin.AsyncTests.Constraints;

namespace Xamarin.WebTests.HttpRequestTests
{
	using TestFramework;
	using TestAttributes;
	using HttpFramework;
	using HttpHandlers;
	using TestRunners;

	[HttpServerTestCategory (HttpServerTestCategory.GZip)]
	public class ResponseStreamCheckLength : RequestTestFixture
	{
		public bool UseChunkedEncoding {
			get; set;
		}

		HttpContent Content => UseChunkedEncoding ?
			HttpContent.HelloChunked : HttpContent.HelloWorld;

		public sealed override HttpContent ExpectedContent => Content.RemoveTransferEncoding ();

		public override bool HasRequestBody => false;

		public override RequestFlags RequestFlags => RequestFlags.KeepAlive;

		protected override void ConfigureRequest (
			TestContext ctx, InstrumentationOperation operation,
			TraditionalRequest request)
		{
			request.RequestExt.AutomaticDecompression = true;
			base.ConfigureRequest (ctx, operation, request);
		}

		public override HttpResponse HandleRequest (
			TestContext ctx, InstrumentationOperation operation,
			HttpConnection connection, HttpRequest request)
		{
			return new HttpResponse (HttpStatusCode.OK, Content);
		}

		protected override async Task<HttpContent> ReadResponseBody (
			TestContext ctx, TraditionalRequest request,
			HttpWebResponse response, Stream stream,
			CancellationToken cancellationToken)
		{
			await ctx.AssertException<NotSupportedException> (() => Task.FromResult (stream.Length), "Length should throw");
			if (UseChunkedEncoding) {
				ctx.Assert (response.ContentLength, Is.EqualTo (-1L), "ContentLength");
				ctx.Assert (response.Headers["Transfer-Encoding"], Is.EqualTo ("chunked"), "chunked encoding");
			} else {
				ctx.Assert (response.ContentLength, Is.EqualTo ((long)Content.Length), "ContentLength");
				ctx.Assert (response.Headers["Content-Length"], Is.EqualTo (Content.Length.ToString ()), "Content-Length header");
			}
			using (var ms = new MemoryStream ()) {
				await stream.CopyToAsync (ms, 16384).ConfigureAwait (false);
				var bytes = ms.ToArray ();
				var text = Encoding.UTF8.GetString (bytes, 0, bytes.Length);
				return new StringContent (text);
			}
		}
	}
}
