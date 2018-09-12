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
	using TestAttributes;
	using HttpFramework;
	using HttpHandlers;
	using TestRunners;

	// .NET does not support read/write timeouts on HttpWebRequest.
	[HttpServerFlags (HttpServerFlags.RequireMono | HttpServerFlags.RequireNewWebStack)]
	public class ReadTimeout : RequestTestFixture
	{
		public override HttpStatusCode ExpectedStatus => HttpStatusCode.InternalServerError;

		public override WebExceptionStatus ExpectedError => WebExceptionStatus.Timeout;

		public override bool HasRequestBody => false;

		public override RequestFlags RequestFlags => RequestFlags.KeepAlive;

		TaskCompletionSource<bool> finishedTcs;

		protected override void ConfigureRequest (
			TestContext ctx, InstrumentationOperation operation,
			TraditionalRequest request)
		{
			finishedTcs = new TaskCompletionSource<bool> ();
			request.RequestExt.ReadWriteTimeout = 100;
			base.ConfigureRequest (ctx, operation, request);
		}

		public override HttpResponse HandleRequest(
			TestContext ctx, InstrumentationOperation operation,
			HttpConnection connection, HttpRequest request)
		{
			var content = new CustomContent (this);
			return new HttpResponse (HttpStatusCode.OK, content);
		}

		protected override Task<TraditionalResponse> ReadResponse (
			TestContext ctx, TraditionalRequest request,
			HttpWebResponse response, WebException error,
			CancellationToken cancellationToken)
		{
			return ReadWithTimeout (
				ctx, request, response, 5000, WebExceptionStatus.Timeout);
		}

		async Task<TraditionalResponse> ReadWithTimeout (
			TestContext ctx, TraditionalRequest request,
			HttpWebResponse response, int timeout,
			WebExceptionStatus expectedStatus)
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
				finishedTcs.TrySetResult (true);
			}
		}

		class CustomContent : HttpContent
		{
			public ReadTimeout Parent {
				get;
			}

			public CustomContent (ReadTimeout parent)
			{
				Parent = parent;
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
				await Task.WhenAny (Parent.finishedTcs.Task, Task.Delay (10000));
			}
		}
	}
}
