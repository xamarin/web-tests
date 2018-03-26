//
// LargeGZipRead.cs
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
	using HttpFramework;
	using HttpHandlers;

	public class LargeGZipRead : CustomHandlerFixture
	{
		public override HttpServerTestCategory Category => HttpServerTestCategory.GZip;

		HttpContent Content {
			get;
		}

		public sealed override HttpContent ExpectedContent {
			get;
		}

		public override bool HasRequestBody => false;

		public LargeGZipRead ()
		{
			Content = ConnectionHandler.GetLargeChunkedContent (16384);
			ExpectedContent = Content.RemoveTransferEncoding ();
		}

		public override bool CloseConnection => false;

		protected override void ConfigureRequest (
			TestContext ctx, Uri uri, CustomHandler handler,
			TraditionalRequest request)
		{
			request.RequestExt.AutomaticDecompression = true;
			base.ConfigureRequest (ctx, uri, handler, request);
		}

		public override HttpResponse HandleRequest (
			TestContext ctx, HttpOperation operation,
			HttpRequest request, CustomHandler handler)
		{
			var gzipContent = new GZipContent ((ChunkedContent)Content);
			return new HttpResponse (HttpStatusCode.OK, gzipContent);
		}
	}
}
