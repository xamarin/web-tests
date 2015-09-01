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
using System.Linq;
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
	using TestFramework;
	using HttpFramework;
	using HttpHandlers;
	using Providers;
	using Portable;
	using Resources;

	[HttpsTestRunner]
	[FriendlyName ("[HttpsTestRunner]")]
	public class HttpsTestRunner : HttpServer
	{
		new public HttpsTestParameters Parameters {
			get { return (HttpsTestParameters)base.Parameters; }
		}

		public HttpsTestRunner (IHttpProvider provider, IPortableEndPoint endpoint, ListenerFlags flags, HttpsTestParameters parameters)
			: base (provider, endpoint, endpoint, flags, parameters)
		{
		}

		static string GetTestName (ConnectionTestCategory category, ConnectionTestType type, params object[] args)
		{
			var sb = new StringBuilder ();
			sb.Append (type);
			foreach (var arg in args) {
				sb.AppendFormat (":{0}", arg);
			}
			return sb.ToString ();
		}

		public static HttpsTestParameters GetParameters (TestContext ctx, ConnectionTestCategory category, ConnectionTestType type)
		{
			var certificateProvider = DependencyInjector.Get<ICertificateProvider> ();
			var acceptAll = certificateProvider.AcceptAll ();
			var rejectAll = certificateProvider.RejectAll ();
			var acceptNull = certificateProvider.AcceptNull ();
			var acceptSelfSigned = certificateProvider.AcceptThisCertificate (ResourceManager.SelfSignedServerCertificate);
			var acceptFromLocalCA = certificateProvider.AcceptFromCA (ResourceManager.LocalCACertificate);

			var name = GetTestName (category, type);

			switch (type) {
			case ConnectionTestType.Default:
				return new HttpsTestParameters (category, type, name, ResourceManager.SelfSignedServerCertificate) {
					ClientCertificateValidator = acceptAll
				};

			case ConnectionTestType.AcceptFromLocalCA:
				return new HttpsTestParameters (category, type, name, ResourceManager.ServerCertificateFromCA) {
					ClientCertificateValidator = acceptFromLocalCA
				};

			case ConnectionTestType.NoValidator:
				// The default validator only allows ResourceManager.SelfSignedServerCertificate.
				return new HttpsTestParameters (category, type, name, ResourceManager.ServerCertificateFromCA) {
					ExpectTrustFailure = true, ClientAbortsHandshake = true
				};

			case ConnectionTestType.RejectAll:
				// Explicit validator overrides the default ServicePointManager.ServerCertificateValidationCallback.
				return new HttpsTestParameters (category, type, name, ResourceManager.SelfSignedServerCertificate) {
					ExpectTrustFailure = true, ClientCertificateValidator = rejectAll,
					ClientAbortsHandshake = true
				};

			case ConnectionTestType.UnrequestedClientCertificate:
				// Provide a client certificate, but do not require it.
				return new HttpsTestParameters (category, type, name, ResourceManager.SelfSignedServerCertificate) {
					ClientCertificate = ResourceManager.PenguinCertificate, ClientCertificateValidator = acceptSelfSigned,
					ServerCertificateValidator = acceptNull
				};

			case ConnectionTestType.RequestClientCertificate:
				/*
				 * Request client certificate, but do not require it.
				 *
				 * FIXME:
				 * SslStream with Mono's old implementation fails here.
				 */
				return new HttpsTestParameters (category, type, name, ResourceManager.SelfSignedServerCertificate) {
					ClientCertificate = ResourceManager.MonkeyCertificate, ClientCertificateValidator = acceptSelfSigned,
					AskForClientCertificate = true, ServerCertificateValidator = acceptFromLocalCA
				};

			case ConnectionTestType.RequireClientCertificate:
				// Require client certificate.
				return new HttpsTestParameters (category, type, name, ResourceManager.SelfSignedServerCertificate) {
					ClientCertificate = ResourceManager.MonkeyCertificate, ClientCertificateValidator = acceptSelfSigned,
					AskForClientCertificate = true, RequireClientCertificate = true,
					ServerCertificateValidator = acceptFromLocalCA
				};

			case ConnectionTestType.OptionalClientCertificate:
				/*
				 * Request client certificate without requiring one and do not provide it.
				 *
				 * To ask for an optional client certificate (without requiring it), you need to specify a custom validation
				 * callback and then accept the null certificate with `SslPolicyErrors.RemoteCertificateNotAvailable' in it.
				 *
				 * FIXME:
				 * Mono with the old TLS implementation throws SecureChannelFailure.
				 */
				return new HttpsTestParameters (category, type, name, ResourceManager.SelfSignedServerCertificate) {
					ClientCertificateValidator = acceptSelfSigned, AskForClientCertificate = true,
					ServerCertificateValidator = acceptNull
				};

			case ConnectionTestType.RejectClientCertificate:
				// Reject client certificate.
				return new HttpsTestParameters (category, type, name, ResourceManager.SelfSignedServerCertificate) {
					ClientCertificate = ResourceManager.MonkeyCertificate, ClientCertificateValidator = acceptSelfSigned,
					ExpectWebException = true, ServerCertificateValidator = rejectAll,
					AskForClientCertificate = true, ClientAbortsHandshake = true
				};

			case ConnectionTestType.MissingClientCertificate:
				// Missing client certificate.
				return new HttpsTestParameters (category, type, name, ResourceManager.SelfSignedServerCertificate) {
					ClientCertificateValidator = acceptSelfSigned, ExpectWebException = true,
					AskForClientCertificate = true, RequireClientCertificate = true,
					ClientAbortsHandshake = true
				};

			default:
				throw new InvalidOperationException ();
			}
		}

		public Task Run (TestContext ctx, CancellationToken cancellationToken)
		{
			var handler = new HelloWorldHandler ("hello");
			return Run (ctx, handler, cancellationToken);
		}

		public Task Run (TestContext ctx, Handler handler, CancellationToken cancellationToken)
		{
			var impl = new TestRunnerImpl (this, handler);
			if (Parameters.ExpectTrustFailure)
				return impl.Run (ctx, cancellationToken, HttpStatusCode.InternalServerError, WebExceptionStatus.TrustFailure);
			else if (Parameters.ExpectWebException)
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
				 * When the client aborts the handshake due the a certificate validation failure,
				 * then we either receive an exception during the TLS handshake or the connection
				 * will be closed when the handshake is completed.
				 *
				 */
				var haveReq = connection.HasRequest();
				if (Parameters.ClientAbortsHandshake) {
					ctx.Assert (haveReq, Is.False, "expected client to abort handshake");
					return null;
				} else {
					ctx.Assert (haveReq, Is.True, "expected non-empty request");
				}
				return connection;
			} catch {
				if (Parameters.ClientAbortsHandshake)
					return null;
				throw;
			}
		}

		protected override bool HandleConnection (TestContext ctx, HttpConnection connection)
		{
			ctx.Expect (connection.SslStream.IsAuthenticated, "server is authenticated");
			if (Parameters.RequireClientCertificate)
				ctx.Expect (connection.SslStream.IsMutuallyAuthenticated, "server is mutually authenticated");

			return base.HandleConnection (ctx, connection);
		}

		protected Request CreateRequest (TestContext ctx, Uri uri)
		{
			var webRequest = HttpProvider.CreateWebRequest (uri);
			webRequest.SetKeepAlive (true);

			var request = new TraditionalRequest (webRequest);

			if (Parameters.ClientCertificateValidator != null)
				request.Request.InstallCertificateValidator (Parameters.ClientCertificateValidator);

			if (Parameters.ClientCertificate != null)
				request.Request.SetClientCertificates (new IClientCertificate[] { Parameters.ClientCertificate });

			return request;
		}

		protected async Task<Response> RunInner (TestContext ctx, CancellationToken cancellationToken, Request request)
		{
			var traditionalRequest = (TraditionalRequest)request;
			var response = await traditionalRequest.SendAsync (ctx, cancellationToken);

			var provider = DependencyInjector.Get<ICertificateProvider> ();

			var certificate = traditionalRequest.Request.GetCertificate ();
			ctx.Assert (certificate, Is.Not.Null, "certificate");
			ctx.Assert (provider.AreEqual (certificate, Parameters.ServerCertificate), "correct certificate");

			var clientCertificate = traditionalRequest.Request.GetClientCertificate ();
			if ((Parameters.AskForClientCertificate || Parameters.RequireClientCertificate) && Parameters.ClientCertificate != null) {
				ctx.Assert (clientCertificate, Is.Not.Null, "client certificate");
				ctx.Assert (provider.AreEqual (clientCertificate, Parameters.ClientCertificate), "correct client certificate");
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
