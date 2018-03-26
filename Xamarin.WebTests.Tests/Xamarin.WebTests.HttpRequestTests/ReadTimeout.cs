//
// ReadTimeout.cs
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

	public class ReadTimeout : CustomHandlerFixture
	{
		public override HttpServerTestCategory Category => HttpServerTestCategory.NewWebStack;

		public override HttpStatusCode ExpectedStatus => HttpStatusCode.InternalServerError;

		public override WebExceptionStatus ExpectedError => WebExceptionStatus.Timeout;

		public override bool HasRequestBody => false;

		public override bool CloseConnection => false;

		protected override void ConfigureRequest (
			TestContext ctx, Uri uri, CustomHandler handler,
			TraditionalRequest request)
		{
			request.RequestExt.ReadWriteTimeout = 100;
			base.ConfigureRequest (ctx, uri, handler, request);
		}

		public override HttpResponse HandleRequest(
			TestContext ctx, HttpOperation operation,
			HttpRequest request, CustomHandler handler)
		{
			var content = new CustomContent (this, handler);
			return new HttpResponse (HttpStatusCode.OK, content);
		}

		protected override Task<TraditionalResponse> ReadResponse (
			TestContext ctx, TraditionalRequest request,
			CustomHandler handler, HttpWebResponse response,
			WebException error, CancellationToken cancellationToken)
		{
			return ReadWithTimeout (
				ctx, request, handler, response,
				5000, WebExceptionStatus.Timeout);
		}

		async Task<TraditionalResponse> ReadWithTimeout (
			TestContext ctx, TraditionalRequest request,
			CustomHandler handler, HttpWebResponse response,
			int timeout, WebExceptionStatus expectedStatus)
		{
			StreamReader reader = null;
			try
			{
				reader = new StreamReader (response.GetResponseStream ());
				var readTask = reader.ReadToEndAsync();
				if (timeout > 0) {
					var timeoutTask = Task.Delay (timeout);
					var task = await Task.WhenAny (timeoutTask, readTask).ConfigureAwait (false);
					if (task == timeoutTask)
						throw ctx.AssertFail ("Timeout expired.");
				}
				var ret = await readTask.ConfigureAwait (false);
				ctx.LogMessage ($"EXPECTED ERROR: {ret}");
				throw ctx.AssertFail ("Expected exception.");
			} catch (WebException wexc) {
				ctx.Assert ((WebExceptionStatus)wexc.Status, Is.EqualTo (expectedStatus));
				return new TraditionalResponse (request, HttpStatusCode.InternalServerError, wexc);
			} finally {
				handler.SetCompleted ();
			}
		}

		public override bool CheckResponse (
			TestContext ctx, TraditionalResponse response)
		{
			return ctx.Expect (response.Status, Is.EqualTo (HttpStatusCode.OK), "response.StatusCode");
		}

		class CustomContent : HttpContent
		{
			public ReadTimeout Parent {
				get;
			}

			public CustomHandler Handler {
				get;
			}

			public CustomContent (ReadTimeout parent, CustomHandler handler)
			{
				Parent = parent;
				Handler = handler;
			}

			public override bool HasLength => true;

			public override int Length => 4096;

			public override void AddHeadersTo (HttpMessage message)
			{
				message.ContentType = "text/plain";
				message.ContentLength = Length;
			}

			public override byte[] AsByteArray ()
			{
				throw new NotImplementedException ();
			}

			public override string AsString ()
			{
				throw new NotImplementedException ();
			}

			public override async Task WriteToAsync (
				TestContext ctx, Stream stream,
				CancellationToken cancellationToken)
			{
				await stream.WriteAsync (ConnectionHandler.TheQuickBrownFoxBuffer, cancellationToken).ConfigureAwait (false);
				await stream.FlushAsync (cancellationToken);
				await Task.WhenAny (Handler.WaitForCompletion (), Task.Delay (10000));
			}
		}
	}
}
