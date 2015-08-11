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
	using HttpFramework;
	using HttpHandlers;
	using Features;
	using Providers;
	using Portable;
	using Resources;

	[FriendlyName ("[HttpsTestRunner]")]
	public class HttpsTestRunner : HttpServer
	{
		public ClientAndServerParameters Parameters {
			get;
			private set;
		}

		public ClientParameters ClientParameters {
			get { return Parameters.ClientParameters; }
		}

		public HttpsTestRunner (IHttpProvider provider, IPortableEndPoint endpoint, ListenerFlags flags, ClientAndServerParameters parameters)
			: base (provider, endpoint, flags, parameters.ServerParameters)
		{
			Parameters = parameters;
		}

		[Obsolete]
		public static IEnumerable<HttpsTestType> GetHttpsTestTypes (TestContext ctx, string filter)
		{
			if (filter == null)
				return AllHttpsTestTypes;

			var parts = filter.Split (':');
			return AllHttpsTestTypes.Where (t => parts.Contains (t.ToString ()));
		}

		public static IEnumerable<HttpsTestType> GetHttpsTestTypes (TestContext ctx, ConnectionTestCategory category)
		{
			switch (category) {
			case ConnectionTestCategory.Https:
				return AllHttpsTestTypes;
			case ConnectionTestCategory.HttpsWithMono:
				return OldMonoTestTypes;
			case ConnectionTestCategory.HttpsWithDotNet:
				return DotNetTestTypes;
			default:
				ctx.AssertFail ("Unsupported test category: '{0}'.", category);
				throw new NotImplementedException ();
			}
		}

		[Obsolete]
		public static bool IsSupported (TestContext ctx, HttpsTestType type)
		{
			var providerType = CommonHttpFeatures.GetConnectionProviderType (ctx);

			var includeNotWorking = ctx.IsEnabled (IncludeNotWorkingAttribute.Instance) || ctx.CurrentCategory == NotWorkingAttribute.Instance;
			var isNewTls = CommonHttpFeatures.IsNewTls (providerType);

			if (isNewTls || includeNotWorking)
				return true;

			return OldMonoTestTypes.Contains (type);
		}

		static IEnumerable<HttpsTestType> OldMonoTestTypes {
			get {
				yield return HttpsTestType.Default;
				yield return HttpsTestType.AcceptFromLocalCA;
				yield return HttpsTestType.RejectAll;
				yield return HttpsTestType.UnrequestedClientCertificate;
				yield return HttpsTestType.RejectClientCertificate;
				yield return HttpsTestType.MissingClientCertificate;
			}
		}

		static IEnumerable<HttpsTestType> AllHttpsTestTypes {
			get {
				yield return HttpsTestType.Default;
				yield return HttpsTestType.AcceptFromLocalCA;
				yield return HttpsTestType.NoValidator;
				yield return HttpsTestType.RejectAll;
				yield return HttpsTestType.RequestClientCertificate;
				yield return HttpsTestType.RequireClientCertificate;
				yield return HttpsTestType.RejectClientCertificate;
				yield return HttpsTestType.UnrequestedClientCertificate;
				yield return HttpsTestType.OptionalClientCertificate;
				yield return HttpsTestType.RejectClientCertificate;
				yield return HttpsTestType.MissingClientCertificate;
			}
		}

		static IEnumerable<HttpsTestType> DotNetTestTypes {
			get {
				yield return HttpsTestType.NoValidator;
				yield return HttpsTestType.RequestClientCertificate;
				yield return HttpsTestType.RequireClientCertificate;
				yield return HttpsTestType.RejectClientCertificate;
				yield return HttpsTestType.UnrequestedClientCertificate;
				yield return HttpsTestType.OptionalClientCertificate;
				yield return HttpsTestType.RejectClientCertificate;
				yield return HttpsTestType.MissingClientCertificate;
			}
		}

		public static ClientAndServerParameters GetParameters (TestContext ctx, HttpsTestType type)
		{
			var certificateProvider = DependencyInjector.Get<ICertificateProvider> ();
			var acceptAll = certificateProvider.AcceptAll ();
			var rejectAll = certificateProvider.RejectAll ();
			var acceptNull = certificateProvider.AcceptNull ();
			var acceptSelfSigned = certificateProvider.AcceptThisCertificate (ResourceManager.SelfSignedServerCertificate);
			var acceptFromLocalCA = certificateProvider.AcceptFromCA (ResourceManager.LocalCACertificate);

			var selfSignedServer = new ServerParameters ("self-signed", ResourceManager.SelfSignedServerCertificate);

			var acceptAllClient = new ClientParameters ("accept-all") { ClientCertificateValidator = acceptAll };

			switch (type) {
			case HttpsTestType.Default:
				return new ClientAndServerParameters (acceptAllClient, selfSignedServer);
			case HttpsTestType.AcceptFromLocalCA:
				return new ClientAndServerParameters ("accept-local-ca", ResourceManager.ServerCertificateFromCA) {
					ClientCertificateValidator = acceptFromLocalCA
				};

			case HttpsTestType.NoValidator:
				// The default validator only allows ResourceManager.DefaultServerCertificate.
				return new ClientAndServerParameters ("no-validator", ResourceManager.SelfSignedServerCertificate) {
					ClientFlags = ClientFlags.ExpectTrustFailure, ServerFlags = ServerFlags.ClientAbortsHandshake
				};

			case HttpsTestType.RejectAll:
				// Explicit validator overrides the default ServicePointManager.ServerCertificateValidationCallback.
				return new ClientAndServerParameters ("reject-all", ResourceManager.SelfSignedServerCertificate) {
					ClientFlags = ClientFlags.ExpectTrustFailure, ClientCertificateValidator = rejectAll,
					ServerFlags = ServerFlags.ClientAbortsHandshake
				};

			case HttpsTestType.UnrequestedClientCertificate:
				// Provide a client certificate, but do not require it.
				return new ClientAndServerParameters ("unrequested-client-certificate", ResourceManager.SelfSignedServerCertificate) {
					ClientCertificate = ResourceManager.PenguinCertificate, ClientCertificateValidator = acceptSelfSigned,
					ServerCertificateValidator = acceptNull
				};

			case HttpsTestType.RequestClientCertificate:
				/*
				 * Request client certificate, but do not require it.
				 *
				 * FIXME:
				 * SslStream with Mono's old implementation fails here.
				 */
				return new ClientAndServerParameters ("request-client-certificate", ResourceManager.SelfSignedServerCertificate) {
					ClientCertificate = ResourceManager.MonkeyCertificate, ClientCertificateValidator = acceptSelfSigned,
					ServerFlags = ServerFlags.AskForClientCertificate, ServerCertificateValidator = acceptFromLocalCA
				};

			case HttpsTestType.RequireClientCertificate:
				// Require client certificate.
				return new ClientAndServerParameters ("require-client-certificate", ResourceManager.SelfSignedServerCertificate) {
					ClientCertificate = ResourceManager.MonkeyCertificate, ClientCertificateValidator = acceptSelfSigned,
					ServerFlags = ServerFlags.AskForClientCertificate | ServerFlags.RequireClientCertificate,
					ServerCertificateValidator = acceptFromLocalCA
				};

			case HttpsTestType.OptionalClientCertificate:
				/*
				 * Request client certificate without requiring one and do not provide it.
				 *
				 * To ask for an optional client certificate (without requiring it), you need to specify a custom validation
				 * callback and then accept the null certificate with `SslPolicyErrors.RemoteCertificateNotAvailable' in it.
				 *
				 * FIXME:
				 * Mono with the old TLS implementation throws SecureChannelFailure.
				 */
				return new ClientAndServerParameters ("optional-client-certificate", ResourceManager.SelfSignedServerCertificate) {
					ClientCertificateValidator = acceptSelfSigned, ServerFlags = ServerFlags.AskForClientCertificate,
					ServerCertificateValidator = acceptNull
				};

			case HttpsTestType.RejectClientCertificate:
				// Reject client certificate.
				return new ClientAndServerParameters ("reject-client-certificate", ResourceManager.SelfSignedServerCertificate) {
					ClientCertificate = ResourceManager.MonkeyCertificate, ClientCertificateValidator = acceptSelfSigned,
					ClientFlags = ClientFlags.ExpectWebException, ServerCertificateValidator = rejectAll,
					ServerFlags = ServerFlags.AskForClientCertificate | ServerFlags.ClientAbortsHandshake | ServerFlags.ExpectServerException
				};

			case HttpsTestType.MissingClientCertificate:
				// Missing client certificate.
				return new ClientAndServerParameters ("missing-client-certificate", ResourceManager.SelfSignedServerCertificate) {
					ClientCertificateValidator = acceptSelfSigned, ClientFlags = ClientFlags.ExpectWebException,
					ServerFlags = ServerFlags.AskForClientCertificate | ServerFlags.RequireClientCertificate |
					ServerFlags.ClientAbortsHandshake | ServerFlags.ExpectServerException
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
			if ((ClientParameters.Flags & ClientFlags.ExpectTrustFailure) != 0)
				return impl.Run (ctx, cancellationToken, HttpStatusCode.InternalServerError, WebExceptionStatus.TrustFailure);
			else if ((ClientParameters.Flags & ClientFlags.ExpectWebException) != 0)
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
				if ((ServerParameters.Flags & ServerFlags.ClientAbortsHandshake) != 0) {
					ctx.Assert (haveReq, Is.False, "expected client to abort handshake");
					return null;
				} else {
					ctx.Assert (haveReq, Is.True, "expected non-empty request");
				}
				return connection;
			} catch {
				if ((ServerParameters.Flags & ServerFlags.ClientAbortsHandshake) != 0)
					return null;
				throw;
			}
		}

		protected override bool HandleConnection (TestContext ctx, HttpConnection connection)
		{
			ctx.Expect (connection.SslStream.IsAuthenticated, "server is authenticated");
			if ((ServerParameters.Flags & ServerFlags.RequireClientCertificate) != 0)
				ctx.Expect (connection.SslStream.IsMutuallyAuthenticated, "server is mutually authenticated");

			return base.HandleConnection (ctx, connection);
		}

		protected Request CreateRequest (TestContext ctx, Uri uri)
		{
			var webRequest = HttpProvider.CreateWebRequest (uri);
			webRequest.SetKeepAlive (true);

			var request = new TraditionalRequest (webRequest);

			if (ClientParameters.ClientCertificateValidator != null)
				request.Request.InstallCertificateValidator (ClientParameters.ClientCertificateValidator);

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
			if (((ServerParameters.Flags & (ServerFlags.AskForClientCertificate|ServerFlags.RequireClientCertificate)) != 0) && ClientParameters.ClientCertificate != null) {
				ctx.Assert (clientCertificate, Is.Not.Null, "client certificate");
				ctx.Assert (provider.AreEqual (clientCertificate, ClientParameters.ClientCertificate), "correct client certificate");
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
