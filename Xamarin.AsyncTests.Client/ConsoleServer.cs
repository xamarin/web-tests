//
// ConsoleClient.cs
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

namespace Xamarin.AsyncTests.Client
{
	using Framework;
	using Server;

	public class ConsoleServer : Connection
	{
		readonly ConsoleContext context;
		TaskCompletionSource<bool> helloTcs;
		bool shutdownRequested;

		public override TestContext Context {
			get { return context; }
		}

		public ConsoleServer (Stream stream)
			: base (stream)
		{
			context = new ConsoleContext ();
			helloTcs = new TaskCompletionSource<bool> ();
		}

		protected override Task<TestSuite> OnLoadTestSuite (CancellationToken cancellationToken)
		{
			return context.LoadTestSuite (cancellationToken);
		}

		public async Task RunServer ()
		{
			var task = Task.Factory.StartNew (async () => {
				await MainLoop ();
				context.UnloadTestSuite ();
			});

			Debug ("Server started.");

			await helloTcs.Task;
			Debug ("Handshake complete.");
			await task;
		}

		public async Task RunClient ()
		{
			var task = Task.Run (async () => {
				try {
					await MainLoop ();
				} catch (Exception ex) {
					if (shutdownRequested)
						return;
					Debug ("MAIN LOOP EX: {0}", ex);
					throw;
				} finally {
					context.UnloadTestSuite ();
				}
			});

			Debug ("Client started.");

			var settings = await GetSettings (CancellationToken.None);
			context.MergeSettings (settings);

			Debug ("SETTINGS:\n{0}\n", DumpSettings (context.Settings));

			var suite = await LoadTestSuite (CancellationToken.None);
			Debug ("GOT TEST SUITE: {0}", suite);

			await Task.Delay (10000);

			Debug ("DONE WAITING");

			await RunTest (suite);

			Debug ("SHUTTING DOWN");

			shutdownRequested = true;
			await Shutdown ();

			await task;
		}

		public async Task<TestResult> RunTest (TestSuite suite)
		{
			var result = new TestResult (suite.Name);
			await suite.Run (Context, result, CancellationToken.None);
			return result;
		}

		protected override void OnShutdown ()
		{
			shutdownRequested = true;
			base.OnShutdown ();
		}

		#region implemented abstract members of Connection

		protected override void OnLogMessage (string message)
		{
			Debug (message);
		}

		protected override void OnDebug (int level, string message)
		{
			Debug (message);
		}

		#endregion
	}
}

