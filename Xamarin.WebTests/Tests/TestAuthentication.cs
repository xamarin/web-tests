//
// TestAuthenticationAsync.cs
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

	[AsyncTestFixture (Timeout = 10000)]
	public class TestAuthentication : ITestHost<TestRunner>, ITestParameterSource<Handler>, ITestParameterSource<AuthenticationType>
	{
		[TestParameter (typeof (WebTestFeatures.SelectSSL), null, TestFlags.Hidden)]
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

		IEnumerable<Handler> ITestParameterSource<Handler>.GetParameters (TestContext context, string filter)
		{
			return TestPost.GetParameters (context, filter);
		}

		public static IEnumerable<AuthenticationType> GetAuthenticationTypes (TestContext context, string filter)
		{
			yield return AuthenticationType.Basic;
		}

		IEnumerable<AuthenticationType> ITestParameterSource<AuthenticationType>.GetParameters (TestContext context, string filter)
		{
			return GetAuthenticationTypes (context, filter);
		}

		[AsyncTest]
		public Task Run (
			InvocationContext ctx, [TestHost] TestRunner runner,
			[TestParameter] AuthenticationType authType,  [TestParameter] Handler handler,
			CancellationToken cancellationToken)
		{
			var authHandler = new AuthenticationHandler (authType, handler);
			return runner.Run (ctx, authHandler, cancellationToken);
		}

		[AsyncTest]
		public Task MustClearAuthOnRedirect (
			InvocationContext ctx, [TestHost] TestRunner runner,
			CancellationToken cancellationToken)
		{
			var target = new HelloWorldHandler ();
			var targetAuth = new AuthenticationHandler (AuthenticationType.ForceNone, target);

			var redirect = new RedirectHandler (targetAuth, HttpStatusCode.Redirect);
			var authHandler = new AuthenticationHandler (AuthenticationType.Basic, redirect);

			return runner.Run (ctx, authHandler, cancellationToken);
		}
	}
}

