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
				var root = new RootElement (printer, result);
				root.Visit ();
				root.Node.WriteTo (writer);
			}
		}

		static void Debug (string message, params object [] args)
		{
			System.Diagnostics.Debug.WriteLine (string.Format (message, args));
		}

		abstract class Element
		{
			public JUnitResultPrinter Printer {
				get;
			}

			public Element Parent {
				get;
			}

			public XElement Node {
				get;
			}

			public TestPath Path {
				get;
			}

			public abstract string Name {
				get;
			}

			public Element (JUnitResultPrinter printer, Element parent, XElement node, TestPath path)
			{
				Printer = printer;
				Parent = parent;
				Node = node;
				Path = path;
			}

			public void Visit ()
			{
				Resolve ();

				Write ();
			}

			protected abstract void Resolve ();

			protected abstract void Write ();

			public override string ToString ()
			{
				return string.Format ("[{0}: Path={1}, Name={2}]", GetType ().Name, Path, Name);
			}
		}

		abstract class ContainerElement : Element
		{
			public TestResult Result {
				get;
			}

			List<Element> children = new List<Element> ();

			public ContainerElement (JUnitResultPrinter printer, Element parent, XElement node, TestResult result)
				: base (printer, parent, node, result.Path)
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

		sealed class RootElement : ContainerElement
		{
			public override string Name {
				get;
			}

			public RootElement (JUnitResultPrinter printer, TestResult result)
				: base (printer, null, new XElement ("testsuites"), result)
			{
				Name = result.Path.Node.Name;
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

		sealed class SuiteElement : ContainerElement
		{
			public override string Name {
				get;
			}

			public DateTime TimeStamp { get; } = new DateTime (DateTime.Now.Ticks, DateTimeKind.Unspecified);

			public SuiteElement (Element parent, TestPath path, TestResult result)
				: base (parent.Printer, parent, new XElement ("testsuite"), result)
			{
				Name = path.Name;
			}

			protected override void ResolveChildren (TestResult result)
			{
				if (!result.HasChildren || result.Children.Count == 0) {
					AddChild (new CaseElement (this, result));
					return;
				}

				foreach (var child in result.Children) {
					switch (child.Path.Node.PathType) {
					case TestPathType.Assembly:
					case TestPathType.Suite:
					case TestPathType.Fixture:
					case TestPathType.Instance:
						if (child.Path.Node.IsHidden)
							goto default;
						AddChild (new SuiteElement (this, child.Path, child));
						break;
					default:
						ResolveChildren (child);
						break;
					}
				}
			}

			protected override void Write ()
			{
				Node.SetAttributeValue ("name", Name);

				Node.SetAttributeValue ("timestamp", TimeStamp.ToString ("yyyy-MM-dd'T'HH:mm:ss"));
				Node.SetAttributeValue ("hostname", "localhost");
				if (Result.ElapsedTime != null)
					Node.SetAttributeValue ("time", Result.ElapsedTime.Value.TotalSeconds);
			}
		}

		sealed class CaseElement : Element
		{
			public override string Name {
				get;
			}

			public TestResult Result {
				get;
			}

			public XElement Properties { get; } = new XElement ("properties");

			StringBuilder output = new StringBuilder ();
			StringBuilder errorOutput = new StringBuilder ();

			public CaseElement (SuiteElement parent, TestResult result)
				: base (parent.Printer, parent, new XElement ("testcase"), result.Path)
			{
				Result = result;

				var parts = new List<string> ();
				for (var path = Path; !TestPath.Equals (path, Parent.Path); path = path.Parent) {
					var current = path.Node;
					if (current.PathType != TestPathType.Parameter && !current.IsHidden && !string.IsNullOrEmpty (current.Name))
						parts.Add (current.Name);
				}

				if (parts.Count == 0)
					parts.Add (Path.LocalName);
				parts.Reverse ();

				Name = string.Join (".", parts) + Path.ArgumentList;
			}

			bool hasError;

			protected override void Resolve ()
			{
				var serializedPath = Result.Path.SerializePath ().ToString ();
				output.AppendLine (serializedPath);
				output.AppendLine ();

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

			void WriteParameters ()
			{
				var list = new List<Tuple<string, XElement>> ();
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

			void WriteParameters (List<Tuple<string, XElement>> list, TestPath path)
			{
				if (path.Parent != null)
					WriteParameters (list, path.Parent);

				var node = path.Node;
				if (node.IsHidden || node.PathType != TestPathType.Parameter)
					return;

				var line = string.Format ("  {0} = {1}", node.Name, node.ParameterValue);
				var element = new XElement ("property");
				element.SetAttributeValue ("name", node.Name);
				element.SetAttributeValue ("value", node.ParameterValue);

				list.Add (new Tuple<string, XElement> (line, element));
			}

			protected override void Write ()
			{
				CreateTestCase ();
				AddErrors ();
				AddStatus ();

				Node.Add (Properties);

				var systemOut = new XElement ("system-out");
				Node.Add (systemOut);

				var systemErr = new XElement ("system-err");
				Node.Add (systemErr);

				WriteOutput (systemOut);
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

			void AddStatus ()
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

