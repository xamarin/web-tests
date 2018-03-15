//
// HttpValidationTestRunner.cs
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
using System.Net.Sockets;
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
	using TestAttributes;
	using TestFramework;
	using HttpFramework;
	using HttpHandlers;
	using Server;
	using Resources;

	[HttpValidationTestRunner]
	[FriendlyName ("[HttpValidationTestRunner]")]
	public class HttpValidationTestRunner : AbstractConnection
	{
		public ConnectionTestProvider Provider {
			get;
		}

		protected Uri Uri {
			get;
		}

		protected HttpServerFlags ServerFlags {
			get;
		}

		protected bool ExternalServer {
			get { return (ServerFlags & HttpServerFlags.ExternalServer) != 0; }
		}

		public HttpServer Server {
			get;
		}

		public HttpValidationTestParameters Parameters {
			get;
		}

		public string ME {
			get;
		}

		public HttpValidationTestRunner (IPortableEndPoint endpoint, HttpValidationTestParameters parameters,
		                                 ConnectionTestProvider provider, Uri uri, HttpServerFlags flags)
		{
			Parameters = parameters;
			Provider = provider;
			ServerFlags = flags;
			Uri = uri;

			Server = new BuiltinHttpServer (uri, endpoint, ServerFlags, parameters, null);

			ME = $"{GetType ().Name}({EffectiveType})";
		}

		static string GetTestName (ConnectionTestCategory category, HttpValidationTestType type, params object[] args)
		{
			var sb = new StringBuilder ();
			sb.Append (type);
			foreach (var arg in args) {
				sb.AppendFormat (":{0}", arg);
			}
			return sb.ToString ();
		}

		const HttpValidationTestType MartinTest = HttpValidationTestType.ExternalServer;

		public static IEnumerable<HttpValidationTestType> GetTests (TestContext ctx, ConnectionTestCategory category)
		{
			switch (category) {
			case ConnectionTestCategory.Https:
				yield return HttpValidationTestType.Default;
				yield return HttpValidationTestType.AcceptFromLocalCA;
				yield return HttpValidationTestType.NoValidator;
				yield return HttpValidationTestType.RejectAll;
				yield return HttpValidationTestType.RequestClientCertificate;
				yield return HttpValidationTestType.RequireClientCertificate;
				yield return HttpValidationTestType.RejectClientCertificate;
				yield return HttpValidationTestType.UnrequestedClientCertificate;
				yield return HttpValidationTestType.OptionalClientCertificate;
				yield return HttpValidationTestType.RejectClientCertificate;
				yield return HttpValidationTestType.MissingClientCertificate;
				yield break;

			case ConnectionTestCategory.HttpsWithMono:
				yield return HttpValidationTestType.Default;
				yield return HttpValidationTestType.AcceptFromLocalCA;
				yield return HttpValidationTestType.RejectAll;
				yield break;

			case ConnectionTestCategory.HttpsWithDotNet:
				yield return HttpValidationTestType.NoValidator;
				yield return HttpValidationTestType.RequestClientCertificate;
				yield return HttpValidationTestType.RequireClientCertificate;
				yield return HttpValidationTestType.RejectClientCertificate;
				yield return HttpValidationTestType.UnrequestedClientCertificate;
				yield return HttpValidationTestType.OptionalClientCertificate;
				yield return HttpValidationTestType.RejectClientCertificate;
				yield return HttpValidationTestType.MissingClientCertificate;
				yield break;

			case ConnectionTestCategory.HttpsCertificateValidators:
				yield return HttpValidationTestType.DontInvokeGlobalValidator;
				yield return HttpValidationTestType.DontInvokeGlobalValidator2;
				yield return HttpValidationTestType.GlobalValidatorIsNull;
				yield return HttpValidationTestType.MustInvokeGlobalValidator;
				yield break;

			case ConnectionTestCategory.NotYetWorking:
				yield return HttpValidationTestType.ExternalServer;
				yield return HttpValidationTestType.CheckChain;
				yield break;

			case ConnectionTestCategory.TrustedRoots:
				yield return HttpValidationTestType.ServerCertificateWithCA;
				yield return HttpValidationTestType.TrustedRootCA;
				yield return HttpValidationTestType.TrustedIntermediateCA;
				yield return HttpValidationTestType.TrustedSelfSigned;
				yield return HttpValidationTestType.HostNameMismatch;
				yield return HttpValidationTestType.IntermediateServerCertificate;
				yield return HttpValidationTestType.IntermediateServerCertificateFull;
				yield return HttpValidationTestType.IntermediateServerCertificateBare;
				yield break;

			case ConnectionTestCategory.CertificateStore:
				yield return HttpValidationTestType.CertificateStore;
				yield break;

			case ConnectionTestCategory.MartinTest:
				yield return HttpValidationTestType.MartinTest;
				yield break;

			default:
				ctx.AssertFail ("Unsupported test category: '{0}'.", category);
				throw new InternalErrorException ();
			}
		}

		public static HttpValidationTestParameters GetParameters (TestContext ctx, ConnectionTestCategory category, HttpValidationTestType type)
		{
			var certificateProvider = DependencyInjector.Get<ICertificateProvider> ();
			var acceptAll = certificateProvider.AcceptAll ();
			var rejectAll = certificateProvider.RejectAll ();
			var acceptNull = certificateProvider.AcceptNull ();
			var acceptSelfSigned = certificateProvider.AcceptThisCertificate (ResourceManager.SelfSignedServerCertificate);
			var acceptFromLocalCA = certificateProvider.AcceptFromCA (ResourceManager.LocalCACertificate);

			var name = GetTestName (category, type);

			var effectiveType = type == HttpValidationTestType.MartinTest ? MartinTest : type;

			switch (effectiveType) {
			case HttpValidationTestType.Default:
				return new HttpValidationTestParameters (category, type, name, ResourceManager.SelfSignedServerCertificate) {
					ClientCertificateValidator = acceptAll
				};

			case HttpValidationTestType.AcceptFromLocalCA:
				return new HttpValidationTestParameters (category, type, name, ResourceManager.ServerCertificateFromCA) {
					ClientCertificateValidator = acceptFromLocalCA
				};

			case HttpValidationTestType.NoValidator:
				// The default validator only allows ResourceManager.SelfSignedServerCertificate.
				return new HttpValidationTestParameters (category, type, name, ResourceManager.ServerCertificateFromCA) {
					ExpectedStatus = HttpStatusCode.InternalServerError,
					ExpectedError = WebExceptionStatus.TrustFailure,
					Flags = HttpOperationFlags.ClientAbortsHandshake
				};

			case HttpValidationTestType.RejectAll:
				// Explicit validator overrides the default ServicePointManager.ServerCertificateValidationCallback.
				return new HttpValidationTestParameters (category, type, name, ResourceManager.SelfSignedServerCertificate) {
					ClientCertificateValidator = rejectAll,
					ExpectedStatus = HttpStatusCode.InternalServerError,
					ExpectedError = WebExceptionStatus.TrustFailure,
					Flags = HttpOperationFlags.ClientAbortsHandshake
				};

			case HttpValidationTestType.UnrequestedClientCertificate:
				// Provide a client certificate, but do not require it.
				return new HttpValidationTestParameters (category, type, name, ResourceManager.SelfSignedServerCertificate) {
					ClientCertificate = ResourceManager.PenguinCertificate, ClientCertificateValidator = acceptSelfSigned,
					ServerCertificateValidator = acceptNull
				};

			case HttpValidationTestType.RequestClientCertificate:
				/*
				 * Request client certificate, but do not require it.
				 *
				 * FIXME:
				 * SslStream with Mono's old implementation fails here.
				 */
				return new HttpValidationTestParameters (category, type, name, ResourceManager.SelfSignedServerCertificate) {
					ClientCertificate = ResourceManager.MonkeyCertificate, ClientCertificateValidator = acceptSelfSigned,
					AskForClientCertificate = true, ServerCertificateValidator = acceptFromLocalCA
				};

			case HttpValidationTestType.RequireClientCertificate:
				// Require client certificate.
				return new HttpValidationTestParameters (category, type, name, ResourceManager.SelfSignedServerCertificate) {
					ClientCertificate = ResourceManager.MonkeyCertificate, ClientCertificateValidator = acceptSelfSigned,
					AskForClientCertificate = true, RequireClientCertificate = true,
					ServerCertificateValidator = acceptFromLocalCA,
					Flags = HttpOperationFlags.RequireClientCertificate
				};

			case HttpValidationTestType.OptionalClientCertificate:
				/*
				 * Request client certificate without requiring one and do not provide it.
				 *
				 * To ask for an optional client certificate (without requiring it), you need to specify a custom validation
				 * callback and then accept the null certificate with `SslPolicyErrors.RemoteCertificateNotAvailable' in it.
				 *
				 * FIXME:
				 * Mono with the old TLS implementation throws SecureChannelFailure.
				 */
				return new HttpValidationTestParameters (category, type, name, ResourceManager.SelfSignedServerCertificate) {
					ClientCertificateValidator = acceptSelfSigned, AskForClientCertificate = true,
					ServerCertificateValidator = acceptNull
				};

			case HttpValidationTestType.RejectClientCertificate:
				// Reject client certificate.
				return new HttpValidationTestParameters (category, type, name, ResourceManager.SelfSignedServerCertificate) {
					ClientCertificate = ResourceManager.MonkeyCertificate, ClientCertificateValidator = acceptSelfSigned,
					ServerCertificateValidator = rejectAll,
					AskForClientCertificate = true,
					ExpectedStatus = HttpStatusCode.InternalServerError,
					ExpectedError = WebExceptionStatus.AnyErrorStatus,
					Flags = HttpOperationFlags.ClientAbortsHandshake
				};

			case HttpValidationTestType.MissingClientCertificate:
				// Missing client certificate.
				return new HttpValidationTestParameters (category, type, name, ResourceManager.SelfSignedServerCertificate) {
					ClientCertificateValidator = acceptSelfSigned,
					AskForClientCertificate = true, RequireClientCertificate = true,
					ExpectedStatus = HttpStatusCode.InternalServerError,
					ExpectedError = WebExceptionStatus.AnyErrorStatus,
					Flags = HttpOperationFlags.ClientAbortsHandshake | HttpOperationFlags.RequireClientCertificate
				};

			case HttpValidationTestType.DontInvokeGlobalValidator:
				return new HttpValidationTestParameters (category, type, name, ResourceManager.ServerCertificateFromCA) {
					ClientCertificateValidator = acceptAll,
					GlobalValidationFlags = GlobalValidationFlags.SetToTestRunner | GlobalValidationFlags.MustNotInvoke
				};

			case HttpValidationTestType.DontInvokeGlobalValidator2:
				return new HttpValidationTestParameters (category, type, name, ResourceManager.ServerCertificateFromCA) {
					ClientCertificateValidator = rejectAll,
					GlobalValidationFlags = GlobalValidationFlags.SetToTestRunner | GlobalValidationFlags.MustNotInvoke,
					ExpectedStatus = HttpStatusCode.InternalServerError,
					ExpectedError = WebExceptionStatus.TrustFailure,
					Flags = HttpOperationFlags.ClientAbortsHandshake
				};

			case HttpValidationTestType.GlobalValidatorIsNull:
				return new HttpValidationTestParameters (category, type, name, ResourceManager.SelfSignedServerCertificate) {
					GlobalValidationFlags = GlobalValidationFlags.SetToNull,
					ExpectedStatus = HttpStatusCode.InternalServerError,
					ExpectedError = WebExceptionStatus.TrustFailure,
					Flags = HttpOperationFlags.ClientAbortsHandshake
				};

			case HttpValidationTestType.MustInvokeGlobalValidator:
				return new HttpValidationTestParameters (category, type, name, ResourceManager.SelfSignedServerCertificate) {
					GlobalValidationFlags = GlobalValidationFlags.SetToTestRunner |
						GlobalValidationFlags.MustInvoke | GlobalValidationFlags.AlwaysSucceed
				};

			case HttpValidationTestType.CheckChain:
				return new HttpValidationTestParameters (category, type, name, ResourceManager.SelfSignedServerCertificate) {
					GlobalValidationFlags = GlobalValidationFlags.CheckChain,
					ExpectPolicyErrors = SslPolicyErrors.RemoteCertificateChainErrors | SslPolicyErrors.RemoteCertificateNameMismatch,
					ExpectChainStatus = X509ChainStatusFlags.UntrustedRoot
				};

			case HttpValidationTestType.ExternalServer:
				return new HttpValidationTestParameters (category, type, name, CertificateResourceType.TlsTestInternal) {
					ExternalServer = ResourceManager.TlsTest1Uri,
					GlobalValidationFlags = GlobalValidationFlags.CheckChain,
					ExpectPolicyErrors = SslPolicyErrors.None
				};

			case HttpValidationTestType.ServerCertificateWithCA:
				var parameters = new HttpValidationTestParameters (category, type, name, ResourceManager.ServerCertificateWithCA) {
					GlobalValidationFlags = GlobalValidationFlags.SetToTestRunner,
					ExpectChainStatus = X509ChainStatusFlags.UntrustedRoot
				};
				parameters.ValidationParameters = new ValidationParameters ();
				parameters.ValidationParameters.AddExpectedExtraStore (CertificateResourceType.ServerCertificateFromLocalCA);
				parameters.ValidationParameters.AddExpectedExtraStore (CertificateResourceType.HamillerTubeCA);
				parameters.ValidationParameters.ExpectSuccess = false;
				return parameters;

			case HttpValidationTestType.TrustedRootCA:
				parameters = new HttpValidationTestParameters (category, type, name, ResourceManager.ServerCertificateFromCA) {
					GlobalValidationFlags = GlobalValidationFlags.CheckChain,
					ExpectPolicyErrors = SslPolicyErrors.None, OverrideTargetHost = "Hamiller-Tube.local"
				};
				parameters.ValidationParameters = new ValidationParameters ();
				parameters.ValidationParameters.AddTrustedRoot (CertificateResourceType.HamillerTubeCA);
				parameters.ValidationParameters.ExpectSuccess = true;
				return parameters;

			case HttpValidationTestType.TrustedIntermediateCA:
				parameters = new HttpValidationTestParameters (category, type, name, ResourceManager.GetCertificate (CertificateResourceType.IntermediateServerCertificateBare)) {
					GlobalValidationFlags = GlobalValidationFlags.SetToTestRunner,
					ExpectChainStatus = X509ChainStatusFlags.NoError,
					ExpectPolicyErrors = SslPolicyErrors.None, OverrideTargetHost = "Intermediate-Server.local"
				};
				parameters.ValidationParameters = new ValidationParameters ();
				parameters.ValidationParameters.AddTrustedRoot (CertificateResourceType.HamillerTubeIM);
				parameters.ValidationParameters.AddExpectedExtraStore (CertificateResourceType.IntermediateServerCertificateNoKey);
				parameters.ValidationParameters.ExpectSuccess = true;
				return parameters;

			case HttpValidationTestType.TrustedSelfSigned:
				parameters = new HttpValidationTestParameters (category, type, name, ResourceManager.SelfSignedServerCertificate) {
					GlobalValidationFlags = GlobalValidationFlags.SetToTestRunner,
					ExpectChainStatus = X509ChainStatusFlags.NoError,
					ExpectPolicyErrors = SslPolicyErrors.None, OverrideTargetHost = "Hamiller-Tube.local"
				};
				parameters.ValidationParameters = new ValidationParameters ();
				parameters.ValidationParameters.AddTrustedRoot (CertificateResourceType.SelfSignedServerCertificate);
				parameters.ValidationParameters.ExpectSuccess = true;
				return parameters;

			case HttpValidationTestType.HostNameMismatch:
				parameters = new HttpValidationTestParameters (category, type, name, ResourceManager.ServerCertificateFromCA) {
					GlobalValidationFlags = GlobalValidationFlags.CheckChain,
					ExpectPolicyErrors = SslPolicyErrors.RemoteCertificateNameMismatch,
				};
				parameters.ValidationParameters = new ValidationParameters ();
				parameters.ValidationParameters.AddTrustedRoot (CertificateResourceType.HamillerTubeCA);
				parameters.ValidationParameters.ExpectSuccess = false;
				return parameters;

			case HttpValidationTestType.IntermediateServerCertificate:
				parameters = new HttpValidationTestParameters (category, type, name, ResourceManager.GetCertificate (CertificateResourceType.IntermediateServerCertificate)) {
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

			case HttpValidationTestType.IntermediateServerCertificateFull:
				parameters = new HttpValidationTestParameters (category, type, name, ResourceManager.GetCertificate (CertificateResourceType.IntermediateServerCertificateFull)) {
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

			case HttpValidationTestType.IntermediateServerCertificateBare:
				parameters = new HttpValidationTestParameters (category, type, name, ResourceManager.GetCertificate (CertificateResourceType.IntermediateServerCertificateBare)) {
					GlobalValidationFlags = GlobalValidationFlags.SetToTestRunner,
					ExpectPolicyErrors = SslPolicyErrors.RemoteCertificateChainErrors, OverrideTargetHost = "Intermediate-Server.local"
				};
				parameters.ValidationParameters = new ValidationParameters ();
				parameters.ValidationParameters.AddTrustedRoot (CertificateResourceType.HamillerTubeCA);
				parameters.ValidationParameters.AddExpectedExtraStore (CertificateResourceType.IntermediateServerCertificateNoKey);
				parameters.ValidationParameters.ExpectSuccess = false;
				return parameters;

			case HttpValidationTestType.CertificateStore:
				parameters = new HttpValidationTestParameters (category, type, name, ResourceManager.GetCertificate (CertificateResourceType.ServerFromTrustedIntermediateCABare)) {
					GlobalValidationFlags = GlobalValidationFlags.SetToTestRunner,
					ExpectChainStatus = X509ChainStatusFlags.NoError,
					ExpectPolicyErrors = SslPolicyErrors.None, OverrideTargetHost = "Trusted-IM-Server.local"
				};
				return parameters;

			case HttpValidationTestType.Abort:
				return new HttpValidationTestParameters (category, type, name, ResourceManager.SelfSignedServerCertificate) {
					ClientCertificateValidator = acceptAll,
					ExpectedStatus = HttpStatusCode.InternalServerError,
					ExpectedError = WebExceptionStatus.RequestCanceled,
					Flags = HttpOperationFlags.ExpectServerException
				};

			default:
				throw new InternalErrorException ();
			}
		}

		public HttpValidationTestType EffectiveType {
			get {
				if (Parameters.Type == HttpValidationTestType.MartinTest)
					return MartinTest;
				return Parameters.Type;
			}
		}

		static Handler GetBigChunkedHandler ()
		{
			var chunks = new List<string> ();
			for (var i = 'A'; i < 'Z'; i++) {
				chunks.Add (new string (i, 1000));
			}

			var content = new ChunkedContent (chunks);

			return new PostHandler ("Big Chunked", content, TransferMode.Chunked);
		}

		Handler CreateHandler (TestContext ctx)
		{
			if (ExternalServer)
				return new ExternalHandler ("external", HttpStatusCode.OK);
			if (EffectiveType == HttpValidationTestType.Abort)
				return new AbortHandler ("abort");
			if (Parameters.ChunkedResponse)
				return new GetHandler ("chunked", HttpContent.HelloChunked);
			return new HelloWorldHandler ("hello");
		}

		public Task Run (TestContext ctx, CancellationToken cancellationToken)
		{
			var handler = CreateHandler (ctx);

			var operation = new Operation (this, handler);

			if (ExternalServer)
				return operation.RunExternal (ctx, Uri, cancellationToken);

			return operation.Run (ctx, cancellationToken);
		}

		protected Request CreateRequest (TestContext ctx, Uri uri)
		{
			ctx.LogDebug (LogCategories.Https, 1, "Create request: {0}", uri);
			var webRequest = Provider.Client.SslStreamProvider.CreateWebRequest (uri, Parameters);

			var request = new TraditionalRequest (webRequest);

			if (false && Parameters.Type == HttpValidationTestType.MartinTest) {
				request.RequestExt.Timeout = 1500;
			}

			if (Parameters.SendChunked)
				request.RequestExt.SetSendChunked (true);

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
			ctx.LogDebug (LogCategories.Https, 1, "Set validator: {0}", callback != null);
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
			ctx.LogDebug (LogCategories.Https, 1, "Global validator: {0}", globalValidatorInvoked);
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
			if (!ExternalServer)
				await Server.Initialize (ctx, cancellationToken);
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
				await Server.PreRun (ctx, cancellationToken);
		}

		protected override async Task PostRun (TestContext ctx, CancellationToken cancellationToken)
		{
			if (!ExternalServer)
				await Server.PostRun (ctx, cancellationToken).ConfigureAwait (false);

			if (restoreGlobalCallback)
				ServicePointManager.ServerCertificateValidationCallback = savedGlobalCallback;

			if (HasFlag (GlobalValidationFlags.MustInvoke) || HasFlag (GlobalValidationFlags.CheckChain))
				ctx.Assert (globalValidatorInvoked, Is.EqualTo (1), "global validator has been invoked");
		}

		protected override async Task Destroy (TestContext ctx, CancellationToken cancellationToken)
		{
			if (!ExternalServer)
				await Server.Destroy (ctx, cancellationToken);
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

		class Operation : HttpOperation
		{
			public HttpValidationTestRunner Parent {
				get;
			}

			public Operation (HttpValidationTestRunner parent, Handler handler)
				: base (parent.Server, parent.ME, handler, parent.Parameters.Flags,
				        parent.Parameters.ExpectedStatus, parent.Parameters.ExpectedError)
			{
				Parent = parent;
			}

			protected override Request CreateRequest (TestContext ctx, Uri uri)
			{
				return Parent.CreateRequest (ctx, uri);
			}

			protected override void ConfigureRequest (TestContext ctx, Uri uri, Request request)
			{
				Handler.ConfigureRequest (ctx, request, uri);

				request.SetProxy (Parent.Server.GetProxy ());
			}

			protected override Task<Response> RunInner (TestContext ctx, Request request, CancellationToken cancellationToken)
			{
				return Parent.RunInner (ctx, cancellationToken, request);
			}

			protected override void Destroy ()
			{
			}
		}
	}
}
