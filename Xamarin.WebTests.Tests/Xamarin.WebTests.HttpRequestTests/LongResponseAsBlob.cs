//
// LongResponseAsBlob.cs
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
	using TestAttributes;
	using TestFramework;
	using HttpFramework;
	using HttpHandlers;
	using TestRunners;

	[RecentlyFixed]
	public class LongResponseAsBlob : RequestTestFixture
	{
		public override bool HasRequestBody => false;

		public sealed override HttpContent ExpectedContent {
			get;
		}

		public override RequestFlags RequestFlags => RequestFlags.KeepAlive;

		public LongResponseAsBlob ()
		{
			ExpectedContent = new MyContent ();
		}

		protected override void ConfigureRequest (TestContext ctx, InstrumentationOperation operation, TraditionalRequest request)
		{
			request.RequestExt.Timeout = 2500;
			base.ConfigureRequest (ctx, operation, request);
		}

		public override HttpResponse HandleRequest (
			TestContext ctx, InstrumentationOperation operation,
			HttpConnection connection, HttpRequest request)
		{
			return new HttpResponse (HttpStatusCode.OK, ExpectedContent) {
				WriteAsBlob = true, WriteBodyAsBlob = true
			};
		}

		public override bool CheckResponse (TestContext ctx, Response response)
		{
			if (!ctx.Expect (response.Content, Is.Not.Null, "response.Content != null"))
				return false;

			return HttpContent.Compare (ctx, response.Content, ExpectedContent, true, "response.Content");
		}

		class MyContent : HttpContent
		{
			public sealed override bool HasLength => true;

			/*
			 * This requires delicate measuring.
			 * 
			 * - The total length of our response should be around 4084 bytes.
			 * - The HTTP Status line is 17 bytes long.
			 * - The difference between ContentLength and 4096 must be less than the
			 *   lenght of the HTTP Status line.
			 * 
			 */
			public sealed override int Length => 3887;

			readonly byte[] buffer;
			readonly string text;

			public MyContent ()
			{
				var input = ConnectionHandler.TheQuickBrownFoxBuffer;
				buffer = new byte[Length];
				var pos = 0;
				while (pos < Length) {
					var remaining = Math.Min (Length - pos, input.Length);
					Buffer.BlockCopy (input, 0, buffer, pos, remaining);
					pos += remaining;
				}
				text = Encoding.UTF8.GetString (buffer, 0, buffer.Length);
			}

			public override void AddHeadersTo (HttpMessage message)
			{
				message.ContentType = "application/json";
				message.ContentLength = Length;
				message.AddHeader ("One", ConnectionHandler.TheQuickBrownFox);
				message.AddHeader ("Two", ConnectionHandler.TheQuickBrownFox);
			}

			public override byte[] AsByteArray () => buffer;

			public override string AsString () => text;

			public override Task WriteToAsync (TestContext ctx, Stream stream, CancellationToken cancellationToken)
			{
				return stream.WriteAsync (buffer, cancellationToken);
			}
		}
	}
}
