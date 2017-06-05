﻿//
// MonoConnectionTestRunner.cs
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
using System.Net;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using Mono.Security.Interface;
using Xamarin.AsyncTests;
using Xamarin.AsyncTests.Constraints;
using Xamarin.WebTests.ConnectionFramework;
using Xamarin.WebTests.MonoConnectionFramework;
using Xamarin.WebTests.MonoTestFramework;
using Xamarin.WebTests.HttpFramework;
using Xamarin.WebTests.Resources;
using Xamarin.WebTests.TestRunners;
using Xamarin.WebTests.TestFramework;

namespace Xamarin.WebTests.MonoTestFramework
{
	public abstract class MonoConnectionTestRunner : ClientAndServer
	{
		new public MonoConnectionTestParameters Parameters {
			get { return (MonoConnectionTestParameters)base.Parameters; }
		}

		public MonoConnectionTestCategory Category {
			get { return Parameters.Category; }
		}

		public ConnectionTestFlags ConnectionFlags {
			get { return Provider.Flags; }
		}

		public MonoConnectionTestProvider Provider {
			get;
			private set;
		}

		public ConnectionHandler ConnectionHandler {
			get;
			private set;
		}

		protected MonoConnectionTestRunner (Connection server, Connection client, MonoConnectionTestProvider provider, MonoConnectionTestParameters parameters)
			: base (server, client, parameters)
		{
			Provider = provider;
		}

		public static ConnectionTestFlags GetConnectionFlags (TestContext ctx, MonoConnectionTestCategory category)
		{
			switch (category) {
			case MonoConnectionTestCategory.SimpleMonoClient:
			case MonoConnectionTestCategory.SelectClientCipher:
				return ConnectionTestFlags.RequireMonoClient;
			case MonoConnectionTestCategory.SimpleMonoServer:
			case MonoConnectionTestCategory.SelectServerCipher:
				return ConnectionTestFlags.RequireMonoServer;
			case MonoConnectionTestCategory.SimpleMonoConnection:
			case MonoConnectionTestCategory.MonoProtocolVersions:
			case MonoConnectionTestCategory.SelectCipher:
				return ConnectionTestFlags.RequireMono;
			case MonoConnectionTestCategory.ClientConnection:
			case MonoConnectionTestCategory.MartinTestClient:
				return ConnectionTestFlags.RequireMonoClient;
			case MonoConnectionTestCategory.ServerConnection:
			case MonoConnectionTestCategory.MartinTestServer:
				return ConnectionTestFlags.RequireMonoServer;
			case MonoConnectionTestCategory.Connection:
			case MonoConnectionTestCategory.CertificateChecks:
			case MonoConnectionTestCategory.SecurityFramework:
				return ConnectionTestFlags.RequireMono;
			case MonoConnectionTestCategory.MartinTest:
				// return ConnectionTestFlags.RequireMono | ConnectionTestFlags.RequireTls12;
				return ConnectionTestFlags.None;
			default:
				ctx.AssertFail ("Unsupported instrumentation category: '{0}'.", category);
				return ConnectionTestFlags.None;
			}
		}

		protected override Task StartClient (TestContext ctx, CancellationToken cancellationToken)
		{
			return Client.Start (ctx, null, cancellationToken);
		}

		protected override Task StartServer (TestContext ctx, CancellationToken cancellationToken)
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
			var provider = ctx.GetParameter<ClientAndServerProvider> ("ClientAndServerProvider");

			var clientOverridesCipher = (provider.Client.Flags & ConnectionProviderFlags.OverridesCipherSelection) != 0;
			var serverOverridesCipher = (provider.Server.Flags & ConnectionProviderFlags.OverridesCipherSelection) != 0;

			if (Parameters.ValidateCipherList) {
				if (!CipherList.ValidateCipherList (provider, Parameters.ClientCiphers))
					ctx.IgnoreThisTest ();
				if (!CipherList.ValidateCipherList (provider, Parameters.ServerCiphers))
					ctx.IgnoreThisTest ();
			}

			if (serverOverridesCipher) {
				if ((Parameters.ExpectedServerCipher != null || Parameters.ExpectedCipher != null) &&
				    (Parameters.ClientCiphers == null || Parameters.ClientCiphers.Count > 1))
					ctx.IgnoreThisTest ();
			}

