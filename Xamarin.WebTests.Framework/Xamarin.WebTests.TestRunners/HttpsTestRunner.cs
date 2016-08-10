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
using System.Net.Security;
using System.Threading;
using System.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;

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
	using Server;
	using Resources;

	[HttpsTestRunner]
	[FriendlyName ("[HttpsTestRunner]")]
	public class HttpsTestRunner : AbstractConnection
	{
		public ConnectionTestProvider Provider {
			get;
			private set;
		}

		protected Uri Uri {
			get;
			private set;
		}

		protected ListenerFlags ListenerFlags {
			get;
			private set;
		}

		protected bool ExternalServer {
			get { return (ListenerFlags & ListenerFlags.ExternalServer) != 0; }
		}

		public ISslStreamProvider SslStreamProvider {
			get;
			private set;
		}

		new public HttpsTestParameters Parameters {
			get { return (HttpsTestParameters)base.Parameters; }
		}

		MyServer server;

		public HttpsTestRunner (IPortableEndPoint endpoint, HttpsTestParameters parameters,
		                         ConnectionTestProvider provider, Uri uri, ListenerFlags flags)
			: base (endpoint, parameters)
		{
			Provider = provider;
			ListenerFlags = flags;
			Uri = uri;
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

			case ConnectionTestType.DontInvokeGlobalValidator:
				return new HttpsTestParameters (category, type, name, ResourceManager.ServerCertificateFromCA) {
					ClientCertificateValidator = acceptAll,
					GlobalValidationFlags = GlobalValidationFlags.SetToTestRunner | GlobalValidationFlags.MustNotInvoke
				};

			case ConnectionTestType.DontInvokeGlobalValidator2:
				return new HttpsTestParameters (category, type, name, ResourceManager.ServerCertificateFromCA) {
					ClientCertificateValidator = rejectAll,
					GlobalValidationFlags = GlobalValidationFlags.SetToTestRunner | GlobalValidationFlags.MustNotInvoke,
					ExpectTrustFailure = true, ClientAbortsHandshake = true
				};

			case ConnectionTestType.GlobalValidatorIsNull:
				return new HttpsTestParameters (category, type, name, ResourceManager.SelfSignedServerCertificate) {
					GlobalValidationFlags = GlobalValidationFlags.SetToNull,
					ExpectTrustFailure = true, ClientAbortsHandshake = true
				};

			case ConnectionTestType.MustInvokeGlobalValidator:
				return new HttpsTestParameters (category, type, name, ResourceManager.SelfSignedServerCertificate) {
					GlobalValidationFlags = GlobalValidationFlags.SetToTestRunner |
						GlobalValidationFlags.MustInvoke | GlobalValidationFlags.AlwaysSucceed
				};

			case ConnectionTestType.CheckChain:
				return new HttpsTestParameters (category, type, name, ResourceManager.SelfSignedServerCertificate) {
					GlobalValidationFlags = GlobalValidationFlags.CheckChain,
					ExpectPolicyErrors = SslPolicyErrors.RemoteCertificateChainErrors | SslPolicyErrors.RemoteCertificateNameMismatch,
					ExpectChainStatus = X509ChainStatusFlags.UntrustedRoot
				};

			case ConnectionTestType.ExternalServer:
				return new HttpsTestParameters (category, type, name, CertificateResourceType.TlsTestXamDevNew) {
					ExternalServer = new Uri ("https://tlstest-1.xamdev.com/"),
					GlobalValidationFlags = GlobalValidationFlags.CheckChain,
					ExpectPolicyErrors = SslPolicyErrors.None
				};

			case ConnectionTestType.ServerCertificateWithCA:
				var parameters = new HttpsTestParameters (category, type, name, ResourceManager.ServerCertificateWithCA) {
					GlobalValidationFlags = GlobalValidationFlags.SetToTestRunner,
					ExpectChainStatus = X509ChainStatusFlags.UntrustedRoot
				};
				parameters.ValidationParameters = new ValidationParameters ();
				parameters.ValidationParameters.AddExpectedExtraStore (CertificateResourceType.ServerCertificateFromLocalCA);
				parameters.ValidationParameters.AddExpectedExtraStore (CertificateResourceType.HamillerTubeCA);
				parameters.ValidationParameters.ExpectSuccess = false;
				return parameters;

			case ConnectionTestType.MartinTest:
				parameters = new HttpsTestParameters (category, type, name, ResourceManager.GetCertificate (CertificateResourceType.IntermediateServerCertificateBare)) {
					GlobalValidationFlags = GlobalValidationFlags.SetToTestRunner,
					ExpectChainStatus = X509ChainStatusFlags.NoError,
					ExpectPolicyErrors = SslPolicyErrors.None, OverrideTargetHost = "Intermediate-Server.local"
				};
				parameters.ValidationParameters = new ValidationParameters ();
				parameters.ValidationParameters.AddTrustedRoot (CertificateResourceType.HamillerTubeIM);
				parameters.ValidationParameters.AddExpectedExtraStore (CertificateResourceType.IntermediateServerCertificateNoKey);
				// parameters.ValidationParameters.AddExpectedExtraStore (CertificateResourceType.HamillerTubeIM);
				// parameters.ValidationParameters.AddExpectedExtraStore (CertificateResourceType.HamillerTubeCA);
				parameters.ValidationParameters.ExpectSuccess = true;
				return parameters;

			case ConnectionTestType.TrustedRootCA:
				parameters = new HttpsTestParameters (category, type, name, ResourceManager.ServerCertificateFromCA) {
					GlobalValidationFlags = GlobalValidationFlags.CheckChain,
					ExpectPolicyErrors = SslPolicyErrors.None, OverrideTargetHost = "Hamiller-Tube.local"
				};
				parameters.ValidationParameters = new ValidationParameters ();
				parameters.ValidationParameters.AddTrustedRoot (CertificateResourceType.HamillerTubeCA);
				parameters.ValidationParameters.ExpectSuccess = true;
				return parameters;

			case ConnectionTestType.TrustedIntermediateCA:
				parameters = new HttpsTestParameters (category, type, name, ResourceManager.GetCertificate (CertificateResourceType.IntermediateServerCertificateBare)) {
					GlobalValidationFlags = GlobalValidationFlags.SetToTestRunner,
					ExpectChainStatus = X509ChainStatusFlags.NoError,
					ExpectPolicyErrors = SslPolicyErrors.None, OverrideTargetHost = "Intermediate-Server.local"
				};
				parameters.ValidationParameters = new ValidationParameters ();
				parameters.ValidationParameters.AddTrustedRoot (CertificateResourceType.HamillerTubeIM);
				parameters.ValidationParameters.AddExpectedExtraStore (CertificateResourceType.IntermediateServerCertificateNoKey);
				// parameters.ValidationParameters.AddExpectedExtraStore (CertificateResourceType.HamillerTubeIM);
				// parameters.ValidationParameters.AddExpectedExtraStore (CertificateResourceType.HamillerTubeCA);
				parameters.ValidationParameters.ExpectSuccess = true;
				return parameters;

			case ConnectionTestType.HostNameMismatch:
				parameters = new HttpsTestParameters (category, type, name, ResourceManager.ServerCertificateFromCA) {
					GlobalValidationFlags = GlobalValidationFlags.CheckChain,
					ExpectPolicyErrors = SslPolicyErrors.RemoteCertificateNameMismatch,
				};
				parameters.ValidationParameters = new ValidationParameters ();
				parameters.ValidationParameters.AddTrustedRoot (CertificateResourceType.HamillerTubeCA);
				parameters.ValidationParameters.ExpectSuccess = false;
				return parameters;

			case ConnectionTestType.IntermediateServerCertificate:
				parameters = new HttpsTestParameters (category, type, name, ResourceManager.GetCertificate (CertificateResourceType.IntermediateServerCertificate)) {
					GlobalValidationFlags = GlobalValidationFlags.SetToTestRunner,
					ExpectChainStatus = X509ChainStatusFlags.NoError,
					ExpectPolicyErrors = SslPolicyErrors.None, OverrideTargetHost = "Intermediate-Server.local"
				};
				parameters.ValidationParameters = new ValidationParameters ();
				parameters.ValidationParameters.AddTrustedRoot (CertificateResourceType.HamillerTubeCA);
				parameters.ValidationParameters.AddExpectedExtraStore (CertificateResourceType.IntermediateServerCertificateNoKey);
				parameters.ValidationParameters.AddExpectedExtraStore (CertificateResourceType.HamillerTubeIM);
				parameters.ValidationParameters.ExpectSuccess = true;
				return parameters;

			case ConnectionTestType.IntermediateServerCertificateFull:
				parameters = new HttpsTestParameters (category, type, name, ResourceManager.GetCertificate (CertificateResourceType.IntermediateServerCertificateFull)) {
					GlobalValidationFlags = GlobalValidationFlags.SetToTestRunner,
					ExpectChainStatus = X509ChainStatusFlags.NoError,
					ExpectPolicyErrors = SslPolicyErrors.None, OverrideTargetHost = "Intermediate-Server.local"
				};
				parameters.ValidationParameters = new ValidationParameters ();
				parameters.ValidationParameters.AddTrustedRoot (CertificateResourceType.HamillerTubeCA);
				parameters.ValidationParameters.AddExpectedExtraStore (CertificateResourceType.IntermediateServerCertificateNoKey);
				parameters.ValidationParameters.AddExpectedExtraStore (CertificateResourceType.HamillerTubeIM);
				parameters.ValidationParameters.AddExpectedExtraStore (CertificateResourceType.HamillerTubeCA);
				parameters.ValidationParameters.ExpectSuccess = true;
				return parameters;

			case ConnectionTestType.IntermediateServerCertificateBare:
				parameters = new HttpsTestParameters (category, type, name, ResourceManager.GetCertificate (CertificateResourceType.IntermediateServerCertificateBare)) {
					GlobalValidationFlags = GlobalValidationFlags.SetToTestRunner,
					ExpectPolicyErrors = SslPolicyErrors.RemoteCertificateChainErrors, OverrideTargetHost = "Intermediate-Server.local"
				};
				parameters.ValidationParameters = new ValidationParameters ();
				parameters.ValidationParameters.AddTrustedRoot (CertificateResourceType.HamillerTubeCA);
				parameters.ValidationParameters.AddExpectedExtraStore (CertificateResourceType.IntermediateServerCertificateNoKey);
				parameters.ValidationParameters.ExpectSuccess = false;
				return parameters;

			default:
				throw new InternalErrorException ();
			}
		}

		Handler CreateHandler (TestContext ctx)
		{
			if (ExternalServer)
				return null;
			else
				return new HelloWorldHandler ("hello");
		}

		public Task Run (TestContext ctx, CancellationToken cancellationToken)
		{
			var handler = CreateHandler (ctx);
			var impl = new MyRunner (this, server, handler);

			HttpStatusCode expectedStatus;
			WebExceptionStatus expectedException;

			if (Parameters.ExpectTrustFailure) {
				expectedStatus = HttpStatusCode.InternalServerError;
				expectedException = WebExceptionStatus.TrustFailure;
			} else if (Parameters.ExpectWebException) {
				expectedStatus = HttpStatusCode.InternalServerError;
				expectedException = WebExceptionStatus.AnyErrorStatus;
			} else {
				expectedStatus = HttpStatusCode.OK;
				expectedException = WebExceptionStatus.Success;
			}

			if (ExternalServer)
				return impl.RunExternal (ctx, cancellationToken, Uri, expectedStatus, expectedException);
			else
				return impl.Run (ctx, cancellationToken, expectedStatus, expectedException);
		}

		protected Request CreateRequest (TestContext ctx, Uri uri)
		{
			ctx.LogMessage ("Create request: {0}", uri);
			var webRequest = Provider.Client.SslStreamProvider.CreateWebRequest (uri, Parameters);

			var request = new TraditionalRequest (webRequest);

			if (Parameters.OverrideTargetHost != null)
				request.RequestExt.Host = Parameters.OverrideTargetHost;

			request.RequestExt.SetKeepAlive (true);

			if (Parameters.ClientCertificateValidator != null)
				request.RequestExt.InstallCertificateValidator (Parameters.ClientCertificateValidator.ValidationCallback);

			if (Parameters.ClientCertificate != null) {
				var certificates = new X509CertificateCollection ();
				certificates.Add (Parameters.ClientCertificate);
				request.RequestExt.SetClientCertificates (certificates);
			}

			if (ExternalServer) {
				var servicePoint = ServicePointManager.FindServicePoint (Parameters.ExternalServer);
				if (servicePoint != null)
					servicePoint.CloseConnectionGroup (null);
			}

			return request;
		}

		bool HasFlag (GlobalValidationFlags flag)
		{
			return (Parameters.GlobalValidationFlags & flag) == flag;
		}

		RemoteCertificateValidationCallback savedGlobalCallback;
		TestContext savedContext;
		bool restoreGlobalCallback;
		int globalValidatorInvoked;

		void SetGlobalValidationCallback (TestContext ctx, RemoteCertificateValidationCallback callback)
		{
			ctx.LogMessage ("Set validator: {0}", callback != null);
			savedGlobalCallback = ServicePointManager.ServerCertificateValidationCallback;
			ServicePointManager.ServerCertificateValidationCallback = callback;
			savedContext = ctx;
			restoreGlobalCallback = true;
		}

		bool GlobalValidator (object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors errors)
		{
			return GlobalValidator (savedContext, certificate, chain, errors);
		}

		bool GlobalValidator (TestContext ctx, X509Certificate certificate, X509Chain chain, SslPolicyErrors errors)
		{
			ctx.LogMessage ("Global validator: {0}", globalValidatorInvoked);
			if (HasFlag (GlobalValidationFlags.MustNotInvoke)) {
				ctx.AssertFail ("Global validator has been invoked!");
				return false;
			}

			++globalValidatorInvoked;

			bool result = errors == SslPolicyErrors.None;

			if (Parameters.ValidationParameters != null) {
				CertificateInfoTestRunner.CheckValidationResult (ctx, Parameters.ValidationParameters, certificate, chain, errors);
				result = true;
			}

			if (HasFlag (GlobalValidationFlags.CheckChain)) {
				CertificateInfoTestRunner.CheckCallbackChain (ctx, Parameters, certificate, chain, errors);
				result = true;
			}

			if (HasFlag (GlobalValidationFlags.AlwaysFail))
				return false;
			else if (HasFlag (GlobalValidationFlags.AlwaysSucceed))
				return true;

			return result;
		}

		protected override async Task Initialize (TestContext ctx, CancellationToken cancellationToken)
		{
			if (!ExternalServer) {
				server = new MyServer (this);
				await server.Initialize (ctx, cancellationToken);
			}
		}

		protected override async Task PreRun (TestContext ctx, CancellationToken cancellationToken)
		{
			if (HasFlag (GlobalValidationFlags.CheckChain)) {
				Parameters.GlobalValidationFlags |= GlobalValidationFlags.SetToTestRunner;
			} else if (!HasFlag (GlobalValidationFlags.SetToTestRunner)) {
				ctx.Assert (Parameters.ExpectChainStatus, Is.Null, "Parameters.ExpectChainStatus");
				ctx.Assert (Parameters.ExpectPolicyErrors, Is.Null, "Parameters.ExpectPolicyErrors");
			}

			if (HasFlag (GlobalValidationFlags.SetToNull))
				SetGlobalValidationCallback (ctx, null);
			else if (HasFlag (GlobalValidationFlags.SetToTestRunner))
				SetGlobalValidationCallback (ctx, GlobalValidator);

			if (!ExternalServer)
				await server.PreRun (ctx, cancellationToken);
		}

		protected override async Task PostRun (TestContext ctx, CancellationToken cancellationToken)
		{
			if (!ExternalServer)
				await server.PostRun (ctx, cancellationToken).ConfigureAwait (false);

			if (restoreGlobalCallback)
				ServicePointManager.ServerCertificateValidationCallback = savedGlobalCallback;

			if (HasFlag (GlobalValidationFlags.MustInvoke) || HasFlag (GlobalValidationFlags.CheckChain))
				ctx.Assert (globalValidatorInvoked, Is.EqualTo (1), "global validator has been invoked");
		}

		protected override async Task Destroy (TestContext ctx, CancellationToken cancellationToken)
		{
			if (!ExternalServer) {
				try {
					await server.Destroy (ctx, cancellationToken);
				} finally {
					server = null;
				}
			}
		}

		protected override void Stop ()
		{
		}

		protected async Task<Response> RunInner (TestContext ctx, CancellationToken cancellationToken, Request request)
		{
			var traditionalRequest = (TraditionalRequest)request;
			var response = await traditionalRequest.SendAsync (ctx, cancellationToken);
			if (!response.IsSuccess)
				return response;

			var provider = DependencyInjector.Get<ICertificateProvider> ();

			var certificate = traditionalRequest.RequestExt.GetCertificate ();
			ctx.Assert (certificate, Is.Not.Null, "certificate");

			var expectedCert = Parameters.ServerCertificate;
			if (ExternalServer) {
				if (expectedCert == null && Parameters.CertificateType != null)
					expectedCert = ResourceManager.GetCertificate (Parameters.CertificateType.Value);
			} else {
				ctx.Assert (expectedCert, Is.Not.Null, "must set Parameters.ServerCertificate");
			}

			if (expectedCert != null)
				ctx.Assert (provider.AreEqual (certificate, expectedCert), "correct certificate");

			var clientCertificate = traditionalRequest.RequestExt.GetClientCertificate ();
			if ((Parameters.AskForClientCertificate || Parameters.RequireClientCertificate) && Parameters.ClientCertificate != null) {
				ctx.Assert (clientCertificate, Is.Not.Null, "client certificate");
				ctx.Assert (provider.AreEqual (clientCertificate, Parameters.ClientCertificate), "correct client certificate");
			}

			return response;
		}

		class MyServer : HttpServer
		{
			public HttpsTestRunner Parent {
				get;
				private set;
			}

			new public HttpsTestParameters Parameters {
				get { return (HttpsTestParameters)base.Parameters; }
			}

			public MyServer (HttpsTestRunner parent)
				: base (parent.Uri, parent.Parameters.ListenAddress, parent.ListenerFlags, parent.Parameters, parent.SslStreamProvider)
			{
				Parent = parent;
			}

			protected override HttpConnection CreateConnection (TestContext ctx, Stream stream)
			{
				try {
					ctx.LogDebug (5, "HttpTestRunner - CreateConnection");
					var connection = base.CreateConnection (ctx, stream);

					/*
					 * There seems to be some kind of a race condition here.
					 *
					 * When the client aborts the handshake due the a certificate validation failure,
					 * then we either receive an exception during the TLS handshake or the connection
					 * will be closed when the handshake is completed.
					 *
					 */
					var haveReq = connection.HasRequest ();
					ctx.LogDebug (5, "HttpTestRunner - CreateConnection #1: {0}", haveReq);
					if (Parameters.ClientAbortsHandshake) {
						ctx.Assert (haveReq, Is.False, "expected client to abort handshake");
						return null;
					} else {
						ctx.Assert (haveReq, Is.True, "expected non-empty request");
					}
					return connection;
				} catch (Exception ex) {
					if (Parameters.ClientAbortsHandshake) {
						ctx.LogDebug (5, "HttpTestRunner - CreateConnection got expected exception");
						return null;
					}
					ctx.LogDebug (5, "HttpTestRunner - CreateConnection ex: {0}", ex);
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
		}

		class MyRunner : TestRunner
		{
			readonly HttpsTestRunner parent;

			public MyRunner (HttpsTestRunner runner, HttpServer server, Handler handler)
				: base (server, handler)
			{
				this.parent = runner;
			}

			protected override string Name {
				get { return string.Format ("[{0}:{1}]", parent.GetType ().Name, parent.Parameters.Identifier); }
			}

			protected override Request CreateRequest (TestContext ctx, Uri uri)
			{
				return parent.CreateRequest (ctx, uri);
			}
			protected override Task<Response> RunInner (TestContext ctx, CancellationToken cancellationToken, Request request)
			{
				return parent.RunInner (ctx, cancellationToken, request);
			}
		}
	}
}
