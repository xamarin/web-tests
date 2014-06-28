//
// TestProxyAsync.cs
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
using System.Linq;
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

	[AsyncTestFixture]
	public class TestProxyAsync : ITestHost<ProxyTestRunner>, ITestParameterSource<Handler>,
		ITestParameterSource<ProxyKind>, ITestParameterSource<AuthenticationType>
	{
		[TestParameter]
		public bool UseSSL {
			get; set;
		}

		[TestParameter]
		public ProxyKind Kind {
			get; set;
		}

		readonly IPAddress address;
		readonly bool hasNetwork;

		public TestProxyAsync ()
		{
			address = TestRunner.GetAddress ();
			hasNetwork = !IPAddress.IsLoopback (address);
		}

		public ProxyTestRunner CreateInstance (TestContext context)
		{
			if (!hasNetwork)
				throw new InvalidOperationException ();

			switch (Kind) {
			case ProxyKind.Simple:
				return new ProxyTestRunner (address, 9999, 9998);

			case ProxyKind.BasicAuth:
				return new ProxyTestRunner (address, 9997, 9996) {
					AuthenticationType = AuthenticationType.Basic,
					Credentials = new NetworkCredential ("xamarin", "monkey")
				};

			case ProxyKind.NtlmAuth:
				return new ProxyTestRunner (address, 9995, 9994) {
					AuthenticationType = AuthenticationType.NTLM,
					Credentials = new NetworkCredential ("xamarin", "monkey")
				};

			case ProxyKind.Unauthenticated:
				return new ProxyTestRunner (address, 9993, 9992) {
					AuthenticationType = AuthenticationType.Basic
				};

			case ProxyKind.SSL:
				return new ProxyTestRunner (address, 9991, 9990) {
					UseSSL = true
				};

			default:
				throw new InvalidOperationException ();
			}
		}

		IEnumerable<ProxyKind> ITestParameterSource<ProxyKind>.GetParameters (TestContext context, string filter)
		{
			if (!hasNetwork)
				yield break;

			yield return ProxyKind.Simple;
			yield return ProxyKind.BasicAuth;
			yield return ProxyKind.NtlmAuth;
			yield return ProxyKind.Unauthenticated;

			// yield return ProxyKind.SSL;
		}

		IEnumerable<AuthenticationType> ITestParameterSource<AuthenticationType>.GetParameters (TestContext context, string filter)
		{
			return TestAuthenticationAsync.GetAuthenticationTypes (context, filter);
		}

		public IEnumerable<Handler> GetParameters (TestContext context, string filter)
		{
			var list = new List<Handler> ();
			if (!hasNetwork)
				return list;

			list.Add (new HelloWorldHandler ());
			list.AddRange (TestPostAsync.GetParameters (context, filter));
			return list;
		}

		[AsyncTest]
		public Task Run (
			TestContext ctx, [TestHost] ProxyTestRunner runner,
			[TestParameter] Handler handler, CancellationToken cancellationToken)
		{
			if (Kind == ProxyKind.Unauthenticated)
				return runner.Run (
					ctx, handler, cancellationToken,
					HttpStatusCode.ProxyAuthenticationRequired, true);
			else
				return runner.Run (ctx, handler, cancellationToken);
		}

		[AsyncTest]
		public Task TestAuthentication (
			TestContext ctx, [TestHost] ProxyTestRunner runner, [TestParameter] AuthenticationType authType, 
			[TestParameter] Handler handler, CancellationToken cancellationToken)
		{
			var authHandler = new AuthenticationHandler (authType, handler);
			if (Kind == ProxyKind.Unauthenticated)
				return runner.Run (
					ctx, authHandler, cancellationToken,
					HttpStatusCode.ProxyAuthenticationRequired, true);
			else
				return runner.Run (ctx, authHandler, cancellationToken);
		}
	}
}
