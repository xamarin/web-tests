//
// WritingBodyThrows.cs
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

	public class WritingBodyThrows : RequestTestFixture
	{
		public sealed override HttpContent ExpectedContent => throw new InvalidOperationException ();

		public override bool HasRequestBody => false;

		public override RequestFlags RequestFlags => RequestFlags.CloseConnection;

		public override HttpResponse HandleRequest (
			TestContext ctx, InstrumentationOperation operation,
			HttpConnection connection, HttpRequest request)
		{
			return new HttpResponse (HttpStatusCode.OK, new CustomContent ());
		}

		class CustomContent : HttpContent
		{
			public override bool HasLength => true;

			public override int Length => 16;

			public override void AddHeadersTo (HttpMessage message)
			{
				message.ContentType = "text/plain";
				message.ContentLength = 16;
			}

			public override byte[] AsByteArray ()
			{
				throw new NotImplementedException ();
			}

			public override string AsString ()
			{
				throw new NotImplementedException ();
			}

			public override Task WriteToAsync (
				TestContext ctx, Stream stream,
				CancellationToken cancellationToken)
			{
				throw new NotImplementedException ("Intentionally throwing here.");
			}
		}
	}
}
