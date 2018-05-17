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
	public abstract class MonoConnectionTestRunner : ConnectionTestRunner
	{
		new public MonoConnectionTestParameters Parameters => (MonoConnectionTestParameters)base.Parameters;

		public ConnectionTestFlags ConnectionFlags => Provider.Flags;

		public ConnectionTestProvider Provider {
			get;
		}

		protected override string LogCategory => LogCategories.Listener;

		public static ConnectionTestFlags GetConnectionFlags (TestContext ctx, ConnectionTestCategory category)
		{
			switch (category) {
			case ConnectionTestCategory.SimpleMonoClient:
				return ConnectionTestFlags.RequireMonoClient;
			case ConnectionTestCategory.SimpleMonoServer:
				return ConnectionTestFlags.RequireMonoServer;
			case ConnectionTestCategory.SimpleMonoConnection:
			case ConnectionTestCategory.MonoProtocolVersions:
				return ConnectionTestFlags.RequireMono;
			case ConnectionTestCategory.CertificateChecks:
			case ConnectionTestCategory.SecurityFramework:
				return ConnectionTestFlags.RequireMono;
			case ConnectionTestCategory.MartinTest:
				return ConnectionTestFlags.None;
			default:
				ctx.AssertFail ("Unsupported instrumentation category: '{0}'.", category);
				return ConnectionTestFlags.None;
			}
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

			base.InitializeConnection (ctx);
		}

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
	}
}

