//
// ValidationTestFixture.cs
//
// Author:
//       Martin Baulig <mabaul@microsoft.com>
//
// Copyright (c) 2018 Xamarin Inc. (http://www.xamarin.com)
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
using System.Net.Sockets;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Xamarin.AsyncTests;
using Xamarin.AsyncTests.Constraints;

namespace Xamarin.WebTests.ValidationTests
{
	using ConnectionFramework;
	using TestAttributes;
	using TestFramework;
	using HttpFramework;
	using HttpHandlers;
	using TestRunners;
	using Resources;

	[New]
	[AsyncTestFixture (Prefix = "ValidationTests")]
	public abstract class ValidationTestFixture : InstrumentationTestRunner
	{
		protected virtual CertificateResourceType? CertificateType => null;

		bool ExternalServer => false;

		protected ConnectionParameters Parameters => Server.Parameters;

		protected override void CreateParameters (TestContext ctx, ConnectionParameters parameters)
		{
			base.CreateParameters (ctx, parameters);
		}

		protected ValidationTestFixture ()
		{
		}

		[AsyncTest]
		public static Task Run (
			TestContext ctx, CancellationToken cancellationToken,
			ConnectionTestProvider provider, ValidationTestFixture fixture)
		{
			return fixture.Run (ctx, cancellationToken);
		}

		[AsyncTest]
		[Martin (null, UseFixtureName = true)]
		public static Task MartinTest (
			TestContext ctx, CancellationToken cancellationToken,
			ConnectionTestProvider provider, ValidationTestFixture fixture)
		{
			return fixture.Run (ctx, cancellationToken);
		}

		public override bool HasRequestBody => false;

		protected override void InitializeHandler (TestContext ctx)
		{
		}

		protected override Request CreateRequest (TestContext ctx, InstrumentationOperation operation, Uri uri)
		{
			ctx.LogDebug (LogCategories.Validation, 1, "Create request: {0}", uri);

			InstrumentationRequest request;

			// ctx.GetParameter<ConnectionTestFlags> ();
			if (Parameters.ValidationParameters != null) {
				// Add [ConnectionTestFlags (ConnectionTestFlags.RequireTrustedRoots)] to the test fixture.
				var provider = ctx.GetParameter<ConnectionTestProvider> ();
				if (!provider.Client.HasFlag (ConnectionProviderFlags.SupportsTrustedRoots))
					throw ctx.AssertFail ("Using `ValidationParameters` requires `ConnectionProviderFlags.SupportsTrustedRoots`");
				var webRequest = provider.Client.SslStreamProvider.CreateWebRequest (uri, Parameters);
				request = new InstrumentationRequest (this, webRequest);
			} else {
				request = new InstrumentationRequest (this, uri);
			}

			request.RequestExt.SetKeepAlive (true);

			if (Parameters.TargetHost != null)
				request.RequestExt.Host = Parameters.TargetHost;

			if (ClientCertificateValidator != null)
				request.RequestExt.InstallCertificateValidator (ClientCertificateValidator.ValidationCallback);

			if (Parameters.ClientCertificate != null) {
				var certificates = new X509CertificateCollection {
					Parameters.ClientCertificate
				};
				request.RequestExt.SetClientCertificates (certificates);
			}

			ConfigureRequest (ctx, request); 

			return request;
		}

		protected virtual void ConfigureRequest (TestContext ctx, InstrumentationRequest request)
		{
		}

		bool HasFlag (GlobalValidationFlags flag) => (Parameters.GlobalValidationFlags & flag) == flag;

		RemoteCertificateValidationCallback savedGlobalCallback;
		TestContext savedContext;
		bool restoreGlobalCallback;
		int globalValidatorInvoked;

		void SetGlobalValidationCallback (TestContext ctx, RemoteCertificateValidationCallback callback)
		{
			ctx.LogDebug (LogCategories.Validation, 1, "Set validator: {0}", callback != null);
			savedGlobalCallback = ServicePointManager.ServerCertificateValidationCallback;
			ServicePointManager.ServerCertificateValidationCallback = callback;
			savedContext = ctx;
			restoreGlobalCallback = true;
		}

		bool GlobalValidationCallback (object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors errors)
		{
			return GlobalValidationCallback (savedContext, certificate, chain, errors);
		}

		bool GlobalValidationCallback (TestContext ctx, X509Certificate certificate, X509Chain chain, SslPolicyErrors errors)
		{
			ctx.LogDebug (LogCategories.Validation, 1, "Global validator: {0}", globalValidatorInvoked);
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
			if (HasFlag (GlobalValidationFlags.AlwaysSucceed))
				return true;

			return result;
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
				SetGlobalValidationCallback (ctx, GlobalValidationCallback);

			await base.PreRun (ctx, cancellationToken).ConfigureAwait (false);
		}

		protected override async Task PostRun (TestContext ctx, CancellationToken cancellationToken)
		{
			await base.PostRun (ctx, cancellationToken).ConfigureAwait (false);

			if (restoreGlobalCallback)
				ServicePointManager.ServerCertificateValidationCallback = savedGlobalCallback;

			if (HasFlag (GlobalValidationFlags.MustInvoke) || HasFlag (GlobalValidationFlags.CheckChain))
				ctx.Assert (globalValidatorInvoked, Is.EqualTo (1), "global validator has been invoked");
		}

		protected override async Task<Response> Run (TestContext ctx, Request request, CancellationToken cancellationToken)
		{
			var traditionalRequest = (TraditionalRequest)request;
			var response = await traditionalRequest.SendAsync (ctx, cancellationToken);
			if (!response.IsSuccess)
				return response;

			var provider = DependencyInjector.Get<ICertificateProvider> ();

			var certificate = traditionalRequest.RequestExt.GetCertificate ();
			ctx.Assert (certificate, Is.Not.Null, "certificate");

			var expectedCert = ServerCertificate;
			if (ExternalServer) {
				if (expectedCert == null && CertificateType != null)
					expectedCert = ResourceManager.GetCertificate (CertificateType.Value);
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

		public override HttpResponse HandleRequest (TestContext ctx, InstrumentationOperation operation, HttpConnection connection, HttpRequest request)
		{
			return new HttpResponse (HttpStatusCode.OK, ExpectedContent);
		}
	}
}
