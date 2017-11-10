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
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using Xamarin.AsyncTests;
using Xamarin.AsyncTests.Framework;

namespace Xamarin.WebTests.TestRunners
{
	using ConnectionFramework;
	using TestFramework;
	using Resources;

	public abstract class ConnectionTestRunner : ClientAndServer
	{
		new public ConnectionTestParameters Parameters {
			get { return (ConnectionTestParameters)base.Parameters; }
		}

		public ConnectionTestCategory Category {
			get { return Parameters.Category; }
		}

		public ConnectionTestProvider Provider {
			get;
			private set;
		}

		public ConnectionHandler ConnectionHandler {
			get;
		}

		public ConnectionTestRunner (Connection server, Connection client, ConnectionTestProvider provider, ConnectionTestParameters parameters)
			: base (server, client, parameters)
		{
			Provider = provider;

			ConnectionHandler = CreateConnectionHandler ();
		}

		public static IEnumerable<ConnectionTestType> GetConnectionTestTypes (TestContext ctx, ConnectionTestCategory category)
		{
			switch (category) {
			case ConnectionTestCategory.Https:
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
				yield break;

			case ConnectionTestCategory.HttpsWithMono:
				yield return ConnectionTestType.Default;
				yield return ConnectionTestType.AcceptFromLocalCA;
				yield return ConnectionTestType.RejectAll;
				yield break;

			case ConnectionTestCategory.HttpsWithDotNet:
				yield return ConnectionTestType.NoValidator;
				yield return ConnectionTestType.RequestClientCertificate;
				yield return ConnectionTestType.RequireClientCertificate;
				yield return ConnectionTestType.RejectClientCertificate;
				yield return ConnectionTestType.UnrequestedClientCertificate;
				yield return ConnectionTestType.OptionalClientCertificate;
				yield return ConnectionTestType.RejectClientCertificate;
				yield return ConnectionTestType.MissingClientCertificate;
				yield break;

			case ConnectionTestCategory.SslStreamWithTls12:
				yield return ConnectionTestType.Default;
				yield return ConnectionTestType.AcceptFromLocalCA;
				yield return ConnectionTestType.RequireClientCertificate;
				yield return ConnectionTestType.SyncAuthenticate;
				yield break;

			case ConnectionTestCategory.InvalidCertificatesInTls12:
				yield return ConnectionTestType.InvalidServerCertificate;
				yield break;

			case ConnectionTestCategory.HttpsCertificateValidators:
				yield return ConnectionTestType.DontInvokeGlobalValidator;
				yield return ConnectionTestType.DontInvokeGlobalValidator2;
				yield return ConnectionTestType.GlobalValidatorIsNull;
				yield return ConnectionTestType.MustInvokeGlobalValidator;
				yield break;

			case ConnectionTestCategory.NotYetWorking:
				yield return ConnectionTestType.ExternalServer;
				yield return ConnectionTestType.CheckChain;
				yield break;

			case ConnectionTestCategory.SslStreamCertificateValidators:
				yield return ConnectionTestType.MustNotInvokeGlobalValidator;
				yield return ConnectionTestType.MustNotInvokeGlobalValidator2;
				yield break;

			case ConnectionTestCategory.TrustedRoots:
				yield return ConnectionTestType.ServerCertificateWithCA;
				yield return ConnectionTestType.TrustedRootCA;
				yield return ConnectionTestType.TrustedIntermediateCA;
				yield return ConnectionTestType.TrustedSelfSigned;
				yield return ConnectionTestType.HostNameMismatch;
				yield return ConnectionTestType.IntermediateServerCertificate;
				yield return ConnectionTestType.IntermediateServerCertificateFull;
				yield return ConnectionTestType.IntermediateServerCertificateBare;
				yield break;

			case ConnectionTestCategory.CertificateStore:
				yield return ConnectionTestType.CertificateStore;
				yield break;

			case ConnectionTestCategory.MartinTest:
				yield return ConnectionTestType.MartinTest;
				yield break;

			default:
				ctx.AssertFail ("Unsupported test category: '{0}'.", category);
				throw new InternalErrorException ();
			}
		}

