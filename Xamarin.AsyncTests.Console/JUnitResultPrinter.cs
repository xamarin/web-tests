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
				var root = new RootElement (result);
				root.Visit ();
				// printer.Visit (root, null, result.Path, result, false);
				root.Node.WriteTo (writer);
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

		static bool IsHidden (ITestPath path, bool pathHidden)
		{
			if ((path.Flags & TestFlags.Hidden) != 0)
				return true;
			if (pathHidden && (path.Flags & TestFlags.PathHidden) != 0)
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

#if FIXME
		void Visit (Element element, ITestPath parent, TestResult result, bool foundParameter)
		{
			XElement node = root;
			if (result.Path.PathType == TestPathType.Parameter)
				foundParameter = true;

			SuiteElement suite = current;
			bool needsTest = !result.HasChildren || result.Children.Count == 0;
			bool needsSuite = (result.Path.PathType != TestPathType.Parameter) && (!IsHidden (result.Path) || (needsTest && current == null));

			if (needsSuite) {
				Debug ("NEW SUITE: {0} - {1} - {2}", parent, result.Path, result.Path.PathType); 
				suite = new SuiteElement (root, suite, parent, result);
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
#endif

		static void Debug (string message, params object[] args)
		{
			System.Diagnostics.Debug.WriteLine (string.Format (message, args));
		}

		abstract class Element
		{
			public Element Parent {
				get;
				private set;
			}

			public XElement Node {
				get;
				private set;
			}

			public ITestPath Path {
				get;
				private set;
			}

			public abstract string Name {
				get;
			}

			public abstract string LocalName {
				get;
			}

			public Element (Element parent, XElement node, ITestPath path)
			{
				Parent = parent;
				Node = node;
				Path = path;
			}

			public void Visit ()
			{
				Debug ("VISIT ELEMENT: {0}", this); 

				Resolve ();

				Write (); 
			}

			protected abstract void Resolve ();

			protected abstract void Write ();

			public override string ToString ()
			{
				return string.Format ("[{0}: Path={1}, Name={2}, LocalName={3}]", GetType ().Name, Path, Name, LocalName);
			}
		}

		abstract class ContainerElement : Element
		{
			public TestResult Result {
				get;
				private set;
			}

			List<Element> children = new List<Element> ();

			public ContainerElement (Element parent, XElement node, TestResult result)
				: base (parent, node, result.Path)
			{
				Result = result;
			}

			protected override void Resolve ()
			{
				ResolveChildren (Result); 

				foreach (var child in children) {
					child.Visit ();
					Node.Add (child.Node); 
				}
			}

			protected void AddChild (Element child)
			{
				children.Add (child); 
			}

			protected abstract void ResolveChildren (TestResult result);
		}

		class RootElement : ContainerElement
		{
			public override string Name {
				get {
					return name;
				}
			}

			public override string LocalName {
				get {
					return localName;
				}
			}

			readonly string name;
			readonly string localName;

			public RootElement (TestResult result)
				: base (null, new XElement ("testsuites"), result)
			{
				name = localName = Path.Name;
			}

			protected override void ResolveChildren (TestResult result)
			{
				if (result.HasChildren) {
					foreach (var childResult in result.Children) {
						var suite = new SuiteElement (this, childResult.Path, childResult);
						AddChild (suite); 
					}
				}
			}

			protected override void Write ()
			{
			}
		}

		class SuiteElement : ContainerElement
		{
			public override string Name {
				get { return name; }
			}

			public override string LocalName {
				get { return localName; }
			}

			public XElement Properties { get; } = new XElement ("properties");

			public DateTime TimeStamp { get; } = new DateTime (DateTime.Now.Ticks, DateTimeKind.Unspecified);

			readonly string name;
			readonly string localName;

			StringBuilder output = new StringBuilder ();
			StringBuilder errorOutput = new StringBuilder ();
			List<CaseElement> tests = new List<CaseElement> ();

			public SuiteElement (Element parent, ITestPath path, TestResult result)
				: base (parent, new XElement ("testsuite"), result)
			{
				var formatted = new StringBuilder ();
				if (parent.Name != null) {
					formatted.Append (parent.Name);
					formatted.Append (".");
				}

				localName = path.Name;
				if (!string.IsNullOrEmpty (localName))
					formatted.Append (localName);

				name = formatted.ToString ();
			}

			protected override void Resolve ()
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

				base.Resolve (); 
			}

			protected override void ResolveChildren (TestResult result)
			{
				if (!result.HasChildren || result.Children.Count == 0) {
					AddChild (new CaseElement (this, result));
					return;
				}

				foreach (var child in result.Children) {
					if ((child.Path.PathType != TestPathType.Parameter) && !IsHidden (child.Path, false))
						AddChild (new SuiteElement (this, child.Path, child));
					else
						ResolveChildren (child);
				}
			}

			public void AddTest (CaseElement test)
			{
				tests.Add (test);
			}

			protected override void Write ()
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

		class CaseElement : Element
		{
			public override string Name {
				get {
					return name;
				}
			}

			public override string LocalName {
				get {
					return localName;
				}
			}

			public TestResult Result {
				get; private set;
			}

			readonly string name;
			readonly string localName;

			public CaseElement (SuiteElement parent, TestResult result)
				: base (parent, new XElement ("testcase"), result.Path)
			{
				Result = result;

				var argumentList = FormatParameters (Result.Path);
				var reallyNewName = Parent.LocalName + argumentList;

				name = Parent.LocalName + argumentList;
				localName = Parent.LocalName + argumentList;
			}

			bool hasError;

			protected override void Resolve ()
			{
			}

			protected override void Write ()
			{
				CreateTestCase ();
				AddErrors ();
				Finish ();
			}

			void CreateTestCase ()
			{
				Node.SetAttributeValue ("name", Name);
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

