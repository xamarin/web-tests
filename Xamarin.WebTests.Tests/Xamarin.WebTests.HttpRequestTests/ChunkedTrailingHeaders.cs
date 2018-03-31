//
// ChunkedTrailingHeaders.cs
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

	[HttpServerTestCategory (HttpServerTestCategory.Ignore)]
	public class ChunkedTrailingHeaders : RequestTestFixture
	{
		HttpContent Content {
			get;
		}

		public sealed override HttpContent ExpectedContent {
			get;
		}

		public ChunkedTrailingHeaders ()
		{
			var chunkedContent = new ChunkedContent (ConnectionHandler.TheQuickBrownFox);
			chunkedContent.AddExtraHeader ("Foo", "Bar");
			Content = chunkedContent;
			ExpectedContent = Content.RemoveTransferEncoding ();
		}

		public override bool HasRequestBody => false;

		public override RequestFlags RequestFlags => RequestFlags.CloseConnection;

		public override HttpResponse HandleRequest (
			TestContext ctx, InstrumentationOperation operation,
			HttpConnection connection, HttpRequest request)
		{
			return new HttpResponse (HttpStatusCode.OK, Content);
		}

		public override bool CheckResponse (TestContext ctx, Response response)
		{
			var traditionalResponse = (TraditionalResponse)response;
			var headers = traditionalResponse.Response.Headers;
			if (!ctx.Expect (headers["Foo"], Is.EqualTo ("Bar"), "Foo header"))
				return false;
			return base.CheckResponse (ctx, response);
		}
	}
}
