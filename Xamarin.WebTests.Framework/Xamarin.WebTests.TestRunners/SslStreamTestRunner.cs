//
// SslStreamTestRunner.cs
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
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xamarin.AsyncTests;
using Xamarin.AsyncTests.Constraints;
using Xamarin.AsyncTests.Portable;

namespace Xamarin.WebTests.TestRunners
{
	using ConnectionFramework;
	using Resources;
	using Providers;
	using Portable;

	public class SslStreamTestRunner : ConnectionTestRunner
	{
		public SslStreamTestRunner (IServer server, IClient client, SslStreamTestParameters parameters, ConnectionFlags flags)
			: base (server, client, parameters, flags)
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

		public static SslStreamTestParameters GetParameters (TestContext ctx, ConnectionTestCategory category, ConnectionTestType type)
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
				return new SslStreamTestParameters (category, type, name, ResourceManager.SelfSignedServerCertificate) {
					ClientCertificateValidator = acceptAll
				};

			case ConnectionTestType.AcceptFromLocalCA:
				return new SslStreamTestParameters (category, type, name, ResourceManager.ServerCertificateFromCA) {
					ClientCertificateValidator = acceptFromLocalCA
				};

			case ConnectionTestType.NoValidator:
				// The default validator only allows ResourceManager.SelfSignedServerCertificate.
				return new SslStreamTestParameters (category, type, name, ResourceManager.ServerCertificateFromCA) {
					ClientFlags = ClientFlags.ExpectTrustFailure, ServerFlags = ServerFlags.ClientAbortsHandshake
				};

			case ConnectionTestType.RejectAll:
				// Explicit validator overrides the default ServicePointManager.ServerCertificateValidationCallback.
				return new SslStreamTestParameters (category, type, name, ResourceManager.SelfSignedServerCertificate) {
					ClientFlags = ClientFlags.ExpectTrustFailure, ClientCertificateValidator = rejectAll,
					ServerFlags = ServerFlags.ClientAbortsHandshake
				};

			case ConnectionTestType.UnrequestedClientCertificate:
				// Provide a client certificate, but do not require it.
				return new SslStreamTestParameters (category, type, name, ResourceManager.SelfSignedServerCertificate) {
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
				return new SslStreamTestParameters (category, type, name, ResourceManager.SelfSignedServerCertificate) {
					ClientCertificate = ResourceManager.MonkeyCertificate, ClientCertificateValidator = acceptSelfSigned,
					ServerFlags = ServerFlags.AskForClientCertificate, ServerCertificateValidator = acceptFromLocalCA
				};

			case ConnectionTestType.RequireClientCertificate:
				// Require client certificate.
				return new SslStreamTestParameters (category, type, name, ResourceManager.SelfSignedServerCertificate) {
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
				return new SslStreamTestParameters (category, type, name, ResourceManager.SelfSignedServerCertificate) {
					ClientCertificateValidator = acceptSelfSigned, ServerFlags = ServerFlags.AskForClientCertificate,
					ServerCertificateValidator = acceptNull
				};

			case ConnectionTestType.RejectClientCertificate:
				// Reject client certificate.
				return new SslStreamTestParameters (category, type, name, ResourceManager.SelfSignedServerCertificate) {
					ClientCertificate = ResourceManager.MonkeyCertificate, ClientCertificateValidator = acceptSelfSigned,
					ClientFlags = ClientFlags.ExpectWebException, ServerCertificateValidator = rejectAll,
					ServerFlags = ServerFlags.AskForClientCertificate | ServerFlags.ClientAbortsHandshake | ServerFlags.ExpectServerException
				};

			case ConnectionTestType.MissingClientCertificate:
				// Missing client certificate.
				return new SslStreamTestParameters (category, type, name, ResourceManager.SelfSignedServerCertificate) {
					ClientCertificateValidator = acceptSelfSigned, ClientFlags = ClientFlags.ExpectWebException,
					ServerFlags = ServerFlags.AskForClientCertificate | ServerFlags.RequireClientCertificate |
						ServerFlags.ClientAbortsHandshake | ServerFlags.ExpectServerException
				};

			case ConnectionTestType.MartinTest:
				var provider = DependencyInjector.Get<IPortableEndPointSupport> ();
				var endpoint = provider.GetEndpoint ("0.0.0.0", 4433);
				return new SslStreamTestParameters (category, type, name, ResourceManager.SelfSignedServerCertificate) {
					ClientCertificateValidator = acceptSelfSigned, ProtocolVersion = ProtocolVersions.Tls12,
					ServerFlags = ServerFlags.RequireClientCertificate, ClientCertificate = ResourceManager.InvalidClientCertificate,
					ServerCertificateValidator = acceptAll, EndPoint = endpoint
				};

			default:
				throw new InvalidOperationException ();
			}
		}

		protected override async Task OnRun (TestContext ctx, CancellationToken cancellationToken)
		{
			await base.OnRun (ctx, cancellationToken);

			if (!IsManualServer) {
				ctx.Expect (Server.SslStream.IsAuthenticated, "server is authenticated");

				if (Server.Parameters.RequireCertificate) {
					ctx.Expect (Server.SslStream.IsMutuallyAuthenticated, "server is mutually authenticated");
					ctx.Expect (Server.SslStream.HasRemoteCertificate, "server has client certificate");
				}
			}

			if (!IsManualClient) {
				ctx.Expect (Client.SslStream.IsAuthenticated, "client is authenticated");

				ctx.Expect (Client.SslStream.HasRemoteCertificate, "client has server certificate");
			}

			if (!IsManualConnection && Server.Parameters.AskForCertificate && Client.Parameters.ClientCertificate != null)
				ctx.Expect (Client.SslStream.HasLocalCertificate, "client has local certificate");

		}

		protected override async Task MainLoop (TestContext ctx, CancellationToken cancellationToken)
		{
			if (IsManualConnection)
				return;

			var serverStream = new StreamWrapper (Server.Stream);
			var clientStream = new StreamWrapper (Client.Stream);

			await serverStream.WriteLineAsync ("SERVER OK");
			var line = await clientStream.ReadLineAsync ();
			if (!line.Equals ("SERVER OK"))
				throw new ConnectionException ("Got unexpected output from server: '{0}'", line);
			await clientStream.WriteLineAsync ("CLIENT OK");
			line = await serverStream.ReadLineAsync ();
			if (!line.Equals ("CLIENT OK"))
				throw new ConnectionException ("Got unexpected output from client: '{0}'", line);
		}
	}
}