			if (clientOverridesCipher) {
				if ((Parameters.ExpectedClientCipher != null || Parameters.ExpectedCipher != null) &&
				    (Parameters.ServerCiphers == null || Parameters.ServerCiphers.Count > 2))
					ctx.IgnoreThisTest ();
				if (Parameters.ClientCiphers == null && Parameters.ServerCiphers != null)
					ctx.IgnoreThisTest ();
			}

			ConnectionHandler = CreateConnectionHandler ();
			ConnectionHandler.InitializeConnection (ctx);
			base.InitializeConnection (ctx);
		}

		protected abstract ConnectionHandler CreateConnectionHandler ();

		public static IEnumerable<R> Join<T,U,R> (IEnumerable<T> first, IEnumerable<U> second, Func<T, U, R> resultSelector) {
			foreach (var e1 in first) {
				foreach (var e2 in second) {
					yield return resultSelector (e1, e2);
				}
			}
		}

		protected static CertificateValidator AcceptAnyCertificate {
			get { return DependencyInjector.Get<ICertificateProvider> ().AcceptAll (); }
		}

		protected override void OnWaitForClientConnectionCompleted (TestContext ctx, Task task)
		{
			if (Parameters.ExpectClientAlert != null) {
				MonoConnectionHelper.ExpectAlert (ctx, task, Parameters.ExpectClientAlert.Value, "expect client alert");
				throw new ConnectionFinishedException ();
			}

			base.OnWaitForClientConnectionCompleted (ctx, task);
		}

		protected override void OnWaitForServerConnectionCompleted (TestContext ctx, Task task)
		{
			if (Parameters.ExpectClientAlert != null) {
				ctx.Assert (task.IsFaulted, "expecting exception");
				throw new ConnectionFinishedException ();
			}

			if (Parameters.ExpectServerAlert != null) {
				MonoConnectionHelper.ExpectAlert (ctx, task, Parameters.ExpectServerAlert.Value, "expect server alert");
				throw new ConnectionFinishedException ();
			}

			base.OnWaitForServerConnectionCompleted (ctx, task);
		}

		protected bool CheckCipher (TestContext ctx, IMonoConnection connection, CipherSuiteCode cipher)
		{
			ctx.Assert (connection.SupportsConnectionInfo, "supports connection info");
			var connectionInfo = connection.GetConnectionInfo ();

			if (!ctx.Expect (connectionInfo, Is.Not.Null, "connection info"))
				return false;
			return ctx.Expect (connectionInfo.CipherSuiteCode, Is.EqualTo (cipher), "expected cipher");
		}

		protected override Task OnRun (TestContext ctx, CancellationToken cancellationToken)
		{
			var monoClient = Client as IMonoConnection;
			var monoServer = Server as IMonoConnection;

			bool ok = true;
			if (monoClient != null) {
				var expectedCipher = Parameters.ExpectedClientCipher ?? Parameters.ExpectedCipher;
				if (expectedCipher != null)
					ok &= CheckCipher (ctx, monoClient, expectedCipher.Value);
			}

			if (ok && monoServer != null) {
				var expectedCipher = Parameters.ExpectedServerCipher ?? Parameters.ExpectedCipher;
				if (expectedCipher != null)
					ok &= CheckCipher (ctx, monoServer, expectedCipher.Value);
			}

			if (!IsManualConnection && Parameters.ProtocolVersion != null) {
				if (ctx.Expect (Client.ProtocolVersion, Is.EqualTo (Parameters.ProtocolVersion), "client protocol version"))
					ctx.Expect (Server.ProtocolVersion, Is.EqualTo (Parameters.ProtocolVersion), "server protocol version");
			}

			if (Server.Provider.SupportsSslStreams && Parameters.RequireClientCertificate) {
				ctx.Expect (Server.SslStream.RemoteCertificate, Is.Not.Null, "has remote certificate");
				ctx.Expect (Server.SslStream.IsMutuallyAuthenticated, "is mutually authenticated");
			}

			return base.OnRun (ctx, cancellationToken);
		}

		protected void LogDebug (TestContext ctx, int level, string message, params object[] args)
		{
			var sb = new StringBuilder ();
			sb.AppendFormat ("[{0}]: {1}", GetType ().Name, message);
			if (args.Length > 0)
				sb.Append (" -");
			foreach (var arg in args) {
				sb.Append (" ");
				sb.Append (arg);
			}
			var formatted = sb.ToString ();
			ctx.LogDebug (level, formatted);
		}

		protected sealed override Task MainLoop (TestContext ctx, CancellationToken cancellationToken)
		{
			return ConnectionHandler.MainLoop (ctx, cancellationToken);
		}
	}
}

