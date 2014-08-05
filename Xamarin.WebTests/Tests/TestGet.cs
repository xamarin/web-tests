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

	[AsyncTestFixture (Timeout = 5000)]
	public class TestGet : ITestHost<HttpServer>, ITestParameterSource<Handler>
	{
		[TestParameter (typeof (WebTestFeatures.SelectSSL), null, TestFlags.Hidden)]
		public bool UseSSL {
			get; set;
		}

		[TestParameter (typeof (WebTestFeatures.SelectReuseConnection), null, TestFlags.Hidden)]
		public bool ReuseConnection {
			get; set;
		}

		public HttpServer CreateInstance (TestContext ctx)
		{
			return new HttpServer (PortableSupport.Web.GetLoopbackEndpoint (9999), ReuseConnection, UseSSL);
		}

		public IEnumerable<Handler> GetParameters (TestContext ctx, string filter)
		{
			yield return new HelloWorldHandler ();
			yield return new HelloWorldHandler ();
		}

		[AsyncTest]
		public Task Run (TestContext ctx, CancellationToken cancellationToken, [TestHost] HttpServer server, [TestParameter] Handler handler)
		{
			return TestRunner.RunTraditional (ctx, server, handler, cancellationToken);
		}

		[AsyncTest]
		public Task Run (TestContext ctx, CancellationToken cancellationToken,
			[TestHost] HttpServer server, bool sendAsync,
			[TestParameter] Handler handler)
		{
			return TestRunner.RunTraditional (ctx, server, handler, cancellationToken, sendAsync);
		}

		[AsyncTest]
		public Task Redirect (TestContext ctx, CancellationToken cancellationToken,
			[TestHost] HttpServer server, bool sendAsync,
			[TestParameter (typeof (RedirectStatusSource))] HttpStatusCode code,
			[TestParameter] Handler handler)
		{
			var description = string.Format ("{0}: {1}", code, handler.Description);
			var redirect = new RedirectHandler (handler, code) { Description = description };

			return TestRunner.RunTraditional (ctx, server, redirect, cancellationToken, sendAsync);
		}

		[Work]
		[AsyncTest]
		public async Task TestFork (TestContext ctx, [Fork (5, RandomDelay = 1500)] IFork fork, CancellationToken cancellationToken)
		{
			ctx.LogMessage ("FORK START: {0} {1}", fork.ID, ctx.PortableSupport.CurrentThreadId);
			Task.Delay (1500).Wait ();
			ctx.LogMessage ("FORK HERE: {0} {1}", fork.ID, ctx.PortableSupport.CurrentThreadId);
			await Task.Delay (5000);
			ctx.LogMessage ("FORK DONE: {0} {1}", fork.ID, ctx.PortableSupport.CurrentThreadId);
		}
	}
}

