//
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

		public MonoConnectionTestFlags ConnectionFlags {
			get { return Provider.Flags; }
		}

		public MonoConnectionTestProvider Provider {
			get;
			private set;
		}

		public MonoConnectionHandler ConnectionHandler {
			get;
			private set;
		}

		public MonoConnectionTestRunner (IServer server, IClient client, MonoConnectionTestProvider provider, MonoConnectionTestParameters parameters)
			: base (server, client, parameters)
		{
			Provider = provider;

			ConnectionHandler = CreateConnectionHandler ();
		}

		public static MonoConnectionTestFlags GetConnectionFlags (TestContext ctx, MonoConnectionTestCategory category)
		{
			switch (category) {
			case MonoConnectionTestCategory.SimpleMonoClient:
			case MonoConnectionTestCategory.SelectClientCipher:
				return MonoConnectionTestFlags.RequireMonoClient;
			case MonoConnectionTestCategory.SimpleMonoServer:
			case MonoConnectionTestCategory.SelectServerCipher:
				return MonoConnectionTestFlags.RequireMonoServer;
			case MonoConnectionTestCategory.SimpleMonoConnection:
			case MonoConnectionTestCategory.MonoProtocolVersions:
			case MonoConnectionTestCategory.SelectCipher:
				return MonoConnectionTestFlags.RequireMono;
			case MonoConnectionTestCategory.ClientConnection:
			case MonoConnectionTestCategory.MartinTestClient:
				return MonoConnectionTestFlags.RequireMonoClient;
			case MonoConnectionTestCategory.ServerConnection:
			case MonoConnectionTestCategory.MartinTestServer:
				return MonoConnectionTestFlags.RequireMonoServer;
			case MonoConnectionTestCategory.Connection:
			case MonoConnectionTestCategory.CertificateChecks:
				return MonoConnectionTestFlags.RequireMono;
			case MonoConnectionTestCategory.MartinTest:
				return MonoConnectionTestFlags.RequireMono | MonoConnectionTestFlags.RequireTls12;
			default:
				ctx.AssertFail ("Unsupported instrumentation category: '{0}'.", category);
				return MonoConnectionTestFlags.None;
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

			ConnectionHandler.InitializeConnection (ctx);
			base.InitializeConnection (ctx);
		}

		protected abstract MonoConnectionHandler CreateConnectionHandler ();

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

		protected void CheckCipher (TestContext ctx, IMonoCommonConnection connection, CipherSuiteCode cipher)
		{
			ctx.Assert (connection.SupportsConnectionInfo, "supports connection info");
			var connectionInfo = connection.GetConnectionInfo ();

			if (ctx.Expect (connectionInfo, Is.Not.Null, "connection info"))
				ctx.Expect (connectionInfo.CipherSuiteCode, Is.EqualTo (cipher), "expected cipher");
		}

		protected override Task OnRun (TestContext ctx, CancellationToken cancellationToken)
		{
			var monoClient = Client as IMonoClient;
			var monoServer = Server as IMonoServer;

			if (monoClient != null) {
				var expectedCipher = Parameters.ExpectedClientCipher ?? Parameters.ExpectedCipher;
				if (expectedCipher != null)
					CheckCipher (ctx, monoClient, expectedCipher.Value);
			}

			if (monoServer != null) {
				var expectedCipher = Parameters.ExpectedServerCipher ?? Parameters.ExpectedCipher;
				if (expectedCipher != null)
					CheckCipher (ctx, monoServer, expectedCipher.Value);
			}

			if (!IsManualConnection && Parameters.ProtocolVersion != null) {
				ctx.Expect (Client.ProtocolVersion, Is.EqualTo (Parameters.ProtocolVersion), "client protocol version");
				ctx.Expect (Server.ProtocolVersion, Is.EqualTo (Parameters.ProtocolVersion), "server protocol version");
			}

			if (Server.Provider.SupportsSslStreams && Parameters.RequireClientCertificate) {
				ctx.Expect (Server.SslStream.HasRemoteCertificate, "has remote certificate");
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

		async Task HandleConnection (TestContext ctx, ICommonConnection connection, Task readTask, Task writeTask, CancellationToken cancellationToken)
		{
			var t1 = readTask.ContinueWith (t => {
				LogDebug (ctx, 1, "HandleConnection - read done", connection, t.Status, t.IsFaulted, t.IsCanceled);
				if (t.IsFaulted || t.IsCanceled)
					Dispose ();
			});
			var t2 = writeTask.ContinueWith (t => {
				LogDebug (ctx, 1, "HandleConnection - write done", connection, t.Status, t.IsFaulted, t.IsCanceled);
				if (t.IsFaulted || t.IsCanceled)
					Dispose ();
			});

			LogDebug (ctx, 1, "HandleConnection", connection);

			await Task.WhenAll (readTask, writeTask, t1, t2);
			cancellationToken.ThrowIfCancellationRequested ();

			LogDebug (ctx, 1, "HandleConnection done", connection);
		}

		protected sealed override Task MainLoop (TestContext ctx, CancellationToken cancellationToken)
		{
			return ConnectionHandler.MainLoop (ctx, cancellationToken);
		}

		public override Task<bool> Shutdown (TestContext ctx, CancellationToken cancellationToken)
		{
			ConnectionHandler.Shutdown (ctx);
			return base.Shutdown (ctx, cancellationToken);
		}

		public async Task ExpectAlert (TestContext ctx, AlertDescription alert, CancellationToken cancellationToken)
		{
			var serverTask = Server.WaitForConnection (ctx, cancellationToken);
			var clientTask = Client.WaitForConnection (ctx, cancellationToken);

			var t1 = clientTask.ContinueWith (t => MonoConnectionHelper.ExpectAlert (ctx, t, alert, "client"));
			var t2 = serverTask.ContinueWith (t => MonoConnectionHelper.ExpectAlert (ctx, t, alert, "server"));

			await Task.WhenAll (t1, t2);
		}
	}
}

