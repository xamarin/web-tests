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
				printer.Visit (root, null, result.Path, result, false);
				root.WriteTo (writer);
			}
		}

		static string FormatName (TestName name)
		{
			return name.FullName;
		}

		enum NameFormat
		{
			Full,
			FullWithParameters,
			Parent,
			Local,
			LocalWithParameters,
			Parameters
		}

		static bool IsHidden (ITestPath path)
		{
			if ((path.Flags & TestFlags.Hidden) != 0)
				return true;
			if (false && (path.Flags & TestFlags.PathHidden) != 0)
				return true;
			if (path.PathType == TestPathType.Parameter && ((path.Flags & TestFlags.PathHidden) != 0))
				return true;
			return false;
		}

#if FIXME
		static string FormatName (ITestPath path, NameFormat format)
		{
			var parts = new List<string> ();
			var parameters = new List<string> ();
			FormatName_inner (path);

			var formatted = new StringBuilder ();

			var (start, end, includeParameters) = GetFormatParameters ();

			for (int i = start; i < end; i++) {
				if (i > start)
					formatted.Append (".");
				formatted.Append (parts [i]);
			}

			if (includeParameters && parameters.Count > 0) {
				formatted.Append ("(");
				formatted.Append (string.Join (",", parameters));
				formatted.Append (")");
			}

			return formatted.ToString ();

			void FormatName_inner (ITestPath current)
			{
				if (current.Parent != null)
					FormatName_inner (current.Parent);
				if (current.PathType == TestPathType.Parameter) {
					if (!IsHidden (current))
						parameters.Add (current.ParameterValue);
				} else {
					if (!string.IsNullOrEmpty (current.Name) && ((current.Flags & TestFlags.Hidden) == 0))
						// if (!string.IsNullOrEmpty (current.Name) && !IsHidden (current))
						parts.Add (current.Name);
				}
			}

			(int, int, bool) GetFormatParameters ()
			{
				switch (format) {
				case NameFormat.Full:
					return (0, parts.Count, false);
				case NameFormat.FullWithParameters:
					return (0, parts.Count, true);
				case NameFormat.Local:
					return (parts.Count - 1, parts.Count, false);
				case NameFormat.LocalWithParameters:
					return (parts.Count - 1, parts.Count, true);
				case NameFormat.Parent:
					if (parts.Count > 0)
						return (0, parts.Count - 1, false);
					else
						return (0, 0, false);
				case NameFormat.Parameters:
					return (0, 0, true);
				default:
					throw new InternalErrorException ();
				}
			}
		}
#else
		static string FormatParameters (ITestPath path)
		{
			var parameters = new List<string> ();

			for (; path != null; path = path.Parent) {
				if (path.PathType != TestPathType.Parameter)
					continue;
				if ((path.Flags & TestFlags.Hidden) != 0)
					continue;
				parameters.Add (path.ParameterValue);
			}

			if (parameters.Count == 0)
				return string.Empty;
			return "(" + string.Join (",", parameters) + ")"; 
		}
