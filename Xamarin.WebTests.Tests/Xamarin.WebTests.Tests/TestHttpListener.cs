//
// TestHttpListener.cs
//
// Author:
//       Martin Baulig <mabaul@microsoft.com>
//
// Copyright (c) 2017 Xamarin Inc. (http://www.xamarin.com)
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
using System.Collections.Generic;
using Xamarin.AsyncTests;
using Xamarin.WebTests.TestFramework;
using Xamarin.WebTests.HttpFramework;
using Xamarin.WebTests.HttpHandlers;
using Xamarin.WebTests.TestRunners;

namespace Xamarin.WebTests.Tests {
	[AsyncTestFixture]
	public class TestHttpListener : ITestParameterSource<Handler> {
		public IEnumerable<Handler> GetParameters (TestContext ctx, string filter)
		{
			switch (filter) {
			case null:
				yield return new HelloWorldHandler ("Hello World");
				yield return new PostHandler ("Empty body", StringContent.Empty);
				yield return new PostHandler ("Normal post", HttpContent.HelloWorld);
				yield return new PostHandler ("Content-Length", HttpContent.HelloWorld, TransferMode.ContentLength);
				yield return new PostHandler ("Chunked", HttpContent.HelloChunked, TransferMode.Chunked);
				yield return new PostHandler ("Explicit length and empty body", StringContent.Empty, TransferMode.ContentLength);
				yield return new PostHandler ("Explicit length and no body", null, TransferMode.ContentLength);
				yield return new PostHandler ("Bug #41206", new RandomContent (102400));
				yield return new PostHandler ("Bug #41206 odd size", new RandomContent (102431));
				yield return new DeleteHandler ("Empty delete");
				yield return new DeleteHandler ("DELETE with empty body", string.Empty);
				yield return new DeleteHandler ("DELETE with request body", "I have a body!");
				yield return new DeleteHandler ("DELETE with no body and a length") {
					Flags = RequestFlags.ExplicitlySetLength
				};
				break;
			case "martin":
				yield return new AuthenticationHandler (AuthenticationType.Basic, HelloWorldHandler.Simple);
				yield return new RedirectHandler (HelloWorldHandler.Simple, HttpStatusCode.Redirect);
				break;
			case "not-working":
				yield return new PostHandler ("No body");
				break;
			}
		}

		[Work]
		[AsyncTest]
		[HttpServerFlags (HttpServerFlags.HttpListener)]
		public Task Run (TestContext ctx, HttpServer server, Handler handler,
		                 CancellationToken cancellationToken)
		{
			return TestRunner.RunTraditional (ctx, server, handler, cancellationToken);
		}

		[Martin]
		[HttpServerFlags (HttpServerFlags.HttpListener)]
		[AsyncTest (ParameterFilter = "martin", Unstable = true)]
		public Task MartinTest (TestContext ctx, HttpServer server, Handler handler,
		                        CancellationToken cancellationToken)
		{
			ctx.AssertFail ("Expected Error!");
			return TestRunner.RunTraditional (ctx, server, handler, cancellationToken);
		}

		[Work]
		[AsyncTest (ParameterFilter = "martin")]
		public Task RunWithInternalServer (TestContext ctx, HttpServer server, Handler handler,
		                                   CancellationToken cancellationToken)
		{
			return TestRunner.RunTraditional (ctx, server, handler, cancellationToken, true);
		}
	}
}
