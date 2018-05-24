//
// SendAsync.cs
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

namespace Xamarin.WebTests.HttpClientTests
{
	using TestFramework;
	using TestAttributes;
	using HttpFramework;
	using HttpHandlers;
	using TestRunners;

	public class SendAsync : HttpClientTestFixture
	{
		public SendAsyncType Type {
			get;
		}

		public HttpContent Content {
			get;
		}

		[AsyncTest]
		public SendAsync (SendAsyncType type)
		{
			Type = type;

			switch (type) {
			case SendAsyncType.PutChunked:
				Content = ConnectionHandler.GetLargeChunkedContent (50);
				break;
			}
		}

		public enum SendAsyncType {
			SendAsyncGet,
			SendAsyncHead,
			SendAsyncEmptyBody,
			// Bug 31830. Fixed in PR #6059 / #6068.
			SendAsyncObscureVerb,
			PutChunked
		}

		public override Handler CreateHandler (TestContext ctx)
		{
			switch (Type) {
			case SendAsyncType.SendAsyncEmptyBody:
				return new PostHandler (ME, null, TransferMode.ContentLength);
			case SendAsyncType.SendAsyncObscureVerb:
				return new PostHandler (ME, null, TransferMode.ContentLength) { Method = "EXECUTE" };
			case SendAsyncType.SendAsyncGet:
				return new PostHandler (ME, null) { Method = "GET" };
			case SendAsyncType.SendAsyncHead:
				return new PostHandler (ME, null) { Method = "HEAD" };
			case SendAsyncType.PutChunked:
				return new PostHandler (ME, Content, TransferMode.Chunked) {
					Method = "PUT"
				};
			default:
				throw ctx.AssertFail (Type);
			}
		}

		protected override void ConfigureRequest (
			TestContext ctx, InstrumentationOperation operation,
			Request request, Uri uri)
		{
			if (Type == SendAsyncType.PutChunked) {
				request.Content = Content.RemoveTransferEncoding ();
				request.SendChunked ();
			}
			base.ConfigureRequest (ctx, operation, request, uri);
		}

		public override Task<Response> Run (
			TestContext ctx, HttpClientRequest request,
			CancellationToken cancellationToken)
		{
			return request.SendAsync (ctx, cancellationToken);
		}
	}
}
