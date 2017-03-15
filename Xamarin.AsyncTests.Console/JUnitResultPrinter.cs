//
// JUnitResultPrinter.cs
//
// Author:
//       Martin Baulig <martin.baulig@xamarin.com>
//
// Copyright (c) 2017 Xamarin Inc. (http://www.xamarin.com)

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
using System.Text;
using System.Xml;
using System.Xml.Linq;
using Xamarin.AsyncTests.Framework;

namespace Xamarin.AsyncTests.Console
{
	public class JUnitResultPrinter
	{
		public TestResult Result {
			get;
			private set;
		}

		public bool ShowIgnored {
			get { return showIgnored ?? true; }
			set { showIgnored = value; }
		}

		bool? showIgnored;

		JUnitResultPrinter (TestResult result)
		{
			Result = result;
		}

		public static void Print (TestResult result, string output)
		{
			var settings = new XmlWriterSettings {
				Indent = true
			};
			using (var writer = XmlWriter.Create (output, settings)) {
				var printer = new JUnitResultPrinter (result);
				var root = new XElement ("testsuites");
				printer.Visit (root, TestName.Empty, result);
				root.WriteTo (writer);
			}
		}

		XElement Print (XElement root, TestName parent, TestResult node)
		{
			var timestamp = new DateTime (DateTime.Now.Ticks, DateTimeKind.Unspecified);
			var suite = new XElement ("testsuite");
			suite.SetAttributeValue ("name", parent.FullName);

#if FIXME
			if (!node.HasChildren || node.Children.Count == 0) {
				switch (node.Status) {
				case TestStatus.Success:
					suite.SetAttributeValue ("errors", "0");
					suite.SetAttributeValue ("failures", "0");
					suite.SetAttributeValue ("tests", "1");
					break;
				case TestStatus.Error:
					suite.SetAttributeValue ("errors", "1");
					suite.SetAttributeValue ("failures", "0");
					suite.SetAttributeValue ("tests", "0");
					break;
				}
			} else {
#if FIXME
				suite.SetAttributeValue ("errors", "0");
				suite.SetAttributeValue ("failures", "0");
				suite.SetAttributeValue ("tests", "1");
#endif
			}
#endif

			suite.SetAttributeValue ("timestamp", timestamp.ToString ("yyyy-MM-dd'T'HH:mm:ss"));
			suite.SetAttributeValue ("hostname", "localhost");
			if (node.ElapsedTime != null)
				suite.SetAttributeValue ("time", node.ElapsedTime.Value.TotalSeconds);
			root.Add (suite);

			var properties = new XElement ("properties");
			suite.Add (properties);

			if (!node.HasChildren || node.Children.Count == 0) {
				var test = CreateTestCase (node);
				suite.Add (test);
			}

			var systemOut = new XElement ("system-out");
			suite.Add (systemOut);

			var systemErr = new XElement ("system-err");
			suite.Add (systemErr);

			if (node.Path != null) {
				var serializedPath = node.Path.SerializePath ().ToString ();
				systemOut.Add (serializedPath);
				systemOut.Add (Environment.NewLine);
				systemOut.Add (Environment.NewLine);
			}

			systemOut.Add (string.Format ("TEST: {0} {1}\n", node.HasMessages, node.HasLogEntries));

			if (node.Name.HasParameters) {
				foreach (var parameter in node.Name.Parameters) {
					if (parameter.IsHidden)
						continue;
					var propNode = new XElement ("property");
					propNode.SetAttributeValue ("name", parameter.Name);
					propNode.SetAttributeValue ("value", parameter.Value);
					systemOut.Add (string.Format ("{0} = {1}{2}", parameter.Name, parameter.Value, Environment.NewLine));
					properties.Add (propNode);
				}
			}

			systemOut.Add (Environment.NewLine);

			if (node.HasMessages) {
				foreach (var message in node.Messages) {
					systemOut.Add (message + Environment.NewLine);
				}
			}

			if (node.HasLogEntries) {
				foreach (var entry in node.LogEntries) {
					systemOut.Add (string.Format ("LOG: {0} {1} {2}\n", entry.Kind, entry.LogLevel, entry.Text));
				}
			}

			return suite;
		}

		XElement CreateTestCase (TestResult result)
		{
			var test = new XElement ("testcase");
			test.SetAttributeValue ("name", result.Name.LocalName);
			test.SetAttributeValue ("status", result.Status);
			if (result.ElapsedTime != null)
				test.SetAttributeValue ("time", result.ElapsedTime.Value.TotalSeconds);

			var hasError = false;

			if (result.HasErrors) {
				foreach (var error in result.Errors) {
					var xerror = new XElement ("error");
					var savedException = error as SavedException;
					if (savedException != null) {
						xerror.SetAttributeValue ("type", savedException.Type);
						xerror.SetAttributeValue ("message", savedException.Message + "\n" + savedException.StackTrace);
					} else {
						xerror.SetAttributeValue ("type", error.GetType ().FullName);
						xerror.SetAttributeValue ("message", error.Message + "\n" + error.StackTrace);
					}
					test.Add (xerror);
					hasError = true;
				}
			}

			if (result.Status == TestStatus.Error && !hasError) {
				test.Add (new XElement ("error"));
			}

			return test;
		}


		string FormatName (TestName name)
		{
			return name.FullName;
		}

		void Visit (XElement root, TestName parent, TestResult result)
		{
			if (true && result.Status == TestStatus.Ignored)
				return;

			XElement node = root;

			if (result.Path == null ||
			    result.Path.Identifier == "suite" ||
			    result.Path.Identifier == "assembly") {
				;
			} else {
				node = Print (root, parent, result);
			}

			if (result.HasChildren) {
				foreach (var child in result.Children)
					Visit (node, result.Name, child);
			}
		}
	}
}

