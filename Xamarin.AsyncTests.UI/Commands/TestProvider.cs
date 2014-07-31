//
// TestProvider.cs
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
using System.Windows.Input;
using Xamarin.AsyncTests;
using Xamarin.AsyncTests.Framework;
using Xamarin.Forms;

namespace Xamarin.AsyncTests.UI
{
	using Framework;
	using Server;

	public abstract class TestProvider
	{
		public UITestApp App {
			get;
			private set;
		}

		public string Name {
			get;
			private set;
		}

		public TestProvider (UITestApp app, string name)
		{
			App = app;
			Name = name;
		}

		public static TestProvider StartLocal (UITestApp app)
		{
			app.ServerManager.SetStatusMessage ("Loaded locally.");
			return new LocalTestProvider (app);
		}

		public static async Task<TestProvider> StartServer (UITestApp app, CancellationToken cancellationToken)
		{
			await Task.Yield ();

			var connection = await app.ServerHost.Start (cancellationToken);
			app.ServerManager.SetStatusMessage ("Server running ({0})", connection.Name);
			var stream = await connection.Open (cancellationToken);
			app.ServerManager.SetStatusMessage ("Got remote connection.");

			var server = new TestServer (app, stream, connection, true);
			return new RemoteTestProvider (app, server, connection);
		}

		public static async Task<TestProvider> Connect (UITestApp app, string address, CancellationToken cancellationToken)
		{
			await Task.Yield ();

			var connection = await app.ServerHost.Connect (address, cancellationToken);
			var stream = await connection.Open (cancellationToken);
			app.ServerManager.SetStatusMessage ("Connected to remote host.");

			var server = new TestServer (app, stream, connection, false);
			return new RemoteTestProvider (app, server, connection);
		}

		internal abstract Task<bool> Run (CancellationToken cancellationToken);

		internal abstract Task Stop (CancellationToken cancellationToken);

		public abstract Task<TestSuite> LoadTestSuite (CancellationToken cancellationToken);
	}
}

