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

		protected TestServer (TestApp app)
		{
			App = app;
		}

		public static TestServer StartLocal (TestApp app)
		{
			return new LocalTestServer (app);
		}

		public static async Task<TestServer> StartServer (TestApp app, CancellationToken cancellationToken)
		{
			await Task.Yield ();

			var connection = await app.PortableSupport.ServerHost.Start (cancellationToken);
			var stream = await connection.Open (cancellationToken);

			var server = new ServerConnection (app, stream, connection);
			return new RemoteTestServer (app, server);
		}

		public static async Task<TestServer> Connect (TestApp app, string address, CancellationToken cancellationToken)
		{
			await Task.Yield ();

			var connection = await app.PortableSupport.ServerHost.Connect (address, cancellationToken);
			var stream = await connection.Open (cancellationToken);

			var server = new ServerConnection (app, stream, connection);
			return new RemoteTestServer (app, server);
		}

		public abstract Task<bool> Run (CancellationToken cancellationToken);

		public abstract Task Stop (CancellationToken cancellationToken);

		public abstract Task<TestFramework> GetTestFramework (CancellationToken cancellationToken);

		public async Task<TestSuite> LoadTestSuite (CancellationToken cancellationToken)
		{
			var framework = await GetTestFramework (cancellationToken);
			return await framework.LoadTestSuite (App, cancellationToken);
		}

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

			public override Task<TestFramework> GetTestFramework (CancellationToken cancellationToken)
			{
				return Task.Run (() => App.GetLocalTestFramework ());
			}
		}

		class RemoteTestServer : TestServer
		{
			ServerConnection server;

			public RemoteTestServer (TestApp app, ServerConnection server)
				: base (app)
			{
				this.server = server;
			}

			public override async Task<bool> Run (CancellationToken cancellationToken)
			{
				if (server != null)
					await server.Run (cancellationToken);
				return false;
			}

			public override async Task Stop (CancellationToken cancellationToken)
			{
				if (server != null) {
					try {
						await server.Shutdown ();
					} catch {
						;
					}
					server.Stop ();
					server = null;
				}
			}

			public override Task<TestFramework> GetTestFramework (CancellationToken cancellationToken)
			{
				return RemoteObjectManager.GetRemoteTestFramework (server, cancellationToken);
			}
		}
	}
}