		public static ConnectionTestFlags GetConnectionFlags (TestContext ctx, ConnectionTestCategory category)
		{
			switch (category) {
			case ConnectionTestCategory.Https:
				return ConnectionTestFlags.RequireSslStream;
			case ConnectionTestCategory.HttpsWithMono:
				return ConnectionTestFlags.RequireSslStream;
			case ConnectionTestCategory.HttpsWithDotNet:
				return ConnectionTestFlags.RequireSslStream | ConnectionTestFlags.RequireTls12 | ConnectionTestFlags.RequireClientCertificates;
			case ConnectionTestCategory.SslStreamWithTls12:
				return ConnectionTestFlags.RequireSslStream | ConnectionTestFlags.RequireTls12;
			case ConnectionTestCategory.InvalidCertificatesInTls12:
				return ConnectionTestFlags.RequireSslStream | ConnectionTestFlags.RequireTls12 | ConnectionTestFlags.RequireClientCertificates;
			case ConnectionTestCategory.HttpsCertificateValidators:
				return ConnectionTestFlags.RequireHttp;
			case ConnectionTestCategory.SslStreamCertificateValidators:
				return ConnectionTestFlags.RequireSslStream;
			case ConnectionTestCategory.TrustedRoots:
			case ConnectionTestCategory.CertificateStore:
				return ConnectionTestFlags.RequireTrustedRoots;
			case ConnectionTestCategory.SimpleMonoClient:
				return ConnectionTestFlags.RequireMono;
			case ConnectionTestCategory.SimpleMonoServer:
				return ConnectionTestFlags.RequireMono;
			case ConnectionTestCategory.SimpleMonoConnection:
			case ConnectionTestCategory.MonoProtocolVersions:
				return ConnectionTestFlags.RequireMono;
			case ConnectionTestCategory.CertificateChecks:
			case ConnectionTestCategory.SecurityFramework:
				return ConnectionTestFlags.RequireMono;
			case ConnectionTestCategory.SslStreamInstrumentation:
			case ConnectionTestCategory.SslStreamInstrumentationExperimental:
				return ConnectionTestFlags.RequireSslStream | ConnectionTestFlags.RequireTls12 | ConnectionTestFlags.RequireMono;
			case ConnectionTestCategory.SslStreamInstrumentationMono:
				return ConnectionTestFlags.RequireSslStream | ConnectionTestFlags.RequireTls12 | ConnectionTestFlags.RequireMono;
			case ConnectionTestCategory.SslStreamInstrumentationShutdown:
				return ConnectionTestFlags.RequireSslStream | ConnectionTestFlags.RequireMono | ConnectionTestFlags.RequireCleanShutdown;
			case ConnectionTestCategory.SslStreamInstrumentationServerShutdown:
				return ConnectionTestFlags.RequireSslStream | ConnectionTestFlags.RequireMono | ConnectionTestFlags.RequireCleanServerShutdown;
			case ConnectionTestCategory.SslStreamInstrumentationRecentlyFixed:
				return ConnectionTestFlags.RequireSslStream | ConnectionTestFlags.RequireTls12;
			case ConnectionTestCategory.SslStreamInstrumentationNewWebStack:
				return ConnectionTestFlags.RequireSslStream | ConnectionTestFlags.RequireTls12;
			case ConnectionTestCategory.HttpInstrumentation:
			case ConnectionTestCategory.HttpInstrumentationNoSSL:
			case ConnectionTestCategory.HttpInstrumentationStress:
			case ConnectionTestCategory.HttpInstrumentationNewWebStack:
			case ConnectionTestCategory.HttpInstrumentationExperimental:
			case ConnectionTestCategory.HttpStress:
			case ConnectionTestCategory.HttpStressExperimental:
				return ConnectionTestFlags.RequireHttp | ConnectionTestFlags.RequireSslStream | ConnectionTestFlags.RequireTls12;
			case ConnectionTestCategory.MartinTest:
				return ConnectionTestFlags.AssumeSupportedByTest;
			default:
				ctx.AssertFail ("Unsupported instrumentation category: '{0}'.", category);
				return ConnectionTestFlags.None;
			}
		}

		protected sealed override Task StartClient (TestContext ctx, CancellationToken cancellationToken)
		{
			return Client.Start (ctx, null, cancellationToken);
		}

		protected sealed override Task StartServer (TestContext ctx, CancellationToken cancellationToken)
		{
			return Server.Start (ctx, null, cancellationToken);
		}

		protected override Task ClientShutdown (TestContext ctx, CancellationToken cancellationToken)
		{
			return Client.Shutdown (ctx, cancellationToken);
		}

		protected override Task ServerShutdown (TestContext ctx, CancellationToken cancellationToken)
		{
			return Server.Shutdown (ctx, cancellationToken);
		}

		protected override void InitializeConnection (TestContext ctx)
		{
			ConnectionHandler.InitializeConnection (ctx);
			base.InitializeConnection (ctx);
		}

		protected abstract ConnectionHandler CreateConnectionHandler ();

		protected sealed override Task MainLoop (TestContext ctx, CancellationToken cancellationToken)
		{
			return ConnectionHandler.MainLoop (ctx, cancellationToken);
		}
	}
}

