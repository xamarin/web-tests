//
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

namespace Xamarin.WebTests.Tests
{
	using Handlers;
	using Framework;
	using Portable;

	[AttributeUsage (AttributeTargets.Parameter | AttributeTargets.Property, AllowMultiple = false)]
	public class GetHandlerAttribute : TestParameterAttribute, ITestParameterSource<Handler>
	{
		public GetHandlerAttribute (string filter = null, TestFlags flags = TestFlags.Browsable)
			: base (filter, flags)
		{
		}

		public IEnumerable<Handler> GetParameters (TestContext ctx, string filter)
		{
			return TestGet.GetParameters (ctx, filter);
		}
	}

	[Work]
	[AsyncTestFixture (Timeout = 5000)]
	public class TestGet : ITestHost<HttpServer>
	{
		[WebTestFeatures.SelectSSL]
		public bool UseSSL {
			get; set;
		}

		[WebTestFeatures.SelectReuseConnection]
		public bool ReuseConnection {
			get; set;
		}

		public HttpServer CreateInstance (TestContext ctx)
		{
			return new HttpServer (PortableSupport.Web.GetLoopbackEndpoint (9999), ReuseConnection, UseSSL);
		}

		public static IEnumerable<Handler> GetParameters (TestContext ctx, string filter)
		{
			yield return new HelloWorldHandler ("First Hello");
			yield return new HelloWorldHandler ("Second Hello");
		}

		[AsyncTest]
		public Task Run (TestContext ctx, CancellationToken cancellationToken, [TestHost] HttpServer server, [GetHandler] Handler handler)
		{
			return TestRunner.RunTraditional (ctx, server, handler, cancellationToken);
		}

		[AsyncTest]
		public Task Run (TestContext ctx, CancellationToken cancellationToken,
			[TestHost] HttpServer server, bool sendAsync,
			[GetHandler] Handler handler)
		{
			return TestRunner.RunTraditional (ctx, server, handler, cancellationToken, sendAsync);
		}

		[AsyncTest]
		public Task Redirect (TestContext ctx, CancellationToken cancellationToken,
			[TestHost] HttpServer server, bool sendAsync,
			[RedirectStatusAttribute] HttpStatusCode code,
			[GetHandler] Handler handler)
		{
			var description = string.Format ("{0}: {1}", code, handler.Identifier);
			var redirect = new RedirectHandler (handler, code, description);

			return TestRunner.RunTraditional (ctx, server, redirect, cancellationToken, sendAsync);
		}

		[Work]
		[AsyncTest]
		public void MartinTest (TestContext ctx, CancellationToken cancellationToken, bool test)
		{
			ctx.LogMessage ("HELLO WORLD: {0}", test);
		}

		[Work]
		[AsyncTest]
		public void MartinTest2 (TestContext ctx, [GetHandler] Handler handler)
		{
			ctx.LogMessage ("HELLO WORLD: {0}", handler);
		}

	}
}

