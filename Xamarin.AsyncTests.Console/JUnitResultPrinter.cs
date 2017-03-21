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
using System.Collections.Generic;
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
				printer.Visit (root, result, false);
				root.WriteTo (writer);
			}
		}

		static string FormatName (TestName name)
		{
			return name.FullName;
		}

		static void FormatName_inner (ITestPath path, List<string> parts, List<string> parameters)
		{
			if (path.Parent != null)
				FormatName_inner (path.Parent, parts, parameters);
			if (path.PathType == TestPathType.Parameter) {
				if ((path.Flags & TestFlags.PathHidden) == 0)
					parameters.Add (path.ParameterValue);
			} else {
				if (!string.IsNullOrEmpty (path.Name) && ((path.Flags & TestFlags.Hidden) == 0))
					parts.Add (path.Name);
			}
		}

		static string FormatName (ITestPath path, bool fullName, bool includeParameters)
		{
			var parts = new List<string> ();
			var parameters = new List<string> ();
			FormatName_inner (path, parts, parameters);

			string formatted;
			if (fullName)
				formatted = string.Join (".", parts);
			else
				formatted = parts [parts.Count - 1];

			if (includeParameters && parameters.Count > 0) {
				var joinedParams = string.Join (",", parameters);
				formatted = formatted + "(" + joinedParams + ")";
			}

			return formatted;
		}

		void Visit (XElement root, TestResult result, bool foundParameter)
		{
			if (false && result.Status == TestStatus.Ignored)
				return;

			XElement node = root;
			if (result.Path.PathType == TestPathType.Parameter)
				foundParameter = true;
			if (foundParameter) {
				var suite = new TestSuite (root, result);
				suite.Write ();
				root.Add (suite.Node);
				node = suite.Node;
			}

			if (result.HasChildren) {
				foreach (var child in result.Children)
					Visit (node, child, foundParameter);
			}
		}

		class TestSuite
		{
			public XElement Root {
				get; private set;
			}

			public TestResult Result {
				get; private set;
			}

			public XElement Node { get; } = new XElement ("testsuite");

			public DateTime TimeStamp { get; } = new DateTime (DateTime.Now.Ticks, DateTimeKind.Unspecified);

			public TestSuite (XElement root, TestResult result)
			{
				Root = root;
				Result = result;
			}

			public void Write ()
			{
				Node.SetAttributeValue ("name", FormatName (Result.Path, true, false));

				Node.SetAttributeValue ("timestamp", TimeStamp.ToString ("yyyy-MM-dd'T'HH:mm:ss"));
				Node.SetAttributeValue ("hostname", "localhost");
				if (Result.ElapsedTime != null)
					Node.SetAttributeValue ("time", Result.ElapsedTime.Value.TotalSeconds);

				var properties = new XElement ("properties");
				Node.Add (properties);

				if (!Result.HasChildren || Result.Children.Count == 0) {
					var test = new TestCase (Result);
					test.Write ();
					Node.Add (test.Node);
				}

				var systemOut = new XElement ("system-out");
				Node.Add (systemOut);

				var systemErr = new XElement ("system-err");
				Node.Add (systemErr);

				var serializedPath = Result.Path.SerializePath ().ToString ();
				systemOut.Add (serializedPath);
				systemOut.Add (Environment.NewLine);
				systemOut.Add (FormatName (Result.Path, true, true));
				systemOut.Add (Environment.NewLine);
				systemOut.Add (Environment.NewLine);

				WriteParameters (properties, systemOut);

				systemOut.Add (Environment.NewLine);

				if (Result.HasMessages) {
					foreach (var message in Result.Messages) {
						systemOut.Add (message + Environment.NewLine);
					}
				}

				if (Result.HasLogEntries) {
					foreach (var entry in Result.LogEntries) {
						systemOut.Add (string.Format ("LOG: {0} {1} {2}\n", entry.Kind, entry.LogLevel, entry.Text));
					}
				}
			}

			void WriteParameters (XElement properties, XElement output)
			{
				var list = new List<Tuple<string,XElement>> ();
				WriteParameters (list, Result.Path);
				if (list.Count == 0)
					return;

				output.Add ("<parameters>" + Environment.NewLine);
				foreach (var entry in list) {
					output.Add (entry.Item1);
					properties.Add (entry.Item2);
				}
				output.Add ("</parameters>" + Environment.NewLine);
				output.Add (Environment.NewLine);
			}

			void WriteParameters (List<Tuple<string,XElement>> list, ITestPath path)
			{
				if (path.Parent != null)
					WriteParameters (list, path.Parent);

				if (path.PathType != TestPathType.Parameter)
					return;
				if ((path.Flags & TestFlags.Hidden) != 0)
					return;

				var output = string.Format ("  {0} = {1}{2}", path.Name, path.ParameterValue, Environment.NewLine);
				var element = new XElement ("property");
				element.SetAttributeValue ("name", path.Name);
				element.SetAttributeValue ("value", path.ParameterValue);

				list.Add (new Tuple<string,XElement> (output, element));
			}
		}

		class TestCase
		{
			public TestResult Result {
				get; private set;
			}

			public XElement Node { get; } = new XElement ("testcase");

			public TestCase (TestResult result)
			{
				Result = result;
			}

			bool hasError;

			public void Write ()
			{
				CreateTestCase ();
				AddErrors ();
				Finish ();
			}

			void CreateTestCase ()
			{
				Node.SetAttributeValue ("name", FormatName (Result.Path, false, true));
				Node.SetAttributeValue ("status", Result.Status);
				if (Result.ElapsedTime != null)
					Node.SetAttributeValue ("time", Result.ElapsedTime.Value.TotalSeconds);
			}

			void AddErrors ()
			{
				if (!Result.HasErrors)
					return;

				foreach (var error in Result.Errors) {
					var xerror = new XElement ("error");
					var savedException = error as SavedException;
					if (savedException != null) {
						xerror.SetAttributeValue ("type", savedException.Type);
						xerror.SetAttributeValue ("message", savedException.Message + "\n" + savedException.StackTrace);
					} else {
						xerror.SetAttributeValue ("type", error.GetType ().FullName);
						xerror.SetAttributeValue ("message", error.Message + "\n" + error.StackTrace);
					}
					Node.Add (xerror);
					hasError = true;
				}
			}

			void Finish ()
			{
				switch (Result.Status) {
				case TestStatus.Error:
				case TestStatus.Canceled:
					if (!hasError)
						Node.Add (new XElement ("error"));
					break;
				case TestStatus.Ignored:
					Node.Add (new XElement ("skipped"));
					break;
				}
			}
		}
	}
}

