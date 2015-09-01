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

namespace Xamarin.WebTests.TestRunners
{
	using ConnectionFramework;
	using TestFramework;
	using Resources;
	using Providers;
	using Portable;

	[SslStreamTestRunner]
	public class SslStreamTestRunner : ConnectionTestRunner
	{
		new public SslStreamTestParameters Parameters {
			get { return (SslStreamTestParameters)base.Parameters; }
		}

		public SslStreamTestRunner (IServer server, IClient client, ConnectionTestProvider provider, SslStreamTestParameters parameters)
			: base (server, client, provider, parameters)
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
					ExpectClientException = true
				};

			case ConnectionTestType.RejectAll:
				// Explicit validator overrides the default ServicePointManager.ServerCertificateValidationCallback.
				return new SslStreamTestParameters (category, type, name, ResourceManager.SelfSignedServerCertificate) {
					ExpectClientException = true, ClientCertificateValidator = rejectAll
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
					AskForClientCertificate = true, ServerCertificateValidator = acceptFromLocalCA
				};

			case ConnectionTestType.RequireClientCertificate:
				// Require client certificate.
				return new SslStreamTestParameters (category, type, name, ResourceManager.SelfSignedServerCertificate) {
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
				return new SslStreamTestParameters (category, type, name, ResourceManager.SelfSignedServerCertificate) {
					ClientCertificateValidator = acceptSelfSigned, AskForClientCertificate = true,
					ServerCertificateValidator = acceptNull
				};

			case ConnectionTestType.RejectClientCertificate:
				// Reject client certificate.
				return new SslStreamTestParameters (category, type, name, ResourceManager.SelfSignedServerCertificate) {
					ClientCertificate = ResourceManager.MonkeyCertificate, ClientCertificateValidator = acceptSelfSigned,
					ServerCertificateValidator = rejectAll, AskForClientCertificate = true,
					ExpectClientException = true, ExpectServerException = true
				};

			case ConnectionTestType.MissingClientCertificate:
				// Missing client certificate.
				return new SslStreamTestParameters (category, type, name, ResourceManager.SelfSignedServerCertificate) {
					ClientCertificateValidator = acceptSelfSigned,
					AskForClientCertificate = true, RequireClientCertificate = true,
					ExpectClientException = true, ExpectServerException = true
				};

			case ConnectionTestType.MartinTest:
				return new SslStreamTestParameters (category, type, name, ResourceManager.SelfSignedServerCertificate) {
					ClientCertificateValidator = acceptSelfSigned
				};

			default:
				throw new InvalidOperationException ();
			}
		}

		protected override async Task OnRun (TestContext ctx, CancellationToken cancellationToken)
		{
			await base.OnRun (ctx, cancellationToken);

			if (Parameters.ExpectServerException)
				ctx.AssertFail ("expecting server exception");
			if (Parameters.ExpectClientException)
				ctx.AssertFail ("expecting client exception");

			if (!IsManualServer) {
				ctx.Expect (Server.SslStream.IsAuthenticated, "server is authenticated");

				if (Server.Parameters.RequireClientCertificate) {
					ctx.LogDebug (1, "Client certificate required: {0} {1}", Server.SslStream.IsMutuallyAuthenticated, Server.SslStream.HasRemoteCertificate);
					ctx.Expect (Server.SslStream.IsMutuallyAuthenticated, "server is mutually authenticated");
					ctx.Expect (Server.SslStream.HasRemoteCertificate, "server has client certificate");
				}
			}

			if (!IsManualClient) {
				ctx.Expect (Client.SslStream.IsAuthenticated, "client is authenticated");

				ctx.Expect (Client.SslStream.HasRemoteCertificate, "client has server certificate");
			}

			if (!IsManualConnection && Server.Parameters.AskForClientCertificate && Client.Parameters.ClientCertificate != null)
				ctx.Expect (Client.SslStream.HasLocalCertificate, "client has local certificate");

		}

		protected override void OnWaitForServerConnectionCompleted (TestContext ctx, Task task)
		{
			if (Parameters.ExpectServerException) {
				ctx.Assert (task.IsFaulted, "expecting exception");
				throw new ConnectionFinishedException ();
			}

			if (task.IsFaulted) {
				if (Parameters.ExpectClientException)
					throw new ConnectionFinishedException ();
				throw task.Exception;
			}

			base.OnWaitForServerConnectionCompleted (ctx, task);
		}

		protected override void OnWaitForClientConnectionCompleted (TestContext ctx, Task task)
		{
			if (task.IsFaulted) {
				if (Parameters.ExpectClientException)
					throw new ConnectionFinishedException ();
				throw task.Exception;
			}

			base.OnWaitForClientConnectionCompleted (ctx, task);
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

