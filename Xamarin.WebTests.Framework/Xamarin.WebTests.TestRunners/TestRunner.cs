//
// HttpTest.cs
//
// Author:
//       Martin Baulig <martin.baulig@xamarin.com>
//
// Copyright (c) 2014 Xamarin Inc. (http://www.xamarin.com)
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
using System.Text;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;
using Xamarin.AsyncTests;
using Xamarin.AsyncTests.Constraints;

namespace Xamarin.WebTests.TestRunners
{
	using HttpFramework;
	using HttpHandlers;

	public abstract class TestRunner
	{
		public HttpServer Server {
			get;
			private set;
		}

		public Handler Handler {
			get;
			private set;
		}

		public RedirectHandler Redirect {
			get;
			private set;
		}

		protected virtual string Name {
			get { return string.Format ("{0}:{1}", Server, Handler); }
		}

		protected TestRunner (HttpServer server, Handler handler, RedirectHandler redirect = null)
		{
			Server = server;
			Handler = handler;
			Redirect = redirect;
		}

		protected void Debug (TestContext ctx, int level, string message, params object[] args)
		{
			var sb = new StringBuilder ();
			sb.AppendFormat ("{0}: {1}", Name, message);
			for (int i = 0; i < args.Length; i++) {
				sb.Append (" ");
				sb.Append (args [i] != null ? args [i].ToString () : "<null>");
			}

			ctx.LogDebug (level, sb.ToString ());
		}

		protected abstract Request CreateRequest (TestContext ctx, Uri uri);

		protected virtual void ConfigureRequest (TestContext ctx, Uri uri, Request request)
		{
			Handler.ConfigureRequest (request, uri);

			request.SetProxy (Server.GetProxy ());
		}

		protected abstract Task<Response> RunInner (TestContext ctx, CancellationToken cancellationToken, Request request);

		public Task Run (
			TestContext ctx, CancellationToken cancellationToken,
			HttpStatusCode expectedStatus = HttpStatusCode.OK,
			WebExceptionStatus expectedError = WebExceptionStatus.Success)
		{
			Debug (ctx, 1, "RUN");

			Handler target = (Handler)Redirect ?? Handler;

			return target.RunWithContext (ctx, Server, async (uri) => {
				var request = CreateRequest (ctx, uri);
				ConfigureRequest (ctx, uri, request);

				var response = await RunInner (ctx, cancellationToken, request);

				CheckResponse (ctx, response, cancellationToken, expectedStatus, expectedError);
			});
		}

		public async Task RunExternal (
			TestContext ctx, CancellationToken cancellationToken, Uri uri,
			HttpStatusCode expectedStatus = HttpStatusCode.OK,
			WebExceptionStatus expectedError = WebExceptionStatus.Success)
		{
			Debug (ctx, 1, "RUN");

			var request = CreateRequest (ctx, uri);

			var response = await RunInner (ctx, cancellationToken, request);

			CheckResponse (ctx, response, cancellationToken, expectedStatus, expectedError);
		}

		public static Task RunTraditional (
			TestContext ctx, HttpServer server, Handler handler,
			CancellationToken cancellationToken, bool sendAsync = false,
			HttpStatusCode expectedStatus = HttpStatusCode.OK,
			WebExceptionStatus expectedError = WebExceptionStatus.Success)
		{
			var runner = new TraditionalTestRunner (server, handler, sendAsync);
			return runner.Run (ctx, cancellationToken, expectedStatus, expectedError);
		}

		public static Task RunHttpClient (
			TestContext ctx, CancellationToken cancellationToken, HttpServer server,
			HttpClientHandler handler, RedirectHandler redirect = null,
			HttpStatusCode expectedStatus = HttpStatusCode.OK,
			WebExceptionStatus expectedError = WebExceptionStatus.Success)
		{
			var runner = new HttpClientTestRunner (server, handler, redirect);
			return runner.Run (ctx, cancellationToken, expectedStatus, expectedError);
		}

		protected virtual void CheckResponse (
			TestContext ctx, Response response, CancellationToken cancellationToken,
			HttpStatusCode expectedStatus = HttpStatusCode.OK, WebExceptionStatus expectedError = WebExceptionStatus.Success)
		{
			Debug (ctx, 1, "GOT RESPONSE", response.Status, response.IsSuccess, response.Error != null ? response.Error.Message : string.Empty);

			if (ctx.HasPendingException)
				return;

			if (cancellationToken.IsCancellationRequested) {
				ctx.OnTestCanceled ();
				return;
			}

			if (expectedError != WebExceptionStatus.Success) {
				ctx.Expect (response.Error, Is.Not.Null, "expecting exception");
				ctx.Expect (response.Status, Is.EqualTo (expectedStatus));
				var wexc = response.Error as WebException;
				ctx.Expect (wexc, Is.Not.Null, "WebException");
				if (expectedError != WebExceptionStatus.AnyErrorStatus)
					ctx.Expect ((WebExceptionStatus)wexc.Status, Is.EqualTo (expectedError));
				return;
			}

			if (response.Error != null) {
				if (response.Content != null)
					ctx.OnError (new WebException (response.Content.AsString (), response.Error));
				else
					ctx.OnError (response.Error);
			} else {
				var ok = ctx.Expect (expectedStatus, Is.EqualTo (response.Status), "status code");
				if (ok)
					ctx.Expect (response.IsSuccess, Is.True, "success status");
			}

			if (response.Content != null)
				Debug (ctx, 5, "GOT RESPONSE BODY", response.Content);
		}
	}
}

