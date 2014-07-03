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
using System.Threading;
using System.Threading.Tasks;

namespace Xamarin.AsyncTests.UI
{
	using Server;

	public class TestServer : ServerConnection
	{
		public TestApp App {
			get;
			private set;
		}

		public override TestContext Context {
			get { return App.Context; }
		}

		TestServer (TestApp app, Stream stream)
			: base (stream)
		{
			App = app;
		}

		public static async Task<TestServer> Start (TestApp app, CancellationToken cancellationToken)
		{
			if (app.Server == null)
				return null;

			var stream = await app.Server.Start (cancellationToken);
			var server = new TestServer (app, stream);
			server.Run ();
			return server;
		}

		void Debug (string message, params object[] args)
		{
			System.Diagnostics.Debug.WriteLine (message, args);
		}

		#region implemented abstract members of ServerConnection

		protected override Task OnLoadResult (TestResult result)
		{
			return Task.Run (() => {
				App.RootTestResult.Result.AddChild (result);
			});
		}

		protected override void OnMessage (string message)
		{
			Debug ("MESSAGE: {0}", message);
		}

		protected override void OnDebug (int level, string message)
		{
			Debug ("DEBUG ({0}): {1}", level, message);
		}

		protected override Task OnHello (CancellationToken cancellationToken)
		{
			return Task.Run (() => {
				Debug ("HELLO WORLD!");
			});
		}

		#endregion
	}
}

