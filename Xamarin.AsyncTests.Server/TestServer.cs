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

namespace Xamarin.AsyncTests.Server
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

		public static async Task<TestServer> StartLocal (TestApp app, CancellationToken cancellationToken)
		{
			var server = new LocalTestServer (app);
			await server.Initialize (cancellationToken);
			return server;
		}

		public static async Task<TestServer> CreatePipe (TestApp app, CancellationToken cancellationToken)
		{
			await Task.Yield ();

			cancellationToken.ThrowIfCancellationRequested ();
			var connection = await app.PortableSupport.ServerHost.CreatePipe (cancellationToken);

			var serverApp = new PipeApp (app.PortableSupport, app.Framework);

			var server = await StartServer (serverApp, connection.Server, cancellationToken);
			var client = await StartClient (app, connection.Client, cancellationToken);

			var pipe = new PipeServer (app, client, server);
			await pipe.Initialize (cancellationToken);
			return pipe;
		}

		static async Task<ServerConnection> StartServer (TestApp app, IServerConnection connection, CancellationToken cancellationToken)
		{
			cancellationToken.ThrowIfCancellationRequested ();
			var stream = await connection.Open (cancellationToken);

			cancellationToken.ThrowIfCancellationRequested ();
			return new ServerConnection (app, stream, connection);
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
			TestSuite = await Session.LoadTestSuite (cancellationToken);

			Connection.Debug ("GOT TEST SUITE: {0}", TestSuite);
		}

		public abstract Task<bool> Run (CancellationToken cancellationToken);

		public abstract Task Stop (CancellationToken cancellationToken);

		public abstract Task<TestSession> GetTestSession (CancellationToken cancellationToken);

		class LocalTestServer : TestServer
		{
			public LocalTestServer (TestApp app)
				: base (app)
			{
			}

			public override Task<bool> Run (CancellationToken cancellationToken)
			{
				return Task.FromResult (true);
			}

			public override Task Stop (CancellationToken cancellationToken)
			{
				return Task.FromResult<object> (null);
			}

			public override Task<TestSession> GetTestSession (CancellationToken cancellationToken)
			{
				return Task.FromResult (TestSession.CreateLocal (App, App.Framework));
			}
		}

		class PipeApp : TestApp
		{
			IPortableSupport support;
			SettingsBag settings;
			TestFramework framework;

			public PipeApp (IPortableSupport support, TestFramework framework)
			{
				this.support = support;
				this.framework = framework;

				settings = SettingsBag.CreateDefault ();
			}

			public IPortableSupport PortableSupport {
				get { return support; }
			}

			public TestFramework Framework {
				get { return framework; }
			}

			public TestLogger Logger {
				get { throw new ServerErrorException (); }
			}

			public SettingsBag Settings {
				get { return settings; }
			}
		}

		class PipeServer : TestServer
		{
			ClientConnection client;
			ServerConnection server;
			Task serverTask;
			Task clientTask;

			public PipeServer (TestApp app, ClientConnection client, ServerConnection server)
				: base (app)
			{
				this.client = client;
				this.server = server;
			}

			protected override async Task Initialize (CancellationToken cancellationToken)
			{
				await Task.WhenAll (server.Start (cancellationToken), client.Start (cancellationToken));

				serverTask = server.Run (cancellationToken);
				clientTask = client.Run (cancellationToken);
				await base.Initialize (cancellationToken);
			}

			public override async Task<bool> Run (CancellationToken cancellationToken)
			{
				await Task.Yield ();
				Connection.Debug ("RUN PIPE");
				await Task.WhenAll (serverTask, clientTask);
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
					await server.Shutdown ();
				} catch {
					;
				}
				try {
					client.Stop();
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
				return RemoteObjectManager.GetRemoteTestSession (client, cancellationToken);
			}
		}
	}
}

