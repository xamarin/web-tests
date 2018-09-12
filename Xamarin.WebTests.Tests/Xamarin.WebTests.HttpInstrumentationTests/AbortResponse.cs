//
// AbortResponse.cs
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

namespace Xamarin.WebTests.HttpInstrumentationTests
{
	using ConnectionFramework;
	using TestAttributes;
	using TestFramework;
	using HttpFramework;
	using HttpHandlers;
	using TestRunners;

	public class AbortResponse : HttpInstrumentationTestFixture
	{
		public override HttpStatusCode ExpectedStatus => HttpStatusCode.InternalServerError;

		public override WebExceptionStatus ExpectedError => WebExceptionStatus.RequestCanceled;

		ServicePoint servicePoint;
		TraditionalRequest primaryRequest;
		TaskCompletionSource<bool> finishedTcs;

		protected override Request CreateRequest (
			TestContext ctx, InstrumentationOperation operation, Uri uri)
		{
			return new InstrumentationRequest (this, uri);
		}

		protected override void ConfigurePrimaryRequest (
			TestContext ctx, InstrumentationOperation operation,
			TraditionalRequest request)
		{
			ctx.Assert (Interlocked.CompareExchange (ref primaryRequest, request, null), Is.Null);
			finishedTcs = new TaskCompletionSource<bool> ();

			ctx.Assert (servicePoint, Is.Null, "ServicePoint");
			servicePoint = ServicePointManager.FindServicePoint (request.Uri);
			base.ConfigurePrimaryRequest (ctx, operation, request);
		}

		protected override bool ConfigureNetworkStream (
			TestContext ctx, StreamInstrumentation instrumentation)
		{
			return false;
		}

		public override HttpResponse HandleRequest (
			TestContext ctx, InstrumentationOperation operation,
			HttpConnection connection, HttpRequest request)
		{
			var content = new MyContent (this);
			return new HttpResponse (HttpStatusCode.OK, content);
		}

		protected override Task<TraditionalResponse> ReadResponse (
			TestContext ctx, TraditionalRequest request,
			HttpWebResponse response, WebException error,
			CancellationToken cancellationToken)
		{
			return ReadWithTimeout (ctx, request, response, 0, WebExceptionStatus.RequestCanceled);
		}

		async Task<TraditionalResponse> ReadWithTimeout (
			TestContext ctx, TraditionalRequest request,
			WebResponse response, int timeout,
			WebExceptionStatus expectedStatus)
		{
			StreamReader reader = null;
			try {
				reader = new StreamReader (response.GetResponseStream ());
				var readTask = reader.ReadToEndAsync ();
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

		async Task HandleWriteBody (
			TestContext ctx, Stream stream,
			CancellationToken cancellationToken)
		{
			await stream.WriteAsync (ConnectionHandler.TheQuickBrownFoxBuffer, cancellationToken).ConfigureAwait (false);
			await stream.FlushAsync (cancellationToken);
			await Task.Delay (500).ConfigureAwait (false);
			PrimaryOperation.Request.Abort ();
			await Task.WhenAny (finishedTcs.Task, Task.Delay (10000));
		}

		class MyContent : HttpContent
		{
			public AbortResponse Parent {
				get;
			}

			public override bool HasLength => true;

			public override int Length => 4096;

			public MyContent (AbortResponse parent)
			{
				Parent = parent;
			}

			public override string AsString ()
			{
				throw new NotImplementedException ();
			}

			public override byte[] AsByteArray ()
			{
				throw new NotImplementedException ();
			}

			public override void AddHeadersTo (HttpMessage message)
			{
				message.ContentType = "text/plain";
				message.ContentLength = Length;
			}

			public override Task WriteToAsync (
				TestContext ctx, Stream stream,
				CancellationToken cancellationToken)
			{
				return Parent.HandleWriteBody (
					ctx, stream, cancellationToken);
			}
		}
	}
}
