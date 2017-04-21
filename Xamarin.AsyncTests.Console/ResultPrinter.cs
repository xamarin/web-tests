//
// ResultPrinter.cs
//
// Author:
//       Martin Baulig <martin.baulig@xamarin.com>
//
// Copyright (c) 2016 Xamarin Inc. (http://www.xamarin.com)

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
using System.Xml.Linq;
using Xamarin.AsyncTests.Framework;

namespace Xamarin.AsyncTests.Console
{
	public class ResultPrinter
	{
		public TestResult Result {
			get;
			private set;
		}

		public TextWriter Writer {
			get;
			private set;
		}

		public bool ShowIgnored {
			get; set;
		}

		int current;

		public ResultPrinter (TextWriter writer, TestResult result)
		{
			Writer = writer;
			Result = result;
		}

		public static ResultPrinter Load (TextWriter writer, string filename)
		{
			var doc = XDocument.Load (filename);
			var result = TestSerializer.ReadTestResult (doc.Root);
			return new ResultPrinter (writer, result);
		}

		public bool Print ()
		{
			Writer.WriteLine ();
			Writer.WriteLine ("Test result: {0} - {1}", Result.Path.FullName, Result.Status);
			Writer.WriteLine ();

			if (Result.Status == TestStatus.Success)
				return true;

			Visit (Result);
			return false;
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

			Writer.WriteLine ("{0}) {1}: {2}", ++current, node.Path.FullName, node.Status);

			if (node.HasErrors && (node.Status == TestStatus.Error || node.Status == TestStatus.Unstable)) {
				foreach (var error in node.Errors) {
					Writer.WriteLine ();
					Writer.WriteLine (error);
				}
			}

			Writer.WriteLine ();
		}
	}
}

