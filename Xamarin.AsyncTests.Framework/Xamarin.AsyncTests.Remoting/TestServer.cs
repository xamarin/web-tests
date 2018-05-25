//
// TestServer.cs
//
// Author:
//       Martin Baulig <martin.baulig@xamarin.com>
//
// Copyright (c) 2014 Xamarin Inc. (http://www.xamarin.com)
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
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Xamarin.AsyncTests.Remoting
{
	using Portable;
	using Framework;

	public abstract class TestServer
	{
		public TestApp App {
			get;
			private set;
		}

		public TestSession Session {
			get;
			private set;
		}

		public TestSuite TestSuite {
			get;
			private set;
		}

		internal abstract Connection Connection {
			get;
		}

		internal abstract EndPoint EndPoint {
			get;
		}

		protected TestServer (TestApp app)
		{
			App = app;
		}

		static SocketListener CreateListener (EndPoint endpoint)
		{
			var listener = new SocketListener ();
			listener.Start (endpoint);
			return listener;
		}

		public static async Task<TestServer> StartLocal (TestApp app, TestFramework framework, CancellationToken cancellationToken)
		{
			var server = new LocalTestServer (app, framework);
			await server.Initialize (cancellationToken);
			return server;
		}

		public static async Task<TestServer> WaitForConnection (TestApp app, EndPoint address, CancellationToken cancellationToken)
		{
			var listener = CreateListener (address);
			var socket = await listener.AcceptSocketAsync (cancellationToken).ConfigureAwait (false);
			cancellationToken.ThrowIfCancellationRequested ();

			var clientConnection = new ClientConnection (app, listener);
			var client = new Client (app, clientConnection);
			await client.Initialize (cancellationToken);
			return client;
		}

		public static async Task<TestServer> LaunchApplication (TestApp app, EndPoint address, ApplicationLauncher launcher, LauncherOptions options, CancellationToken cancellationToken)
		{
			var listener = CreateListener (address);

			var sb = new StringBuilder ();

			if (options != null) {
				if (options.Category != null)
					sb.AppendFormat ("--category={0} ", options.Category);
				if (options.Features != null)
					sb.AppendFormat ("--features={0} ", options.Features);
			}

			if (!string.IsNullOrWhiteSpace (app.PackageName))
				sb.AppendFormat ("--package-name={0} ", app.PackageName);

			sb.Append ($"connect {address}");

			var process = await launcher.LaunchApplication (sb.ToString (), cancellationToken);

			var socket = await listener.AcceptSocketAsync (cancellationToken).ConfigureAwait (false);
			cancellationToken.ThrowIfCancellationRequested ();

			var launcherConnection = new LauncherConnection (app, listener, process);
			var client = new Client (app, launcherConnection);
			await client.Initialize (cancellationToken);
			cancellationToken.ThrowIfCancellationRequested ();

			return client;
		}

		public static async Task<TestServer> ConnectToRemote (TestApp app, EndPoint address, TestFramework framework, CancellationToken cancellationToken)
		{
			var client = new SocketClient ();
			await client.ConnectAsync (address, cancellationToken).ConfigureAwait (false);

			var serverConnection = new ServerConnection (app, framework, client);
			var server = new Server (app, framework, serverConnection);
			await server.Initialize (cancellationToken);
			return server;
		}

		public static async Task<TestServer> ConnectToGui (TestApp app, EndPoint address, TestFramework framework, CancellationToken cancellationToken)
		{
			var client = new SocketClient ();
			await client.ConnectAsync (address, cancellationToken).ConfigureAwait (false);

			var serverConnection = new ServerConnection (app, framework, client);
			var server = new Server (app, framework, serverConnection);
			await server.Initialize (cancellationToken);
			return server;
		}

		public static async Task<TestServer> StartServer (TestApp app, EndPoint address, TestFramework framework, CancellationToken cancellationToken)
		{
			var listener = CreateListener (address);
			await listener.AcceptSocketAsync (cancellationToken).ConfigureAwait (false);
			cancellationToken.ThrowIfCancellationRequested ();

			var serverConnection = new ServerConnection (app, framework, listener);
			var server = new Server (app, framework, serverConnection);
			await server.Initialize (cancellationToken);
			return server;
		}

		public static async Task<TestServer> ConnectToServer (TestApp app, EndPoint address, CancellationToken cancellationToken)
		{
			var connection = new SocketClient ();
			await connection.ConnectAsync (address, cancellationToken).ConfigureAwait (false);

			var clientConnection = new ClientConnection (app, connection);
			var client = new Client (app, clientConnection);
			await client.Initialize (cancellationToken);
			return client;
		}

		public static async Task<TestServer> ConnectToForkedParent (TestApp app, EndPoint address, TestFramework framework, CancellationToken cancellationToken)
		{
			var connection = new SocketClient ();
			await connection.ConnectAsync (address, cancellationToken).ConfigureAwait (false);

			cancellationToken.ThrowIfCancellationRequested ();
			var serverConnection = new ServerConnection (app, framework, connection);

			var server = new Server (app, framework, serverConnection);
			await server.Initialize (cancellationToken);
			return server;
		}

		public static async Task<TestServer> ConnectToForkedDomain (TestApp app, TestFramework framework, IExternalDomainServer server, CancellationToken cancellationToken)
		{
			var connection = new ExternalDomainServer (app, framework, server);

			var testServer = new Server (app, framework, connection);
			await testServer.Initialize (cancellationToken).ConfigureAwait (false);
			return testServer;
		}

		public static async Task<TestServer> CreatePipe (TestApp app, EndPoint endpoint, PipeArguments arguments, CancellationToken cancellationToken)
		{
			var listener = CreateListener (endpoint);

			var monoPath = Path.Combine (arguments.MonoPrefix, "bin", "mono");

			var cmd = new StringBuilder ();
			cmd.Append ("--debug ");
			if (arguments.ConsolePath != null)
				cmd.Append (arguments.ConsolePath);
			else
				cmd.Append (arguments.Assembly);
			if (arguments.Dependencies != null) {
				foreach (var dependency in arguments.Dependencies) {
					cmd.AppendFormat (" --dependency={0}", dependency);
				}
			}
			cmd.Append ($" --gui={listener.LocalEndPoint}");
			if (arguments.ExtraArguments != null)
				cmd.AppendFormat (" {0}", arguments.ExtraArguments);
			if (arguments.ConsolePath != null) {
				cmd.Append (" ");
				cmd.Append (arguments.Assembly);
			}

			var support = DependencyInjector.Get<IForkedProcessLauncher> ();
			var process = await support.LaunchApplication (monoPath, cmd.ToString (), cancellationToken).ConfigureAwait (false);

			cancellationToken.ThrowIfCancellationRequested ();

			using (var cts = CancellationTokenSource.CreateLinkedTokenSource (cancellationToken)) {
				EventHandler<int> cancelFunc = delegate {
					try {
						cts.Cancel ();
					} catch {
						;
					}
				};
				process.ExitedEvent += cancelFunc;
				if (process.HasExited)
					cancelFunc (null, 1);

				await listener.AcceptSocketAsync (cts.Token).ConfigureAwait (false);

				var clientConnection = new ClientConnection (app, listener);
				var client = new Client (app, clientConnection);
				await client.Initialize (cts.Token);

				process.ExitedEvent -= cancelFunc;

				return client;
			}
		}

		public static async Task<TestServer> ForkApplication (TestApp app, CancellationToken cancellationToken)
		{
			var listener = CreateListener (null);

			var sb = new StringBuilder ();

			if (!string.IsNullOrWhiteSpace (app.PackageName))
				sb.Append ($"--package-name={app.PackageName} ");

			var address = listener.LocalEndPoint;

			sb.AppendFormat ($"fork {address}");

			var launcher = DependencyInjector.Get<IForkedProcessLauncher> ();
			var process = await launcher.LaunchApplication (sb.ToString (), cancellationToken);

			await listener.AcceptSocketAsync (cancellationToken).ConfigureAwait (false);
			cancellationToken.ThrowIfCancellationRequested ();

			var launcherConnection = new LauncherConnection (app, listener, process);
			var client = new Client (app, launcherConnection);
			await client.Initialize (cancellationToken);
			cancellationToken.ThrowIfCancellationRequested ();

			return client;
		}

		static int nextDomainId;

		public static async Task<TestServer> ForkAppDomain (
			TestApp app, string domainName, CancellationToken cancellationToken)
		{
			if (string.IsNullOrEmpty (domainName))
				domainName = $"ExternalDomain{++nextDomainId}";

			var launcher = DependencyInjector.Get<IExternalDomainSupport> ();
			var host = launcher.Create (app, domainName);

			var domainConnection = new ExternalDomainClient (app, host);
			var client = new Client (app, domainConnection);

			await host.Start (client, cancellationToken).ConfigureAwait (false);
			cancellationToken.ThrowIfCancellationRequested ();

			await client.Initialize (cancellationToken);
			cancellationToken.ThrowIfCancellationRequested ();

			return client;
		}

		protected virtual async Task Initialize (CancellationToken cancellationToken)
		{
			cancellationToken.ThrowIfCancellationRequested ();
			Session = await GetTestSession (cancellationToken);

			cancellationToken.ThrowIfCancellationRequested ();
			TestSuite = Session.Suite;
		}

		public abstract Task<bool> WaitForExit (CancellationToken cancellationToken);

		public abstract Task Stop (CancellationToken cancellationToken);

		protected abstract Task<TestSession> GetTestSession (CancellationToken cancellationToken);

		class LocalTestServer : TestServer
		{
			public TestFramework Framework {
				get;
			}

			internal override Connection Connection => throw new InternalErrorException ();

			internal override EndPoint EndPoint => throw new InternalErrorException ();

			public LocalTestServer (TestApp app, TestFramework framework)
				: base (app)
			{
				Framework = framework;
			}

			public override Task<bool> WaitForExit (CancellationToken cancellationToken)
			{
				return Task.FromResult (true);
			}

			public override Task Stop (CancellationToken cancellationToken)
			{
				return Task.FromResult<object> (null);
			}

			protected override Task<TestSession> GetTestSession (CancellationToken cancellationToken)
			{
				return Task.FromResult (TestSession.CreateLocal (App, Framework));
			}
		}

		class Server : TestServer
		{
			Task serverTask;

			internal TestFramework Framework {
				get;
			}

			internal sealed override Connection Connection {
				get;
			}

			internal override EndPoint EndPoint => throw new InternalErrorException ();

			public Server (TestApp app, TestFramework framework, Connection server)
				: base (app)
			{
				Framework = framework;
				Connection = server;
			}

			protected override async Task Initialize (CancellationToken cancellationToken)
			{
				await Connection.Start (cancellationToken);

				serverTask = Connection.Run (cancellationToken);
				await base.Initialize (cancellationToken);
			}

			public override async Task<bool> WaitForExit (CancellationToken cancellationToken)
			{
				await Task.WhenAll (serverTask);
				return false;
			}

			public override async Task Stop (CancellationToken cancellationToken)
			{
				try {
					await Connection.Shutdown ();
				} catch {
					;
				}
				try {
					Connection.Stop ();
				} catch {
					;
				}
			}

			protected override Task<TestSession> GetTestSession (CancellationToken cancellationToken)
			{
				return Task.FromResult (TestSession.CreateLocal (App, Framework));
			}
		}

		class Client : TestServer
		{
			Task clientTask;

			internal sealed override Connection Connection {
				get;
			}

			internal override EndPoint EndPoint => throw new InternalErrorException ();

			public Client (TestApp app, Connection client)
				: base (app)
			{
				Connection = client;
			}

			protected override async Task Initialize (CancellationToken cancellationToken)
			{
				await Connection.Start (cancellationToken);

				clientTask = Connection.Run (cancellationToken);
				await base.Initialize (cancellationToken);
			}

			public override async Task<bool> WaitForExit (CancellationToken cancellationToken)
			{
				await Task.WhenAll (clientTask);
				return false;
			}

			public override async Task Stop (CancellationToken cancellationToken)
			{
				try {
					await Connection.Shutdown ();
				} catch {
					;
				}
				try {
					Connection.Stop ();
				} catch {
					;
				}
			}

			protected override Task<TestSession> GetTestSession (CancellationToken cancellationToken)
			{
				return RemoteObjectManager.GetRemoteTestSession (Connection, cancellationToken);
			}
		}

		class Launcher : Client
		{
			public Launcher (TestApp app, LauncherConnection connection)
				: base (app, connection)
			{
			}
		}
	}
}

