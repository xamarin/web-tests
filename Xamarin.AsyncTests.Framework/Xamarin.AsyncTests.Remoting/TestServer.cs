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

		protected TestServer (TestApp app)
		{
			App = app;
		}

		public static async Task<TestServer> StartLocal (TestApp app, TestFramework framework, CancellationToken cancellationToken)
		{
			var server = new LocalTestServer (app, framework);
			await server.Initialize (cancellationToken);
			return server;
		}

		public static async Task<TestServer> WaitForConnection (TestApp app, IPortableEndPoint address, CancellationToken cancellationToken)
		{
			var support = DependencyInjector.Get<IServerHost> ();
			var connection = await support.Listen (address, cancellationToken);

			cancellationToken.ThrowIfCancellationRequested ();

			var stream = await connection.Open (cancellationToken);
			cancellationToken.ThrowIfCancellationRequested ();

			var clientConnection = new ClientConnection (app, stream, connection);
			var client = new Client (app, clientConnection);
			await client.Initialize (cancellationToken);
			return client;
		}

		public static async Task<TestServer> ConnectToGui (TestApp app, IPortableEndPoint address, TestFramework framework, CancellationToken cancellationToken)
		{
			var support = DependencyInjector.Get<IServerHost> ();
			var connection = await support.Connect (address, cancellationToken);

			var serverConnection = await StartServer (app, framework, connection, cancellationToken);
			var server = new Server (app, framework, serverConnection);
			await server.Initialize (cancellationToken);
			return server;
		}

		public static async Task<TestServer> StartServer (TestApp app, IPortableEndPoint address, TestFramework framework, CancellationToken cancellationToken)
		{
			var support = DependencyInjector.Get<IServerHost> ();
			var connection = await support.Listen (address, cancellationToken);

			var serverConnection = await StartServer (app, framework, connection, cancellationToken);
			var server = new Server (app, framework, serverConnection);
			await server.Initialize (cancellationToken);
			return server;
		}

		public static async Task<TestServer> ConnectToServer (TestApp app, IPortableEndPoint address, CancellationToken cancellationToken)
		{
			var support = DependencyInjector.Get<IServerHost> ();
			var connection = await support.Connect (address, cancellationToken);

			var clientConnection = await StartClient (app, connection, cancellationToken);
			var client = new Client (app, clientConnection);
			await client.Initialize (cancellationToken);
			return client;
		}

		public static async Task<TestServer> CreatePipe (TestApp app, IPortableEndPoint endpoint, PipeArguments arguments, CancellationToken cancellationToken)
		{
			var support = DependencyInjector.Get<IServerHost> ();
			var connection = await support.CreatePipe (endpoint, arguments, cancellationToken);

			cancellationToken.ThrowIfCancellationRequested ();

			using (var cts = CancellationTokenSource.CreateLinkedTokenSource (cancellationToken)) {
				EventHandler cancelFunc = delegate {
					try {
						cts.Cancel ();
					} catch {
						;
					}
				};
				connection.ExitedEvent += cancelFunc;
				if (connection.HasExited)
					cancelFunc (null, EventArgs.Empty);

				var stream = await connection.Open (cts.Token);
				cts.Token.ThrowIfCancellationRequested ();

				var clientConnection = new ClientConnection (app, stream, connection);
				var client = new Client (app, clientConnection);
				await client.Initialize (cts.Token);

				connection.ExitedEvent -= cancelFunc;

				return client;
			}
		}

		static async Task<ServerConnection> StartServer (TestApp app, TestFramework framework, IServerConnection connection, CancellationToken cancellationToken)
		{
			cancellationToken.ThrowIfCancellationRequested ();
			var stream = await connection.Open (cancellationToken);

			cancellationToken.ThrowIfCancellationRequested ();
			return new ServerConnection (app, framework, stream, connection);
		}

		static async Task<ClientConnection> StartClient (TestApp app, IServerConnection connection, CancellationToken cancellationToken)
		{
			cancellationToken.ThrowIfCancellationRequested ();
			var stream = await connection.Open (cancellationToken);

			cancellationToken.ThrowIfCancellationRequested ();
			return new ClientConnection (app, stream, connection);
		}

		protected virtual async Task Initialize (CancellationToken cancellationToken)
		{
			Connection.Debug ("INITIALIZE");

			cancellationToken.ThrowIfCancellationRequested ();
			Session = await GetTestSession (cancellationToken);

			Connection.Debug ("GOT SESSION: {0}", Session);

			cancellationToken.ThrowIfCancellationRequested ();
			TestSuite = Session.Suite;

			Connection.Debug ("GOT TEST SUITE: {0}", TestSuite);
		}

		public abstract Task<bool> WaitForExit (CancellationToken cancellationToken);

		public abstract Task Stop (CancellationToken cancellationToken);

		public abstract Task<TestSession> GetTestSession (CancellationToken cancellationToken);

		class LocalTestServer : TestServer
		{
			public TestFramework Framework {
				get;
				private set;
			}

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

			public override Task<TestSession> GetTestSession (CancellationToken cancellationToken)
			{
				return Task.FromResult (TestSession.CreateLocal (App, Framework));
			}
		}

		class Server : TestServer
		{
			TestFramework framework;
			ServerConnection server;
			Task serverTask;

			public Server (TestApp app, TestFramework framework, ServerConnection server)
				: base (app)
			{
				this.framework = framework;
				this.server = server;
			}

			protected override async Task Initialize (CancellationToken cancellationToken)
			{
				await server.Start (cancellationToken);

				serverTask = server.Run (cancellationToken);
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
					await server.Shutdown ();
				} catch {
					;
				}
				try {
					server.Stop ();
				} catch {
					;
				}
			}

			public override Task<TestSession> GetTestSession (CancellationToken cancellationToken)
			{
				return Task.FromResult (TestSession.CreateLocal (App, framework));
			}
		}

		class Client : TestServer
		{
			ClientConnection client;
			Task clientTask;

			public Client (TestApp app, ClientConnection client)
				: base (app)
			{
				this.client = client;
			}

			protected override async Task Initialize (CancellationToken cancellationToken)
			{
				await client.Start (cancellationToken);

				clientTask = client.Run (cancellationToken);
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
					await client.Shutdown ();
				} catch {
					;
				}
				try {
					client.Stop ();
				} catch {
					;
				}
			}

			public override Task<TestSession> GetTestSession (CancellationToken cancellationToken)
			{
				return RemoteObjectManager.GetRemoteTestSession (client, cancellationToken);
			}
		}
	}
}

