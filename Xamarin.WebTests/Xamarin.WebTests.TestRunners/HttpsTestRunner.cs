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
using System.IO;
using System.Text;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;

using Xamarin.AsyncTests;
using Xamarin.AsyncTests.Constraints;
using Xamarin.AsyncTests.Portable;
using Xamarin.AsyncTests.Framework;

namespace Xamarin.WebTests.TestRunners
{
	using ConnectionFramework;
	using HttpFramework;
	using HttpHandlers;
	using Providers;
	using Portable;
	using Resources;

	[FriendlyName ("[HttpsTestRunner]")]
	public class HttpsTestRunner : HttpServer
	{
		public IClientAndServerParameters Parameters {
			get;
			private set;
		}

		public IClientParameters ClientParameters {
			get { return Parameters.ClientParameters; }
		}

		public HttpsTestRunner (IHttpProvider provider, IPortableEndPoint endpoint, ListenerFlags flags, IClientAndServerParameters parameters)
			: base (provider, endpoint, flags, parameters.ServerParameters)
		{
			Parameters = parameters;
		}

		public static IEnumerable<IClientAndServerParameters> GetParameters (TestContext ctx, string filter)
		{
			var certificateProvider = DependencyInjector.Get<ICertificateProvider> ();
			var acceptAll = certificateProvider.AcceptAll ();
			var rejectAll = certificateProvider.RejectAll ();
			var acceptSelfSigned = certificateProvider.AcceptThisCertificate (ResourceManager.SelfSignedServerCertificate);
			var acceptFromLocalCA = certificateProvider.AcceptFromCA (ResourceManager.LocalCACertificate);

			var defaultServer = new ServerParameters ("default", ResourceManager.DefaultServerCertificate);
			var selfSignedServer = new ServerParameters ("self-signed", ResourceManager.SelfSignedServerCertificate);
			var serverFromCA = new ServerParameters ("server-ca", ResourceManager.ServerCertificateFromCA);

			var acceptAllClient = new ClientParameters ("accept-all") { CertificateValidator = acceptAll };

			yield return ClientAndServerParameters.Create (acceptAllClient, defaultServer);
			yield return ClientAndServerParameters.Create (acceptAllClient, selfSignedServer);

			yield return ClientAndServerParameters.Create (new ClientParameters ("accept-local-ca") {
				CertificateValidator = acceptFromLocalCA
			}, serverFromCA);

			// The default validator only allows ResourceManager.DefaultServerCertificate.
			yield return ClientAndServerParameters.Create (new ClientParameters ("no-validator") {
				ExpectTrustFailure = true
			}, new ServerParameters ("self-signed-error", ResourceManager.SelfSignedServerCertificate) {
				ExpectEmptyRequest = true
			});

			// Explicit validator overrides the default ServicePointManager.ServerCertificateValidationCallback.
			yield return ClientAndServerParameters.Create (new ClientParameters ("reject-all") {
				ExpectTrustFailure = true, CertificateValidator = rejectAll
			}, new ServerParameters ("default", ResourceManager.DefaultServerCertificate) {
				ExpectEmptyRequest = true
			});

			// Provide a client certificate, but do not require it.
			yield return ClientAndServerParameters.Create (new ClientParameters ("client-certificate") {
				ClientCertificate = ResourceManager.MonkeyCertificate, CertificateValidator = acceptSelfSigned
			}, selfSignedServer);

			// Request client certificate, but do not require it.
			yield return ClientAndServerParameters.Create (new ClientParameters ("client-certificate") {
				ClientCertificate = ResourceManager.MonkeyCertificate, CertificateValidator = acceptSelfSigned
			}, new ServerParameters ("request-certificate", ResourceManager.SelfSignedServerCertificate) {
				AskForClientCertificate = true, CertificateValidator = acceptFromLocalCA
			});

			// Require client certificate.
			yield return ClientAndServerParameters.Create (new ClientParameters ("client-certificate") {
				ClientCertificate = ResourceManager.MonkeyCertificate, CertificateValidator = acceptSelfSigned
			}, new ServerParameters ("require-certificate", ResourceManager.SelfSignedServerCertificate) {
				RequireClientCertificate = true, CertificateValidator = acceptFromLocalCA
			});

			// Request client certificate without requiring one and do not provide it.
			yield return ClientAndServerParameters.Create (new ClientParameters ("no-certificate") {
				CertificateValidator = acceptSelfSigned
			}, new ServerParameters ("request-certificate", ResourceManager.SelfSignedServerCertificate) {
				AskForClientCertificate = true, CertificateValidator = acceptFromLocalCA
			});

			// Reject client certificate.
			yield return ClientAndServerParameters.Create (new ClientParameters ("reject-certificate") {
				ClientCertificate = ResourceManager.MonkeyCertificate, CertificateValidator = acceptSelfSigned,
				ExpectWebException = true
			}, new ServerParameters ("request-certificate", ResourceManager.SelfSignedServerCertificate) {
				AskForClientCertificate = true, CertificateValidator = rejectAll, ExpectEmptyRequest = true
			});

			// Missing client certificate.
			yield return ClientAndServerParameters.Create (new ClientParameters ("no-certificate") {
				CertificateValidator = acceptSelfSigned, ExpectWebException = true
			}, new ServerParameters ("missing-certificate", ResourceManager.SelfSignedServerCertificate) {
				RequireClientCertificate = true, ExpectEmptyRequest = true
			});

		}

