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

namespace Xamarin.WebTests.Framework
{
	using Handlers;
	using Framework;

	public static class TestRunner
	{
		static void Debug (TestContext ctx, HttpServer server, int level,
			Handler handler, string message, params object[] args)
		{
			if (Handler.DebugLevel < level)
				return;
			var sb = new StringBuilder ();
			sb.AppendFormat ("{0}:{1}: {2}", server, handler, message);
			for (int i = 0; i < args.Length; i++) {
				sb.Append (" ");
				sb.Append (args [i] != null ? args [i].ToString () : "<null>");
			}

			ctx.LogDebug (level, sb.ToString ());
		}

		public static async Task<bool> RunTraditional (
			TestContext ctx, HttpServer server, Handler handler,
			CancellationToken cancellationToken, bool sendAsync = false,
			HttpStatusCode expectedStatus = HttpStatusCode.OK,
			bool expectException = false)
		{
			Debug (ctx, server, 1, handler, "RUN TRADITIONAL");

			var retval = await handler.RunWithContext (ctx, server, async (uri) => {
				var request = new TraditionalRequest (uri);
				handler.ConfigureRequest (request, uri);

				request.SetProxy (server.GetProxy ());

				Response response;
				if (sendAsync)
					response = await request.SendAsync (ctx, cancellationToken);
				else
					response = await request.Send (ctx, cancellationToken);

				return CheckResponse (
					ctx, server, response, handler, cancellationToken, expectedStatus, expectException);
			});

			return retval;
		}

		public static Task<bool> RunHttpClient (
			TestContext ctx, HttpServer server, HttpClientHandler handler, CancellationToken cancellationToken,
			HttpStatusCode expectedStatus = HttpStatusCode.OK,
			bool expectException = false)
		{
			Debug (ctx, server, 1, handler, "RUN HTTP CLIENT");

			return handler.RunWithContext (ctx, server, async (uri) => {
				var request = new HttpClientRequest (handler, uri);
				handler.ConfigureRequest (request, uri);

				request.SetProxy (server.GetProxy ());

				Response response;

				switch (handler.Operation) {
				case HttpClientOperation.GetString:
					response = await request.GetString (ctx, cancellationToken);
					break;
				case HttpClientOperation.PostString:
					response = await request.PostString (ctx, handler.ReturnContent, cancellationToken);
					break;
				default:
					throw new InvalidOperationException ();
				}

				return CheckResponse (
					ctx, server, response, handler, cancellationToken, expectedStatus, expectException);
			});
		}

		static bool CheckResponse (
			TestContext ctx, HttpServer server, Response response, Handler handler,
			CancellationToken cancellationToken, HttpStatusCode expectedStatus = HttpStatusCode.OK,
			bool expectException = false)
		{
			Debug (ctx, server, 1, handler, "GOT RESPONSE", response.Status, response.IsSuccess, response.Error);

			if (ctx.HasPendingException)
				return false;

			bool ok;

			if (expectException) {
				ok = ctx.Expect (response.Error, Is.Not.Null, "expecting exception");
				ok &= ctx.Expect (response.Status, Is.EqualTo (expectedStatus));
				return ok;
			}

			if (response.Error != null) {
				if (response.Content != null)
					ctx.OnError (new WebException (response.Content.AsString (), response.Error));
				else
					ctx.OnError (response.Error);
				ok = false;
			} else {
				ok = ctx.Expect (expectedStatus, Is.EqualTo (response.Status), "status code");
				if (ok)
					ok &= ctx.Expect (expectException, Is.EqualTo (!response.IsSuccess), "success status");
			}

			if (response.Content != null)
				Debug (ctx, server, 5, handler, "GOT RESPONSE BODY", response.Content);

			return ok;
		}
	}
}

