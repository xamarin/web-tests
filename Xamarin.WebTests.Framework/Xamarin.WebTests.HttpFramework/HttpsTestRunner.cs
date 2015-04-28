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

			ListenerFlags flags;
			switch (mode) {
			case SslTestMode.Default:
				flags = ListenerFlags.None;
				break;
			case SslTestMode.RejectServerCertificate:
				flags = ListenerFlags.ExpectTrustFailure | ListenerFlags.RejectServerCertificate;
				break;
			case SslTestMode.RequireClientCertificate:
				flags = ListenerFlags.ProvideClientCertificate | ListenerFlags.RequireClientCertificate;
				break;
			case SslTestMode.RejectClientCertificate:
				flags = ListenerFlags.RejectClientCertificate | ListenerFlags.ExpectError |
					ListenerFlags.ProvideClientCertificate | ListenerFlags.RequireClientCertificate;
				break;
			case SslTestMode.MissingClientCertificate:
				flags = ListenerFlags.RequireClientCertificate | ListenerFlags.ExpectError;
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
		protected override Request CreateRequest (TestContext ctx, HttpServer server, Handler handler, Uri uri)
		{
			var webRequest = server.HttpProvider.CreateWebRequest (uri);
			webRequest.SetKeepAlive (true);

			var request = new TraditionalRequest (webRequest);

			var provider = DependencyInjector.Get<ICertificateProvider> ();

			if ((server.Flags & ListenerFlags.RejectServerCertificate) != 0)
				request.Request.InstallCertificateValidator (provider.RejectAll ());
			else {
				var validator = provider.AcceptThisCertificate (server.ServerCertificate);
				request.Request.InstallCertificateValidator (validator);
			}

			if ((server.Flags & ListenerFlags.ProvideClientCertificate) != 0) {
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

			var clientCertificate = traditionalRequest.Request.GetClientCertificate ();
			if ((server.Flags & ListenerFlags.ProvideClientCertificate) != 0) {
				ctx.Assert (clientCertificate, Is.Not.Null, "client certificate");
				ctx.Assert (provider.AreEqual (clientCertificate, ResourceManager.MonkeyCertificate), "correct client certificate");
			} else {
				ctx.Assert (clientCertificate, Is.Null, "no client certificate");
			}

			return response;
		}

		public static Task Run (TestContext ctx, CancellationToken cancellationToken, HttpServer server, Handler handler)
		{
			var runner = new HttpsTestRunner ();
			if ((server.Flags & ListenerFlags.ExpectTrustFailure) != 0)
				return runner.Run (ctx, cancellationToken, server, handler, null, HttpStatusCode.InternalServerError, WebExceptionStatus.TrustFailure);
			else if ((server.Flags & ListenerFlags.ExpectError) != 0)
				return runner.Run (ctx, cancellationToken, server, handler, null, HttpStatusCode.InternalServerError, WebExceptionStatus.AnyErrorStatus);
			else
				return runner.Run (ctx, cancellationToken, server, handler, null, HttpStatusCode.OK, WebExceptionStatus.Success);
		}
	}
}

