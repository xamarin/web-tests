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
using System.ComponentModel;

namespace Xamarin.AsyncTests.UI
{
	using Framework;
	using Server;

	public class TestServer : ServerConnection
	{
		public UITestApp App {
			get;
			private set;
		}

		public IServerConnection Connection {
			get;
			private set;
		}

		public bool IsServer {
			get;
			private set;
		}

		public TestServer (UITestApp app, Stream stream, IServerConnection connection, bool isServer)
			: base (app, stream)
		{
			App = app;
			Connection = connection;
			IsServer = isServer;
		}

		public override void Stop ()
		{
			try {
				base.Stop ();
			} catch {
				;
			}
			try {
				Connection.Close ();
			} catch {
				;
			}
		}

		#region implemented abstract members of ServerConnection

		protected override void OnLogMessage (string message)
		{
			Debug ("MESSAGE: {0}", message);
		}

		protected override void OnDebug (int level, string message)
		{
			Debug ("DEBUG ({0}): {1}", level, message);
		}

		#endregion

		protected override Task<TestSuite> GetLocalTestSuite (CancellationToken cancellationToken)
		{
			return TestSuite.LoadAssembly (App.Context, App.Assembly);
		}

		protected override async Task<TestResult> OnRunTestSuite (CancellationToken cancellationToken)
		{
			var result = App.RootTestResult.Result;

			Debug ("TEST: {0} {1} {2}", result, App.TestRunner.CurrentTestResult, App.TestRunner.CanStart);

			try {
				if (!App.TestRunner.CanStart)
					throw new InvalidOperationException ();

				await App.TestRunner.Run.Execute ();
			} catch (Exception ex) {
				result.AddError (ex);
			}

			return result;
		}
	}
}

