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

	public class ConsoleClient : ClientConnection
	{
		public Program Program {
			get;
			private set;
		}

		public ConsoleClient (Program program, Stream stream)
			: base (program, stream)
		{
			Program = program;
		}

		protected override Task<TestSuite> GetLocalTestSuite (CancellationToken cancellationToken)
		{
			var assembly = typeof(WebTestFeatures).Assembly;
			var framework = TestFramework.GetLocalFramework (assembly);
			return framework.LoadTestSuite (App, cancellationToken);
		}

		public async Task RunClient (CancellationToken cancellationToken)
		{
			await Start (cancellationToken);

			if (Program.Run) {
				var result = await RunTestSuite (cancellationToken);

				Debug ("Done running: {0}", result.Status);

				if (Program.ResultOutput != null)
					SaveTestResult (result);
			}

			if (!Program.IsServer && !Program.Wait) {
				Debug ("Shutting down.");
				await Shutdown ();
			}

			await Run (cancellationToken);
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

