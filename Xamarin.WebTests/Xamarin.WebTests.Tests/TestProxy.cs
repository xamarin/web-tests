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
using Xamarin.AsyncTests.Portable;

namespace Xamarin.WebTests.Tests
{
	using ConnectionFramework;
	using HttpHandlers;
	using HttpFramework;
	using TestRunners;
	using Portable;
	using Providers;

	[AttributeUsage (AttributeTargets.Parameter | AttributeTargets.Property, AllowMultiple = false)]
	public class ProxyHandlerAttribute : TestParameterAttribute, ITestParameterSource<Handler>
	{
		public ProxyHandlerAttribute (string filter = null, TestFlags flags = TestFlags.Browsable)
			: base (filter, flags)
		{
		}

		public IEnumerable<Handler> GetParameters (TestContext ctx, string filter)
		{
			return TestProxy.GetParameters (ctx, filter);
		}
	}

	[WebTestFeatures.Proxy]
	[AsyncTestFixture (Timeout = 30000)]
	public class TestProxy : ITestHost<ProxyServer>
	{
		[WebTestFeatures.SelectProxyKind]
		public ProxyKind Kind {
			get; set;
		}

		readonly static IPortableEndPoint address;
		readonly static IServerCertificate serverCertificate;
		readonly static ServerParameters serverParameters;
		readonly static bool hasNetwork;

		static TestProxy ()
		{
			var support = DependencyInjector.Get<IPortableEndPointSupport> ();
			address = support.GetEndpoint (0);
			hasNetwork = !address.IsLoopback;

			var webSupport = DependencyInjector.Get<IPortableWebSupport> ();
			serverCertificate = webSupport.GetDefaultServerCertificate ();
			serverParameters = new ServerParameters ("proxy", serverCertificate);
		}

		public ProxyServer CreateInstance (TestContext ctx)
		{
			if (!hasNetwork)
				throw new InvalidOperationException ();

			var provider = DependencyInjector.Get<ConnectionProviderFactory> ().DefaultHttpProvider;

			switch (Kind) {
			case ProxyKind.Simple:
				return new ProxyServer (provider, address.CopyWithPort (9999), address.CopyWithPort (9998));

			case ProxyKind.BasicAuth:
				return new ProxyServer (provider, address.CopyWithPort (9997), address.CopyWithPort (9996)) {
					AuthenticationType = AuthenticationType.Basic,
					Credentials = new NetworkCredential ("xamarin", "monkey")
				};

			case ProxyKind.NtlmAuth:
				return new ProxyServer (provider, address.CopyWithPort (9995), address.CopyWithPort (9994)) {
					AuthenticationType = AuthenticationType.NTLM,
					Credentials = new NetworkCredential ("xamarin", "monkey")
				};

			case ProxyKind.Unauthenticated:
				return new ProxyServer (provider, address.CopyWithPort (9993), address.CopyWithPort (9992)) {
					AuthenticationType = AuthenticationType.Basic
				};

			case ProxyKind.SSL:
				return new ProxyServer (provider, address.CopyWithPort (9991), address.CopyWithPort (9990), serverParameters);

			default:
				throw new InvalidOperationException ();
			}
		}

		public static IEnumerable<Handler> GetParameters (TestContext ctx, string filter)
		{
			var list = new List<Handler> ();
			if (!hasNetwork)
				return list;

			list.Add (new HelloWorldHandler ("Hello World"));
			list.AddRange (TestPost.GetParameters (ctx, filter));
			return list;
		}

		[AsyncTest]
		public Task Run (
			TestContext ctx, [TestHost] ProxyServer server,
			[ProxyHandler] Handler handler, CancellationToken cancellationToken)
		{
			if (Kind == ProxyKind.Unauthenticated)
				return TestRunner.RunTraditional (
					ctx, server, handler, cancellationToken, false,
					HttpStatusCode.ProxyAuthenticationRequired, WebExceptionStatus.ProtocolError);
			else
				return TestRunner.RunTraditional (
					ctx, server, handler, cancellationToken, false);
		}

		[AsyncTest]
		public Task RunAuthentication (
			TestContext ctx, [TestHost] ProxyServer server,
			[AuthenticationType] AuthenticationType authType,  [ProxyHandler] Handler handler,
			CancellationToken cancellationToken)
		{
			var authHandler = new AuthenticationHandler (authType, handler);
			if (Kind == ProxyKind.Unauthenticated)
				return TestRunner.RunTraditional (
					ctx, server, authHandler, cancellationToken, false,
					HttpStatusCode.ProxyAuthenticationRequired, WebExceptionStatus.ProtocolError);
			else
				return TestRunner.RunTraditional (
					ctx, server, authHandler, cancellationToken, false);
		}
	}
}
