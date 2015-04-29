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

namespace Xamarin.WebTests.HttpFramework
{
	using HttpHandlers;
	using Portable;
	using Providers;
	using Resources;

	public class HttpsTestHost : TestHostAttribute, ITestHost<HttpServer>
	{
		public HttpsTestHost ()
			: base (typeof (HttpsTestHost))
		{
		}

		#region ITestHost implementation
		public HttpServer CreateInstance (TestContext ctx)
		{
			var support = DependencyInjector.Get<IPortableEndPointSupport> ();
			var endpoint = support.GetLoopbackEndpoint (9999);

			var mode = ctx.GetParameter<SslTestMode> ();

			SslStreamFlags flags;
			switch (mode) {
			case SslTestMode.Default:
				flags = SslStreamFlags.None;
				break;
			case SslTestMode.RejectServerCertificate:
				flags = SslStreamFlags.ExpectTrustFailure | SslStreamFlags.RejectServerCertificate;
				break;
			case SslTestMode.RequireClientCertificate:
				flags = SslStreamFlags.ProvideClientCertificate | SslStreamFlags.RequireClientCertificate;
				break;
			case SslTestMode.RejectClientCertificate:
				flags = SslStreamFlags.RejectClientCertificate | SslStreamFlags.ExpectError |
					SslStreamFlags.ProvideClientCertificate | SslStreamFlags.RequireClientCertificate;
				break;
			case SslTestMode.MissingClientCertificate:
				flags = SslStreamFlags.RequireClientCertificate | SslStreamFlags.ExpectError;
				break;
			default:
				throw new InvalidOperationException ();
			}

			HttpProviderType providerType;
			if (!ctx.TryGetParameter (out providerType))
				providerType = HttpProviderType.Default;

			var factory = DependencyInjector.Get<IHttpProviderFactory> ();
			var provider = factory.GetProvider (providerType);

			ServerCertificateType certificateType;
			if (!ctx.TryGetParameter (out certificateType))
				certificateType = ServerCertificateType.Default;

			var certificate = ResourceManager.GetServerCertificate (certificateType);
			return provider.CreateServer (endpoint, flags, certificate);
		}
		#endregion
	}

	public class HttpsTestRunner : TestRunner
	{
		public HttpsTestRunner (HttpServer server, Handler handler)
			: base (server, handler)
		{
		}

		protected override Request CreateRequest (TestContext ctx, Uri uri)
		{
			var webRequest = Server.HttpProvider.CreateWebRequest (uri);
			webRequest.SetKeepAlive (true);

			var request = new TraditionalRequest (webRequest);

			var provider = DependencyInjector.Get<ICertificateProvider> ();

			if ((Server.SslStreamFlags & SslStreamFlags.RejectServerCertificate) != 0)
				request.Request.InstallCertificateValidator (provider.RejectAll ());
			else {
				var validator = provider.AcceptThisCertificate (Server.ServerCertificate);
				request.Request.InstallCertificateValidator (validator);
			}

			if ((Server.SslStreamFlags & SslStreamFlags.ProvideClientCertificate) != 0) {
				var clientCertificate = ResourceManager.MonkeyCertificate;
				request.Request.SetClientCertificates (new IClientCertificate[] { clientCertificate });
			}

			return request;
		}

		protected override async Task<Response> RunInner (TestContext ctx, CancellationToken cancellationToken, Request request)
		{
			var traditionalRequest = (TraditionalRequest)request;
			var response = await traditionalRequest.SendAsync (ctx, cancellationToken);

			var provider = DependencyInjector.Get<ICertificateProvider> ();

			var certificate = traditionalRequest.Request.GetCertificate ();
			ctx.Assert (certificate, Is.Not.Null, "certificate");
			ctx.Assert (provider.AreEqual (certificate, Server.ServerCertificate), "correct certificate");

			var clientCertificate = traditionalRequest.Request.GetClientCertificate ();
			if ((Server.SslStreamFlags & SslStreamFlags.ProvideClientCertificate) != 0) {
				ctx.Assert (clientCertificate, Is.Not.Null, "client certificate");
				ctx.Assert (provider.AreEqual (clientCertificate, ResourceManager.MonkeyCertificate), "correct client certificate");
			} else {
				ctx.Assert (clientCertificate, Is.Null, "no client certificate");
			}

			return response;
		}

		public static Task Run (TestContext ctx, CancellationToken cancellationToken, HttpServer server, Handler handler)
		{
			var runner = new HttpsTestRunner (server, handler);
			if ((server.SslStreamFlags & SslStreamFlags.ExpectTrustFailure) != 0)
				return runner.Run (ctx, cancellationToken, null, HttpStatusCode.InternalServerError, WebExceptionStatus.TrustFailure);
			else if ((server.SslStreamFlags & SslStreamFlags.ExpectError) != 0)
				return runner.Run (ctx, cancellationToken, null, HttpStatusCode.InternalServerError, WebExceptionStatus.AnyErrorStatus);
			else
				return runner.Run (ctx, cancellationToken, null, HttpStatusCode.OK, WebExceptionStatus.Success);
		}
	}
}

