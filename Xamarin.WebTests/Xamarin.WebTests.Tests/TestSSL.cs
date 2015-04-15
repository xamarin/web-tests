//
// TestSsl.cs
//
// Author:
//       Martin Baulig <martin.baulig@xamarin.com>
//
// Copyright (c) 2015 Xamarin, Inc.
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
using Xamarin.AsyncTests.Constraints;

namespace Xamarin.WebTests.Tests
{
	using Handlers;
	using Framework;
	using Portable;
	using Resources;

	[SSL]
	[AsyncTestFixture (Timeout = 5000)]
	public class TestSSL : ITestHost<HttpServer>
	{
		[WebTestFeatures.SelectServerCertificate]
		public ServerCertificateType ServerCertificateType {
			get;
			private set;
		}

		public bool RejectServerCertificate {
			get;
			private set;
		}

		public HttpServer CreateInstance (TestContext ctx)
		{
			var support = DependencyInjector.Get<IPortableEndPointSupport> ();
			var endpoint = support.GetLoopbackEndpoint (9999);
			var certificate = ResourceManager.GetServerCertificate (ServerCertificateType);
			var flags = RejectServerCertificate ? ListenerFlags.ExpectTrustFailure : ListenerFlags.None;
			return new HttpServer (endpoint, flags, certificate);
		}

		class HttpsTestRunner : TestRunner
		{
			protected override Request CreateRequest (TestContext ctx, HttpServer server, Handler handler, Uri uri)
			{
				var request = new TraditionalRequest (uri);

				var validationProvider = DependencyInjector.Get<ICertificateValidationProvider> ();

				if (server.Flags == ListenerFlags.ExpectTrustFailure)
					request.Request.InstallCertificateValidator (validationProvider.RejectAll ());
				else {
					var validator = validationProvider.AcceptThisCertificate (server.ServerCertificate);
					request.Request.InstallCertificateValidator (validator);
				}

				return request;
			}

			protected override async Task<Response> RunInner (TestContext ctx, CancellationToken cancellationToken, HttpServer server, Handler handler, Request request)
			{
				var traditionalRequest = (TraditionalRequest)request;
				var response = await traditionalRequest.SendAsync (ctx, cancellationToken);

				var certificate = traditionalRequest.Request.GetCertificate ();
				ctx.Assert (certificate, Is.Not.Null, "certificate");

				return response;
			}
		}

		[Work]
		[CertificateTests]
		[AsyncTest]
		public Task RunCertificateTests (TestContext ctx, CancellationToken cancellationToken, [TestHost] HttpServer server, [GetHandler ("hello")] Handler handler)
		{
			var runner = new HttpsTestRunner ();
			if (server.Flags == ListenerFlags.ExpectTrustFailure)
				return runner.Run (ctx, cancellationToken, server, handler, null, HttpStatusCode.InternalServerError, WebExceptionStatus.TrustFailure);
			else
				return runner.Run (ctx, cancellationToken, server, handler);
		}
	}
}

