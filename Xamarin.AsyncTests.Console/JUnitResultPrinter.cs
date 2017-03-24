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
				root.Node.WriteTo (writer);
			}
		}

		static string FormatArguments (ITestPath path)
		{
			var arguments = new List<string> ();

			for (; path != null; path = path.Parent) {
				if (path.PathType != TestPathType.Parameter)
					continue;
				if ((path.Flags & TestFlags.Hidden) != 0)
					continue;
				arguments.Add (path.ParameterValue);
			}

			if (arguments.Count == 0)
				return string.Empty;
			return "(" + string.Join (",", arguments) + ")";
		}

		static string FormatParameters (ITestPath path)
		{
			var parameters = new List<string> ();

			for (; path != null; path = path.Parent) {
				if (path.PathType != TestPathType.Parameter)
					continue;
				if ((path.Flags & TestFlags.Hidden) != 0)
					continue;
				parameters.Add (path.Identifier);
			}

			if (parameters.Count == 0)
				return string.Empty;
			return "(" + string.Join (",", parameters) + ")";
		}

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

			public SuiteElement (Element parent, ITestPath path, TestResult result)
				: base (parent, new XElement ("testsuite"), result)
			{
				var formatted = new StringBuilder ();
				if (parent.Name != null) {
					formatted.Append (parent.Name);
					formatted.Append (".");
				}

				localName = path.Name;
				if (!string.IsNullOrEmpty (localName)) {
					formatted.Append (localName);
					var parameters = FormatParameters (path);
					formatted.Append (parameters); 
				}

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
				Debug ("RESOLVE CHILDREN: {0} - {1} - {2}\n{3}", this, result.Path, result.HasChildren,
				      result.Path.SerializePath ());

				if (!result.HasChildren || result.Children.Count == 0) {
						AddChild (new CaseElement (this, result));
						return;
					}

				foreach (var child in result.Children) {
					Debug ("  RESOLVE CHILD: {0} {1}\n{2}", child, child.Path, child.Path.SerializePath ());
					if ((child.Path.PathType != TestPathType.Parameter) && ((child.Path.Flags & (TestFlags.Hidden | TestFlags.PathHidden)) == 0))
						AddChild (new SuiteElement (this, child.Path, child));
					else
						ResolveChildren (child);
				}
			}

			protected override void Write ()
			{
				Node.SetAttributeValue ("name", Name);

				Node.SetAttributeValue ("timestamp", TimeStamp.ToString ("yyyy-MM-dd'T'HH:mm:ss"));
				Node.SetAttributeValue ("hostname", "localhost");
				if (Result.ElapsedTime != null)
					Node.SetAttributeValue ("time", Result.ElapsedTime.Value.TotalSeconds);

				Node.Add (Properties);

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

				var argumentList = FormatArguments (Result.Path);
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

