//
// HttpsTestRunner.cs
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
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Xamarin.AsyncTests;
using Xamarin.AsyncTests.Constraints;
using Xamarin.AsyncTests.Portable;

namespace Xamarin.WebTests.Framework
{
	using Handlers;
	using Portable;
	using Resources;

	public class HttpsTestRunner : TestRunner
	{
		public static HttpServer CreateServer (TestContext ctx, IPortableEndPoint endpoint, HttpTestMode mode, IServerCertificate certificate)
		{
			ListenerFlags flags;
			switch (mode) {
			case HttpTestMode.Default:
				flags = ListenerFlags.None;
				break;
			case HttpTestMode.ReuseConnection:
				flags = ListenerFlags.ReuseConnection;
				break;
			case HttpTestMode.RejectServerCertificate:
				flags = ListenerFlags.ExpectTrustFailure;
				break;
			case HttpTestMode.RequireClientCertificate:
				flags = ListenerFlags.RequireClientCertificate;
				break;
			default:
				throw new InvalidOperationException ();
			}

			return new HttpServer (endpoint, flags, certificate);
		}

		protected override Request CreateRequest (TestContext ctx, HttpServer server, Handler handler, Uri uri)
		{
			var request = new TraditionalRequest (uri);

			var provider = DependencyInjector.Get<ICertificateProvider> ();

			if (server.Flags == ListenerFlags.ExpectTrustFailure)
				request.Request.InstallCertificateValidator (provider.RejectAll ());
			else {
				var validator = provider.AcceptThisCertificate (server.ServerCertificate);
				request.Request.InstallCertificateValidator (validator);
			}

			if (server.Flags == ListenerFlags.RequireClientCertificate) {
				var clientCertificate = ResourceManager.MonkeyCertificate;
				request.Request.SetClientCertificates (new IClientCertificate[] { clientCertificate });
			}

			return request;
		}

		protected override async Task<Response> RunInner (TestContext ctx, CancellationToken cancellationToken, HttpServer server, Handler handler, Request request)
		{
			var traditionalRequest = (TraditionalRequest)request;
			var response = await traditionalRequest.SendAsync (ctx, cancellationToken);

			var provider = DependencyInjector.Get<ICertificateProvider> ();

			var certificate = traditionalRequest.Request.GetCertificate ();
			ctx.Assert (certificate, Is.Not.Null, "certificate");
			ctx.Assert (provider.AreEqual (certificate, server.ServerCertificate), "correct certificate");

			if (server.Flags == ListenerFlags.RequireClientCertificate) {
				var clientCertificate = traditionalRequest.Request.GetClientCertificate ();
				ctx.Assert (clientCertificate, Is.Not.Null, "client certificate");
				ctx.Assert (provider.AreEqual (clientCertificate, ResourceManager.MonkeyCertificate), "correct client certificate");
			}

			return response;
		}

		public static Task RunStatic (TestContext ctx, CancellationToken cancellationToken, HttpServer server, Handler handler)
		{
			var runner = new HttpsTestRunner ();
			if (server.Flags == ListenerFlags.ExpectTrustFailure)
				return runner.Run (ctx, cancellationToken, server, handler, null, HttpStatusCode.InternalServerError, WebExceptionStatus.TrustFailure);
			else
				return runner.Run (ctx, cancellationToken, server, handler);
		}
	}
}

