//
// ConnectionTestRunner.cs
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
using System.Collections.Generic;
using Xamarin.AsyncTests;

namespace Xamarin.WebTests.TestRunners
{
	using ConnectionFramework;
	using Providers;
	using Resources;
	using Portable;

	public abstract class ConnectionTestRunner : ClientAndServerTestRunner
	{
		public ConnectionTestRunner (IServer server, IClient client, ConnectionTestParameters parameters)
			: base (server, client, parameters)
		{
		}

		public static IEnumerable<ConnectionTestType> GetConnectionTestTypes (TestContext ctx, ConnectionTestCategory category)
		{
			switch (category) {
			case ConnectionTestCategory.Https:
				return AllTestTypes;
			case ConnectionTestCategory.HttpsWithMono:
				return OldMonoTestTypes;
			case ConnectionTestCategory.HttpsWithDotNet:
				return DotNetTestTypes;
			default:
				ctx.AssertFail ("Unsupported test category: '{0}'.", category);
				throw new NotImplementedException ();
			}
		}

		static IEnumerable<ConnectionTestType> OldMonoTestTypes {
			get {
				yield return ConnectionTestType.Default;
				yield return ConnectionTestType.AcceptFromLocalCA;
				yield return ConnectionTestType.RejectAll;
				yield return ConnectionTestType.UnrequestedClientCertificate;
				yield return ConnectionTestType.RejectClientCertificate;
				yield return ConnectionTestType.MissingClientCertificate;
			}
		}

		static IEnumerable<ConnectionTestType> AllTestTypes {
			get {
				yield return ConnectionTestType.Default;
				yield return ConnectionTestType.AcceptFromLocalCA;
				yield return ConnectionTestType.NoValidator;
				yield return ConnectionTestType.RejectAll;
				yield return ConnectionTestType.RequestClientCertificate;
				yield return ConnectionTestType.RequireClientCertificate;
				yield return ConnectionTestType.RejectClientCertificate;
				yield return ConnectionTestType.UnrequestedClientCertificate;
				yield return ConnectionTestType.OptionalClientCertificate;
				yield return ConnectionTestType.RejectClientCertificate;
				yield return ConnectionTestType.MissingClientCertificate;
			}
		}

		static IEnumerable<ConnectionTestType> DotNetTestTypes {
			get {
				yield return ConnectionTestType.NoValidator;
				yield return ConnectionTestType.RequestClientCertificate;
				yield return ConnectionTestType.RequireClientCertificate;
				yield return ConnectionTestType.RejectClientCertificate;
				yield return ConnectionTestType.UnrequestedClientCertificate;
				yield return ConnectionTestType.OptionalClientCertificate;
				yield return ConnectionTestType.RejectClientCertificate;
				yield return ConnectionTestType.MissingClientCertificate;
			}
		}

		public static ClientAndServerParameters GetParameters (TestContext ctx, ConnectionTestType type)
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
			case ConnectionTestType.Default:
				return new ClientAndServerParameters (acceptAllClient, selfSignedServer);
			case ConnectionTestType.AcceptFromLocalCA:
				return new ClientAndServerParameters ("accept-local-ca", ResourceManager.ServerCertificateFromCA) {
					ClientCertificateValidator = acceptFromLocalCA
				};

			case ConnectionTestType.NoValidator:
				// The default validator only allows ResourceManager.SelfSignedServerCertificate.
				return new ClientAndServerParameters ("no-validator", ResourceManager.ServerCertificateFromCA) {
					ClientFlags = ClientFlags.ExpectTrustFailure, ServerFlags = ServerFlags.ClientAbortsHandshake
				};

			case ConnectionTestType.RejectAll:
				// Explicit validator overrides the default ServicePointManager.ServerCertificateValidationCallback.
				return new ClientAndServerParameters ("reject-all", ResourceManager.SelfSignedServerCertificate) {
					ClientFlags = ClientFlags.ExpectTrustFailure, ClientCertificateValidator = rejectAll,
					ServerFlags = ServerFlags.ClientAbortsHandshake
				};

			case ConnectionTestType.UnrequestedClientCertificate:
				// Provide a client certificate, but do not require it.
				return new ClientAndServerParameters ("unrequested-client-certificate", ResourceManager.SelfSignedServerCertificate) {
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
				return new ClientAndServerParameters ("request-client-certificate", ResourceManager.SelfSignedServerCertificate) {
					ClientCertificate = ResourceManager.MonkeyCertificate, ClientCertificateValidator = acceptSelfSigned,
					ServerFlags = ServerFlags.AskForClientCertificate, ServerCertificateValidator = acceptFromLocalCA
				};

			case ConnectionTestType.RequireClientCertificate:
				// Require client certificate.
				return new ClientAndServerParameters ("require-client-certificate", ResourceManager.SelfSignedServerCertificate) {
					ClientCertificate = ResourceManager.MonkeyCertificate, ClientCertificateValidator = acceptSelfSigned,
					ServerFlags = ServerFlags.AskForClientCertificate | ServerFlags.RequireClientCertificate,
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
				return new ClientAndServerParameters ("optional-client-certificate", ResourceManager.SelfSignedServerCertificate) {
					ClientCertificateValidator = acceptSelfSigned, ServerFlags = ServerFlags.AskForClientCertificate,
					ServerCertificateValidator = acceptNull
				};

			case ConnectionTestType.RejectClientCertificate:
				// Reject client certificate.
				return new ClientAndServerParameters ("reject-client-certificate", ResourceManager.SelfSignedServerCertificate) {
					ClientCertificate = ResourceManager.MonkeyCertificate, ClientCertificateValidator = acceptSelfSigned,
					ClientFlags = ClientFlags.ExpectWebException, ServerCertificateValidator = rejectAll,
					ServerFlags = ServerFlags.AskForClientCertificate | ServerFlags.ClientAbortsHandshake | ServerFlags.ExpectServerException
				};

			case ConnectionTestType.MissingClientCertificate:
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
	}
}

