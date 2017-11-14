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

namespace Xamarin.WebTests.Tests {
	using ConnectionFramework;
	using HttpHandlers;
	using HttpFramework;
	using TestFramework;
	using HttpOperations;
	using Resources;

	[WebTestFeatures.Proxy]
	[AsyncTestFixture (Timeout = 30000)]
	public class TestProxy : ITestHost<HttpServer>, ITestParameterSource<Handler> {
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

		BuiltinProxyServer CreateBackend (int port, int proxyPort, ConnectionParameters parameters = null,
		                                  AuthenticationType authType = AuthenticationType.None,
		                                  ICredentials credentials = null)
		{
			var endpoint = address.CopyWithPort (port);
			var proxyEndpoint = address.CopyWithPort (proxyPort);
			var target = new BuiltinHttpServer (endpoint, endpoint, HttpServerFlags.InstrumentationListener, parameters, null);
			return new BuiltinProxyServer (target, proxyEndpoint, HttpServerFlags.Proxy, authType) {
				Credentials = credentials
			};
		}

		BuiltinProxyServer CreateBackend (TestContext ctx)
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
				return CreateBackend (9991, 9990, serverParameters);

			case ProxyKind.NtlmWithSSL:
				return CreateBackend (9989, 9988, serverParameters, AuthenticationType.NTLM, monkeyCredential);

			default:
				throw new InternalErrorException ();
			}
		}

		public HttpServer CreateInstance (TestContext ctx)
		{
			if (!hasNetwork)
				throw new InvalidOperationException ();

			return CreateBackend (ctx);
		}

		public IEnumerable<Handler> GetParameters (TestContext ctx, string filter)
		{
			var list = new List<Handler> ();
			if (!hasNetwork)
				return list;

			switch (filter) {
			case "martin":
				// list.Add (HelloWorldHandler.GetSimple ());
				list.Add (new PostHandler ("Normal post", HttpContent.HelloWorld));
				break;
			default:
				list.Add (HelloWorldHandler.GetSimple ());
				list.AddRange (TestPost.GetParameters (ctx, filter, HttpServerFlags.Proxy));
				break;
			}
			return list;
		}

		[AsyncTest]
		public async Task Run (
			TestContext ctx,
			[WebTestFeatures.SelectProxyKind (IncludeSSL = true)] ProxyKind kind,
			HttpServer server, Handler handler, CancellationToken cancellationToken)
		{
			var oldCount = server.CountRequests;
			HttpOperation operation;
			if (kind == ProxyKind.Unauthenticated)
				operation = new TraditionalOperation (
					server, handler, true, HttpOperationFlags.AbortAfterClientExits,
					HttpStatusCode.ProxyAuthenticationRequired, WebExceptionStatus.ProtocolError);
			else
				operation = new TraditionalOperation (
				server, handler, true);
			try {
				await operation.Run (ctx, cancellationToken).ConfigureAwait (false);
				var newCount = server.CountRequests;
				ctx.Assert (newCount, Is.GreaterThan (oldCount), "used proxy");
			} finally {
				operation.Dispose ();
			}
		}

		[NotWorking]
		[AsyncTest]
		public async Task RunAuthentication (
			TestContext ctx,
			[WebTestFeatures.SelectProxyKind (IncludeSSL = true)] ProxyKind kind,
			HttpServer server, [AuthenticationType] AuthenticationType authType,
			Handler handler, CancellationToken cancellationToken)
		{
			var authHandler = new AuthenticationHandler (authType, handler);
			HttpOperation operation;
			if (kind == ProxyKind.Unauthenticated)
				operation = new TraditionalOperation (server, authHandler, true,
					HttpOperationFlags.AbortAfterClientExits,
					HttpStatusCode.ProxyAuthenticationRequired, WebExceptionStatus.ProtocolError);
			else
				operation = new TraditionalOperation (server, authHandler, true);
			try {
				await operation.Run (ctx, cancellationToken).ConfigureAwait (false);
			} finally {
				operation.Dispose ();
			}
		}

		[AsyncTest]
		[WebTestFeatures.UseProxyKind (ProxyKind.SSL)]
		public async Task RunSsl (
			TestContext ctx, HttpServer server, Handler handler,
			CancellationToken cancellationToken)
		{
			var oldCount = server.CountRequests;
			using (var operation = new TraditionalOperation (server, handler, true))
				await operation.Run (ctx, cancellationToken).ConfigureAwait (false);
			var newCount = server.CountRequests;
			ctx.Assert (newCount, Is.GreaterThan (oldCount), "used proxy");
		}

		[AsyncTest]
		[ExpectedException (typeof (NotSupportedException))]
		public void InvalidProxyScheme (TestContext ctx)
		{
			var url = string.Format ("https://{0}:8888/", address.Address);
			var request = (HttpWebRequest)WebRequest.Create (url);
			var requestExt = DependencyInjector.GetExtension<HttpWebRequest, IHttpWebRequestExtension> (request);
			requestExt.SetProxy (BuiltinProxyServer.CreateSimpleProxy (new Uri (url)));
		}

		[Martin ("Proxy")]
		[AsyncTest (ParameterFilter = "martin", Unstable = true)]
		[WebTestFeatures.UseProxyKind (ProxyKind.SSL)]
		public async Task MartinTest (
			TestContext ctx, HttpServer server, Handler handler,
			CancellationToken cancellationToken)
		{
			var oldCount = server.CountRequests;
			using (var operation = new TraditionalOperation (server, handler, true))
				await operation.Run (ctx, cancellationToken).ConfigureAwait (false);
			var newCount = server.CountRequests;
			ctx.Assert (newCount, Is.GreaterThan (oldCount), "used proxy");
		}
	}
}
