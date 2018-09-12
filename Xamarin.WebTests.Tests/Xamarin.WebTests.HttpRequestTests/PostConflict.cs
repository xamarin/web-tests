//
// PostConflict.cs
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
using Xamarin.AsyncTests.Constraints;

namespace Xamarin.WebTests.HttpRequestTests
{
	using TestFramework;
	using TestAttributes;
	using HttpFramework;
	using HttpHandlers;
	using TestRunners;

	[RecentlyFixed]
	public class PostConflict : RequestTestFixture
	{
		public override bool HasRequestBody => true;

		HttpContent RequestContent => ConnectionHandler.TheQuickBrownFoxContent;

		HttpContent ReturnContent => new ChunkedContent (ConnectionHandler.TheQuickBrownFox);

		public override HttpContent ExpectedContent => ReturnContent;

		public override RequestFlags RequestFlags => RequestFlags.KeepAlive;

		public override HttpStatusCode ExpectedStatus => HttpStatusCode.Conflict;

		public override WebExceptionStatus ExpectedError => WebExceptionStatus.ProtocolError;

		protected override void ConfigureRequest (
			TestContext ctx, InstrumentationOperation operation,
			TraditionalRequest request)
		{
			request.Method = "POST";
			request.SetContentType ("text/plain");
			request.SetContentLength (RequestContent.Length);

			base.ConfigureRequest (ctx, operation, request);
		}

		protected override async Task WriteRequestBody (
			TestContext ctx, TraditionalRequest request,
			CancellationToken cancellationToken)
		{
			using (var stream = await request.RequestExt.GetRequestStreamAsync ().ConfigureAwait (false)) {
				await RequestContent.WriteToAsync (ctx, stream, cancellationToken).ConfigureAwait (false);
				await stream.FlushAsync ();
			}
		}

		public override HttpResponse HandleRequest (
			TestContext ctx, InstrumentationOperation operation,
			HttpConnection connection, HttpRequest request)
		{
			return new HttpResponse (HttpStatusCode.Conflict, ReturnContent) {
				WriteAsBlob = true
			};
		}

		public override bool CheckResponse (TestContext ctx, Response response)
		{
			ctx.Assert (response.Error, Is.InstanceOf<WebException> (), "WebException");
			var wexc = (WebException)response.Error;
			ctx.Assert (wexc.Response, Is.Not.Null, "Response");

			ctx.Assert (response.Content, Is.Not.Null, "Response.Content");

			var expectedContent = ReturnContent.RemoveTransferEncoding ();
			return HttpContent.Compare (ctx, response.Content, expectedContent, false, "Response.Content");
		}
	}
}