#endif

		void Visit (XElement root, TestSuite current, ITestPath parent, TestResult result, bool foundParameter)
		{
			XElement node = root;
			if (result.Path.PathType == TestPathType.Parameter)
				foundParameter = true;

			TestSuite suite = current;
			bool needsTest = !result.HasChildren || result.Children.Count == 0;
			bool needsSuite = (parent.PathType != TestPathType.Parameter) && (!IsHidden (result.Path) || (needsTest && current == null));

			if (needsSuite) {
				Debug ("NEW SUITE: {0} - {1} - {2}", parent, result.Path, result.Path.PathType); 
				suite = new TestSuite (root, suite, parent, result);
				suite.Resolve ();
				root.Add (suite.Node);
				node = suite.Node;
				current = suite;
			}

			if (result.HasChildren) {
				foreach (var child in result.Children)
					Visit (node, current, result.Path, child, foundParameter);
			}

			if (needsTest) {
				var test = new TestCase (current, result);
				current.AddTest (test);
			}

			if (suite != null)
				suite.Write ();
		}

		static void Debug (string message, params object[] args)
		{
			System.Diagnostics.Debug.WriteLine (string.Format (message, args));
		}

		class TestSuite
		{
			public XElement Root {
				get; private set;
			}

			public TestSuite Parent {
				get; private set;
			}

			public ITestPath ParentPath {
				get; private set;
			}

			public TestResult Result {
				get; private set;
			}

			public string Name {
				get;
				private set;
			}

			public string LocalName {
				get;
				private set;
			}

			public XElement Node { get; } = new XElement ("testsuite");

			public XElement Properties { get; } = new XElement ("properties");

			public DateTime TimeStamp { get; } = new DateTime (DateTime.Now.Ticks, DateTimeKind.Unspecified);

			StringBuilder output = new StringBuilder ();
			StringBuilder errorOutput = new StringBuilder ();
			List<TestCase> tests = new List<TestCase> ();

			public TestSuite (XElement root, TestSuite parent, ITestPath parentPath, TestResult result)
			{
				Root = root;
				Parent = parent;
				ParentPath = parentPath;
				Result = result;

				var formatted = new StringBuilder ();
				if (parent != null) {
					formatted.Append (parent.Name);
					formatted.Append (".");
				}

				LocalName = result.Path.Name;
				if (!string.IsNullOrEmpty (LocalName))
					formatted.Append (LocalName);

				Name = formatted.ToString ();
			}

			public void Resolve ()
			{
				var serializedPath = Result.Path.SerializePath ().ToString ();
				output.AppendLine (serializedPath);
				output.AppendLine ();

				Debug ("RESOLVE SUITE: {0}\n{1}", Name, serializedPath); 

				WriteParameters ();

				output.AppendLine ();

				if (Result.HasMessages) {
					foreach (var message in Result.Messages) {
						output.AppendLine (message);
					}
				}

				if (Result.HasLogEntries) {
					foreach (var entry in Result.LogEntries) {
						output.AppendFormat (string.Format ("LOG: {0} {1} {2}\n", entry.Kind, entry.LogLevel, entry.Text));
					}
				}
			}

			public void AddTest (TestCase test)
			{
				tests.Add (test);
			}

			public void Write ()
			{
				Node.SetAttributeValue ("name", Name);

				Node.SetAttributeValue ("timestamp", TimeStamp.ToString ("yyyy-MM-dd'T'HH:mm:ss"));
				Node.SetAttributeValue ("hostname", "localhost");
				if (Result.ElapsedTime != null)
					Node.SetAttributeValue ("time", Result.ElapsedTime.Value.TotalSeconds);

				Node.Add (Properties);

				foreach (var test in tests)
					Node.Add (test.Node);
 
				var systemOut = new XElement ("system-out");
				Node.Add (systemOut);

				var systemErr = new XElement ("system-err");
				Node.Add (systemErr);

				WriteOutput (systemOut);

				foreach (var test in tests)
					test.Write ();
			}

			static int nextId;

			void WriteOutput (XElement element)
			{
				element.Add (string.Format ("OUTPUT: {0}\n", ++nextId));
				using (var reader = new StringReader (output.ToString ())) {
					string line;
					while ((line = reader.ReadLine ()) != null) {
						element.Add (line);
						element.Add (Environment.NewLine);
					}
				}
			}

			void WriteParameters ()
			{
				var list = new List<Tuple<string,XElement>> ();
				WriteParameters (list, Result.Path);
				if (list.Count == 0)
					return;

				output.AppendLine ("<parameters>");
				foreach (var entry in list) {
					output.AppendLine (entry.Item1);
					Properties.Add (entry.Item2);
				}
				output.AppendLine ("</parameters>");
				output.AppendLine ();
			}

			void WriteParameters (List<Tuple<string,XElement>> list, ITestPath path)
			{
				if (path.Parent != null)
					WriteParameters (list, path.Parent);

				if (path.PathType != TestPathType.Parameter)
					return;
				if ((path.Flags & TestFlags.Hidden) != 0)
					return;

				var line = string.Format ("  {0} = {1}", path.Name, path.ParameterValue);
				var element = new XElement ("property");
				element.SetAttributeValue ("name", path.Name);
				element.SetAttributeValue ("value", path.ParameterValue);

				list.Add (new Tuple<string,XElement> (line, element));
			}
		}

		class TestCase
		{
			public TestSuite Parent {
				get; private set;
			}

			public TestResult Result {
				get; private set;
			}

			public XElement Node { get; } = new XElement ("testcase");

			public TestCase (TestSuite parent, TestResult result)
			{
				Parent = parent;
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
				// var newName = FormatName (Result.Path, NameFormat.LocalWithParameters);
				// var argumentList = FormatName (Result.Path, NameFormat.Parameters);
				var argumentList = FormatParameters (Result.Path);
				var reallyNewName = Parent.LocalName + argumentList;

				Debug ("TEST CASE: {0} - {1} - {2} => {3}", Parent.Result.Path, Parent.LocalName, argumentList, reallyNewName); 

				Node.SetAttributeValue ("name", reallyNewName);
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

