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
	using Handlers;
	using Framework;
	using Portable;

	[AsyncTestFixture (Timeout = 10000)]
	public class TestAuthentication : ITestHost<HttpServer>, ITestParameterSource<Handler>, ITestParameterSource<AuthenticationType>
	{
		[TestParameter (typeof (WebTestFeatures.SelectSSL), null, TestFlags.Hidden)]
		public bool UseSSL {
			get; set;
		}

		[TestParameter (null, TestFlags.Hidden)]
		public bool ReuseConnection {
			get; set;
		}

		public HttpServer CreateInstance (TestContext ctx)
		{
			return new HttpServer (PortableSupport.Web.GetLoopbackEndpoint (9999), ReuseConnection, UseSSL);
		}

		IEnumerable<Handler> ITestParameterSource<Handler>.GetParameters (TestContext ctx, string filter)
		{
			return TestPost.GetParameters (ctx, filter);
		}

		public static IEnumerable<AuthenticationType> GetAuthenticationTypes (TestContext ctx, string filter)
		{
			yield return AuthenticationType.Basic;
			if (ctx.IsEnabled (WebTestFeatures.NTLM))
				yield return AuthenticationType.NTLM;
		}

		IEnumerable<AuthenticationType> ITestParameterSource<AuthenticationType>.GetParameters (TestContext ctx, string filter)
		{
			return GetAuthenticationTypes (ctx, filter);
		}

		[AsyncTest]
		public Task Run (
			TestContext ctx, [TestHost] HttpServer server, bool sendAsync,
			[TestParameter] AuthenticationType authType, [TestParameter] Handler handler,
			CancellationToken cancellationToken)
		{
			var authHandler = new AuthenticationHandler (authType, handler);
			return TestRunner.RunTraditional (ctx, server, authHandler, cancellationToken);
		}

		[AsyncTest]
		public Task MustClearAuthOnRedirect (
			TestContext ctx, [TestHost] HttpServer server, bool sendAsync,
			CancellationToken cancellationToken)
		{
			var target = new HelloWorldHandler ();
			var targetAuth = new AuthenticationHandler (AuthenticationType.ForceNone, target);

			var redirect = new RedirectHandler (targetAuth, HttpStatusCode.Redirect);
			var authHandler = new AuthenticationHandler (AuthenticationType.Basic, redirect);

			return TestRunner.RunTraditional (ctx, server, authHandler, cancellationToken, sendAsync);
		}
	}
}

