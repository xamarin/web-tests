//
// TestResponseStream.cs
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
	using TestFramework;
	using HttpFramework;
	using HttpHandlers;
	using TestRunners;

	public class TestResponseStream : RequestTestFixture
	{
		public override RequestFlags RequestFlags => RequestFlags.CloseConnection;

		public override HttpContent ExpectedContent => new StringContent ("AAAA");

		public override bool HasRequestBody => false;

		public override HttpResponse HandleRequest (
			TestContext ctx, InstrumentationOperation operation,
			HttpConnection connection, HttpRequest request)
		{
			return new HttpResponse (HttpStatusCode.OK, ExpectedContent) {
				WriteAsBlob = true
			};
		}

		protected override async Task<HttpContent> ReadResponseBody (
			TestContext ctx, TraditionalRequest request,
			HttpWebResponse response, Stream stream,
			CancellationToken cancellationToken)
		{
			var buffer = new byte[5];
			var ret = await stream.ReadAsync (buffer, 4, 1).ConfigureAwait (false);
			ctx.Assert (ret, Is.EqualTo (1), "#A1");
			ctx.Assert (buffer[4], Is.EqualTo ((byte)65), "#A2");
			ret = await stream.ReadAsync (buffer, 0, 2);
			ctx.Assert (ret, Is.EqualTo (2), "#B1");
			return ExpectedContent;
		}
	}
}
