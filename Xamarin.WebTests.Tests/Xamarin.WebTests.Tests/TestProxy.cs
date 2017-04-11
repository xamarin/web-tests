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
using System.Security.Cryptography.X509Certificates;

using Xamarin.AsyncTests;
using Xamarin.AsyncTests.Portable;
using Xamarin.AsyncTests.Constraints;

namespace Xamarin.WebTests.Tests
{
	using ConnectionFramework;
	using HttpHandlers;
	using HttpFramework;
	using TestFramework;
	using TestRunners;
	using Resources;
	using Server;

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
		readonly static IPortableEndPoint address;
		readonly static X509Certificate serverCertificate;
		readonly static ConnectionParameters serverParameters;
		readonly static bool hasNetwork;

		static TestProxy ()
		{
			var support = DependencyInjector.Get<IPortableEndPointSupport> ();
			address = support.GetEndpoint (0);
			hasNetwork = !address.IsLoopback;

			serverCertificate = ResourceManager.SelfSignedServerCertificate;
			serverParameters = new ConnectionParameters ("proxy", serverCertificate);
		}

		ProxyBackend CreateBackend (int port, int proxyPort, ConnectionParameters parameters = null,
		                            AuthenticationType authType = AuthenticationType.None,
		                            ICredentials credentials = null)
		{
			var endpoint = address.CopyWithPort (port);
			var proxyEndpoint = address.CopyWithPort (proxyPort);
			var target = new BuiltinHttpBackend (endpoint, endpoint, ListenerFlags.None, parameters, null);
			return new ProxyBackend (target, proxyEndpoint, ListenerFlags.Proxy) {
				AuthenticationType = authType,
				Credentials = credentials
			};
		}

		ProxyBackend CreateBackend (TestContext ctx)
		{
			var kind = ctx.GetParameter<ProxyKind> ();

			var monkeyCredential = new NetworkCredential ("xamarin", "monkey");

			switch (kind) {
			case ProxyKind.Simple:
				return CreateBackend (9999, 9998);

			case ProxyKind.BasicAuth:
				return CreateBackend (9997, 9996, null, AuthenticationType.Basic, monkeyCredential);

			case ProxyKind.NtlmAuth:
				return CreateBackend (9995, 9994, null, AuthenticationType.NTLM, monkeyCredential);

			case ProxyKind.Unauthenticated:
				return CreateBackend (9993, 9992, null, AuthenticationType.Basic);

			case ProxyKind.SSL:
				return CreateBackend (9991, 9900, serverParameters);

			default:
				throw new InternalErrorException ();
			}
		}

		public ProxyServer CreateInstance (TestContext ctx)
		{
			if (!hasNetwork)
				throw new InvalidOperationException ();

			return new ProxyServer (CreateBackend (ctx));
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
		public async Task Run (
			TestContext ctx,
			[WebTestFeatures.SelectProxyKind (IncludeSSL = true)] ProxyKind kind,
			[TestHost] ProxyServer server,
			[ProxyHandler] Handler handler,
			CancellationToken cancellationToken)
		{
			var oldCount = server.CountRequests;
			if (kind == ProxyKind.Unauthenticated) {
				await TestRunner.RunTraditional (
					ctx, server, handler, cancellationToken, false,
					HttpStatusCode.ProxyAuthenticationRequired, WebExceptionStatus.ProtocolError);
			} else {
				await TestRunner.RunTraditional (
					ctx, server, handler, cancellationToken, false).ConfigureAwait (false);
				var newCount = server.CountRequests;
				ctx.Assert (newCount, Is.GreaterThan (oldCount), "used proxy");
			}
		}

		[AsyncTest]
		public Task RunAuthentication (
			TestContext ctx,
			[WebTestFeatures.SelectProxyKind (IncludeSSL = true)] ProxyKind kind,
			[TestHost] ProxyServer server,
			[AuthenticationType] AuthenticationType authType,
			[ProxyHandler] Handler handler,
			CancellationToken cancellationToken)
		{
			var authHandler = new AuthenticationHandler (authType, handler);
			if (kind == ProxyKind.Unauthenticated)
				return TestRunner.RunTraditional (
					ctx, server, authHandler, cancellationToken, false,
					HttpStatusCode.ProxyAuthenticationRequired, WebExceptionStatus.ProtocolError);
			else
				return TestRunner.RunTraditional (
					ctx, server, authHandler, cancellationToken, false);
		}

		[AsyncTest]
		[WebTestFeatures.UseProxyKindAttribute (ProxyKind.SSL)]
		public async Task RunSsl (
			TestContext ctx,
			[TestHost] ProxyServer server,
			[ProxyHandler] Handler handler,
			CancellationToken cancellationToken)
		{
			var oldCount = server.CountRequests;
			await TestRunner.RunTraditional (ctx, server, handler, cancellationToken, false);
			var newCount = server.CountRequests;
			ctx.Assert (newCount, Is.GreaterThan (oldCount), "used proxy");
		}

		[AsyncTest]
		[ExpectedException (typeof (NotSupportedException))]
		public void InvalidProxyScheme (TestContext ctx)
		{
			var url = string.Format ("https://{0}:8888/", address.Address);
			var request = (HttpWebRequest)WebRequest.Create (url);
			var requestExt = DependencyInjector.GetExtension<HttpWebRequest,IHttpWebRequestExtension> (request);
			requestExt.SetProxy (ProxyBackend.CreateSimpleProxy (new Uri (url)));
		}
	}
}
