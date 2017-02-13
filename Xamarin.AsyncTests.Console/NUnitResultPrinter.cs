//
// ResultPrinter.cs
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
	public class NUnitResultPrinter
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
		int current;

		NUnitResultPrinter (TestResult result)
		{
			Result = result;
		}

		public static void Print (TestResult result, string output)
		{
			var settings = new XmlWriterSettings {
				Indent = true
			};
			using (var writer = XmlWriter.Create (output, settings)) {
				var printer = new NUnitResultPrinter (result);
				var root = new XElement ("testsuites");
				printer.Print (root);
				root.WriteTo (writer);
			}
		}

		bool Print (XElement root)
		{
			var suite = new XElement ("testsuite");
			suite.SetAttributeValue ("name", "Martin");
			suite.SetAttributeValue ("errors", "0");
			suite.SetAttributeValue ("failures", "0");
			suite.SetAttributeValue ("tests", "1");
			root.Add (suite);

			var tcase = new XElement ("testcase");
			tcase.SetAttributeValue ("classname", "martin.test");
			tcase.SetAttributeValue ("name", "Test1");
			tcase.SetAttributeValue ("time", "123.345000");
			suite.Add (tcase);

			var tout = new XElement ("system-out");
			tout.Add (new XText ("Hello World!"));
			tcase.Add (tout);

			// Writer.WriteLine ();
			// Writer.WriteLine ("Test result: {0} - {1}", Result.Name.FullName, Result.Status);
			// Writer.WriteLine ();

			if (Result.Status == TestStatus.Success)
				return true;

			Visit (Result);
			return false;
		}

		string FormatName (TestName name)
		{
			return name.FullName;
		}

		void Visit (TestResult node)
		{
			if (node.HasChildren) {
				foreach (var child in node.Children)
					Visit (child);
				return;
			}

			if (node.Status == TestStatus.Success)
				return;
			else if (node.Status == TestStatus.Ignored && !ShowIgnored)
				return;

			// Writer.WriteLine ("{0}) {1}: {2}", ++current, FormatName (node.Name), node.Status);

			if (node.Status == TestStatus.Error && node.HasErrors) {
				foreach (var error in node.Errors) {
					// Writer.WriteLine ();
					// Writer.WriteLine (error);
				}
			}

			// Writer.WriteLine ();
		}
	}
}

