//
// ChunkSizeWithLeadingZero.cs
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

namespace Xamarin.WebTests.HttpClientTests
{
	using TestFramework;
	using TestAttributes;
	using HttpFramework;
	using HttpHandlers;
	using TestRunners;

	[HttpServerTestCategory (HttpServerTestCategory.Default)]
	public class ChunkSizeWithLeadingZero : HttpClientTestFixture
	{
		public HttpContent Content {
			get;
		}

		public HttpContent ReturnContent {
			get;
		}

		public ChunkSizeWithLeadingZero ()
		{
			Content = HttpContent.HelloWorld;
			ReturnContent = new Bug20583Content ();
		}

		public override Handler CreateHandler (TestContext ctx)
		{
			// Bug 20583
			return new PostHandler (ME, Content) {
				ReturnContent = ReturnContent
			};
		}

		protected override void ConfigureRequest (
			TestContext ctx, InstrumentationOperation operation,
			Request request, Uri uri)
		{
			request.Content = Content;
			base.ConfigureRequest (ctx, operation, request, uri);
		}

		public override Task<Response> Run (
			TestContext ctx, HttpClientRequest request,
			CancellationToken cancellationToken)
		{
			return request.PostString (ctx, cancellationToken);
		}

		class Bug20583Content : HttpContent
		{
			#region implemented abstract members of HttpContent
			public override bool HasLength => false;
			public override int Length => throw new InvalidOperationException ();
			public override string AsString ()
			{
				return "AAAA";
			}
			public override byte[] AsByteArray ()
			{
				throw new NotSupportedException ();
			}
			public override void AddHeadersTo (HttpMessage message)
			{
				message.TransferEncoding = "chunked";
				message.ContentType = "text/plain";
			}
			public override async Task WriteToAsync (
				TestContext ctx, Stream stream,
				CancellationToken cancellationToken)
			{
				await Task.Delay (500).ConfigureAwait (false);
				await stream.WriteAsync ("0");
				await Task.Delay (500);
				await stream.WriteAsync ("4\r\n");
				await stream.WriteAsync ("AAAA\r\n0\r\n\r\n");
			}
			#endregion
		}
	}
}
