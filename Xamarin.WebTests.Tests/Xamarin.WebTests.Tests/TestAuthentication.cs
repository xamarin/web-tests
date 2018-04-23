﻿//
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
using Xamarin.AsyncTests.Portable;

namespace Xamarin.WebTests.Tests
{
	using HttpHandlers;
	using HttpFramework;
	using TestAttributes;
	using TestFramework;
	using HttpOperations;

	[AttributeUsage (AttributeTargets.Parameter | AttributeTargets.Property, AllowMultiple = false)]
	public class AuthenticationTypeAttribute : TestParameterAttribute, ITestParameterSource<AuthenticationType>
	{
		public AuthenticationTypeAttribute (string filter = null, TestFlags flags = TestFlags.Browsable)
			: base (filter, flags)
		{
		}

		public IEnumerable<AuthenticationType> GetParameters (TestContext ctx, string filter)
		{
			return TestAuthentication.GetAuthenticationTypes (ctx, filter);
		}
	}

	[AsyncTestFixture (Timeout = 10000)]
	public class TestAuthentication : ITestParameterSource<Handler>
	{
		[WebTestFeatures.SelectHttpServerFlags]
		public HttpServerFlags Flags {
			get; set;
		}

		public bool SendAsync {
			get; set;
		}

		public IEnumerable<Handler> GetParameters (TestContext ctx, string filter)
		{
			return TestPost.GetParameters (ctx, filter, Flags);
		}

		public static IEnumerable<AuthenticationType> GetAuthenticationTypes (TestContext ctx, string filter)
		{
			if (filter != null) {
				switch (filter) {
				case "include-none":
					yield return AuthenticationType.None;
					break;
				default:
					throw new InvalidOperationException ();
				}
			}

			yield return AuthenticationType.Basic;
			if (ctx.IsEnabled (WebTestFeatures.Instance.NTLM))
				yield return AuthenticationType.NTLM;
		}

		[AsyncTest]
		public async Task Run (
			TestContext ctx, HttpServer server,
			[AuthenticationType] AuthenticationType authType, Handler handler,
			CancellationToken cancellationToken)
		{
			var authHandler = new AuthenticationHandler (authType, handler);
			using (var operation = new TraditionalOperation (server, authHandler, true))
				await operation.Run (ctx, cancellationToken).ConfigureAwait (false);

		}

		[New]
		[AsyncTest]
		public async Task RunMartinTest (
			TestContext ctx, HttpServer server,
			[AuthenticationType] AuthenticationType authType, Handler handler,
			CancellationToken cancellationToken)
		{
			var authHandler = new AuthenticationHandler (authType, handler);
			using (var operation = new TraditionalOperation (server, authHandler, true))
				await operation.Run (ctx, cancellationToken).ConfigureAwait (false);

		}

		[AsyncTest]
		public async Task MustClearAuthOnRedirect (
			TestContext ctx, HttpServer server,
			CancellationToken cancellationToken)
		{
			var target = new HelloWorldHandler ("Hello World");
			var targetAuth = new AuthenticationHandler (AuthenticationType.ForceNone, target);

			var redirect = new RedirectHandler (targetAuth, HttpStatusCode.Redirect);
			var authHandler = new AuthenticationHandler (AuthenticationType.Basic, redirect);

			using (var operation = new TraditionalOperation (server, authHandler, true))
				await operation.Run (ctx, cancellationToken).ConfigureAwait (false);

		}
	}
}