		public Task Run (TestContext ctx, Handler handler, CancellationToken cancellationToken)
		{
			var impl = new TestRunnerImpl (this, handler);
			if (ClientParameters.ExpectTrustFailure)
				return impl.Run (ctx, cancellationToken, HttpStatusCode.InternalServerError, WebExceptionStatus.TrustFailure);
			else if (ClientParameters.ExpectWebException)
				return impl.Run (ctx, cancellationToken, HttpStatusCode.InternalServerError, WebExceptionStatus.AnyErrorStatus);
			else
				return impl.Run (ctx, cancellationToken, HttpStatusCode.OK, WebExceptionStatus.Success);
		}

		protected override HttpConnection CreateConnection (TestContext ctx, Stream stream)
		{
			try {
				var connection = base.CreateConnection (ctx, stream);

				/*
				 * There seems to be some kind of a race condition here.
				 *
				 * When the client shuts down the connection due to auth failure, then we
				 * either crash during the TLS handshake or receive a closed connection
				 * when the handshake is completed.
				 *
				 */
				var haveReq = connection.HasRequest();
				if (ServerParameters.ExpectEmptyRequest) {
					ctx.Assert (haveReq, Is.False, "expected empty request");
					return null;
				} else {
					ctx.Assert (haveReq, Is.True, "expected non-empty request");
				}
				return connection;
			} catch {
				if (ServerParameters.ExpectEmptyRequest)
					return null;
				throw;
			}
		}

		protected override bool HandleConnection (TestContext ctx, HttpConnection connection)
		{
			if (ServerParameters.RequireClientCertificate)
				ctx.Expect (connection.SslStream.HasClientCertificate, Is.True, "client certificate");

			return base.HandleConnection (ctx, connection);
		}

		protected Request CreateRequest (TestContext ctx, Uri uri)
		{
			var webRequest = HttpProvider.CreateWebRequest (uri);
			webRequest.SetKeepAlive (true);

			var request = new TraditionalRequest (webRequest);

			if (ClientParameters.CertificateValidator != null)
				request.Request.InstallCertificateValidator (ClientParameters.CertificateValidator);

			if (ClientParameters.ClientCertificate != null)
				request.Request.SetClientCertificates (new IClientCertificate[] { ClientParameters.ClientCertificate });

			return request;
		}

		protected async Task<Response> RunInner (TestContext ctx, CancellationToken cancellationToken, Request request)
		{
			var traditionalRequest = (TraditionalRequest)request;
			var response = await traditionalRequest.SendAsync (ctx, cancellationToken);

			var provider = DependencyInjector.Get<ICertificateProvider> ();

			var certificate = traditionalRequest.Request.GetCertificate ();
			ctx.Assert (certificate, Is.Not.Null, "certificate");
			ctx.Assert (provider.AreEqual (certificate, ServerParameters.ServerCertificate), "correct certificate");

			var clientCertificate = traditionalRequest.Request.GetClientCertificate ();
			if (ServerParameters.AskForClientCertificate && ClientParameters.ClientCertificate != null) {
				ctx.Assert (clientCertificate, Is.Not.Null, "client certificate");
				ctx.Assert (provider.AreEqual (clientCertificate, ClientParameters.ClientCertificate), "correct client certificate");
			} else {
				ctx.Assert (clientCertificate, Is.Null, "no client certificate");
			}

			return response;
		}

		class TestRunnerImpl : TestRunner
		{
			readonly HttpsTestRunner runner;

			public TestRunnerImpl (HttpsTestRunner runner, Handler handler)
				: base (runner, handler)
			{
				this.runner = runner;
			}

			protected override Request CreateRequest (TestContext ctx, Uri uri)
			{
				return runner.CreateRequest (ctx, uri);
			}
			protected override Task<Response> RunInner (TestContext ctx, CancellationToken cancellationToken, Request request)
			{
				return runner.RunInner (ctx, cancellationToken, request);
			}
		}
	}
}
