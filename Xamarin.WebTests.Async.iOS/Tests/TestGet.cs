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
	using Server;
	using Runners;
	using Handlers;
	using Framework;

	// [AsyncTestFixture]
	public class TestGet : ITestHost<TestRunner>, ITestParameterSource<Handler>
	{
		[TestParameter (null, TestFlags.Hidden)]
		public bool UseSSL {
			get; set;
		}

		[TestParameter (null, TestFlags.Hidden)]
		public bool ReuseConnection {
			get; set;
		}

		public TestRunner CreateInstance (TestContext context)
		{
			return new HttpTestRunner { UseSSL = UseSSL, ReuseConnection = ReuseConnection };
		}

		public IEnumerable<Handler> GetParameters (TestContext context, string filter)
		{
			yield return new HelloWorldHandler ();
			yield return new HelloWorldHandler ();
		}

		[AsyncTest]
		public Task Run (TestContext ctx, CancellationToken cancellationToken,
			[TestHost (typeof (TestGet))] TestRunner runner, [TestParameter] Handler handler)
		{
			return runner.Run (ctx, handler, cancellationToken);
		}

		[AsyncTest]
		public Task Redirect (TestContext ctx, CancellationToken cancellationToken,
			[TestHost] TestRunner runner,
			[TestParameter (typeof (RedirectStatusSource))] HttpStatusCode code,
			[TestParameter] Handler handler)
		{
			var description = string.Format ("{0}: {1}", code, handler.Description);
			var redirect = new RedirectHandler (handler, code) { Description = description };

			return runner.Run (ctx, redirect, cancellationToken);
		}
	}
}

