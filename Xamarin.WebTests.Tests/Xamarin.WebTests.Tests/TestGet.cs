﻿//
// TestPost.cs
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
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;

using Xamarin.AsyncTests;
using Xamarin.AsyncTests.Portable;

namespace Xamarin.WebTests.Tests {
	using HttpHandlers;
	using HttpFramework;
	using TestFramework;
	using HttpOperations;

	[AsyncTestFixture (Timeout = 5000)]
	public class TestGet : ITestParameterSource<Handler> {
		[WebTestFeatures.SelectHttpServerFlags]
		public HttpServerFlags Flags {
			get; set;
		}

		public bool SendAsync {
			get; set;
		}

		public IEnumerable<Handler> GetParameters (TestContext ctx, string filter)
		{
			if (string.Equals (filter, "hello")) {
				yield return new HelloWorldHandler ("Hello World");
				yield break;
			}

			yield return new HelloWorldHandler ("First Hello");
			yield return new HelloWorldHandler ("Second Hello");
		}

		[AsyncTest]
		public async Task Run (TestContext ctx, CancellationToken cancellationToken,
		                       HttpServer server, Handler handler)
		{
			using (var operation = new TraditionalOperation (server, handler, SendAsync))
				await operation.Run (ctx, cancellationToken).ConfigureAwait (false);
		}

		[AsyncTest]
		public async Task Redirect (TestContext ctx, CancellationToken cancellationToken,
		                            HttpServer server, [RedirectStatus] HttpStatusCode code,
		                            Handler handler)
		{
			var description = string.Format ("{0}: {1}", code, handler.Value);
			var redirect = new RedirectHandler (handler, code, description);

			using (var operation = new TraditionalOperation (server, redirect, SendAsync))
				await operation.Run (ctx, cancellationToken).ConfigureAwait (false);
		}
	}
}

