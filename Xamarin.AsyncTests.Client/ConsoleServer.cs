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
using System.Xml;
using System.Xml.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xamarin.WebTests;

namespace Xamarin.AsyncTests.Client
{
	using Framework;
	using Server;

	public class ConsoleServer : Connection
	{
		TaskCompletionSource<bool> helloTcs;
		bool shutdownRequested;
		bool useServerSettings;

		public ConsoleServer (TestContext context, Stream stream, bool useServerSettings)
			: base (context, stream)
		{
			this.useServerSettings = useServerSettings;
			helloTcs = new TaskCompletionSource<bool> ();
		}

		protected override async Task<TestSuite> OnLoadTestSuite (CancellationToken cancellationToken)
		{
			var assembly = typeof(WebTestFeatures).Assembly;
			var suite = await TestSuite.LoadAssembly (Context, assembly);
			Context.CurrentTestSuite = suite;
			return suite;
		}

		public async Task RunServer ()
		{
			var task = Task.Factory.StartNew (async () => {
				await MainLoop ();
				Context.CurrentTestSuite = null;
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
					Context.CurrentTestSuite = null;
				}
			});

			Debug ("Client started.");

			await Hello (useServerSettings, CancellationToken.None);

			var suite = await LoadTestSuite (CancellationToken.None);
			Debug ("Got test suite from server: {0}", suite);

			await Task.Delay (2500);

			var result = await RunTest (suite);

			Debug ("Done running: {0}", result.Status);

			Debug ("RESULT:\n{0}\n", DumpTestResult (result));

			await Task.Delay (10000);

			Debug ("Shutting down.");

			shutdownRequested = true;
			await Shutdown ();

			await task;
		}

		static string Write (XElement node)
		{
			var wxs = new XmlWriterSettings ();
			wxs.Indent = true;
			using (var writer = new StringWriter ()) {
				var xml = XmlWriter.Create (writer, wxs);
				node.WriteTo (xml);
				xml.Flush ();
				return writer.ToString ();
			}
		}

		static string DumpTestResult (TestResult result)
		{
			var node = Connection.WriteTestResult (result);
			return Write (node);
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

		protected override async Task<TestResult> OnRun (TestCase test, CancellationToken cancellationToken)
		{
			var result = new TestResult (test.Name);

			try {
				await test.Run (Context, result, cancellationToken).ConfigureAwait (false);
			} catch (OperationCanceledException) {
				result.Status = TestStatus.Canceled;
			} catch (Exception ex) {
				result.AddError (ex);
			}

			return result;
		}

		#endregion
	}
}

