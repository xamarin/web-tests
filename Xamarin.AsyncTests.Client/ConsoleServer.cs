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
		TestSuite suite;

		public Program Program {
			get;
			private set;
		}

		public ConsoleServer (Program program, Stream stream)
			: base (program.Context, stream)
		{
			Program = program;
			helloTcs = new TaskCompletionSource<bool> ();
		}

		protected override async Task<TestSuite> OnLoadTestSuite (CancellationToken cancellationToken)
		{
			var assembly = typeof(WebTestFeatures).Assembly;
			suite = await TestSuite.LoadAssembly (Context, assembly);
			Context.CurrentTestSuite = suite;
			return suite;
		}

		protected override Task<TestResult> OnRunTestSuite (CancellationToken cancellationToken)
		{
			return OnRun (suite, cancellationToken);
		}

		public async Task RunServer ()
		{
			var task = Task.Factory.StartNew (async () => {
				await MainLoop ();
				Context.CurrentTestSuite = null;
				suite = null;
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
					suite = null;
				}
			});

			Debug ("Client started.");

			await Hello (Program.UseServerSettings, CancellationToken.None);

			suite = await LoadTestSuite (CancellationToken.None);
			Debug ("Got test suite from server: {0}", suite);

			if (!Program.NoRun) {
				var result = await RunTestSuite (CancellationToken.None);

				Debug ("Done running: {0}", result.Status);

				if (Program.ResultOutput != null)
					SaveTestResult (result);
			}

			if (!Program.Wait) {
				Debug ("Shutting down.");
				shutdownRequested = true;
				await Shutdown ();
			}

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

		void SaveTestResult (TestResult result)
		{
			var wxs = new XmlWriterSettings ();
			wxs.Indent = true;
			using (var writer = new StreamWriter (Program.ResultOutput)) {
				var xml = XmlWriter.Create (writer, wxs);
				var node = Connection.WriteTestResult (result);
				node.WriteTo (xml);
				xml.Flush ();
			}
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

