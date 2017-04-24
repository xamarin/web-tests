//
// TestPath.cs
//
// Author:
//       Martin Baulig <martin.baulig@xamarin.com>
//
// Copyright (c) 2015 Xamarin, Inc.
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
using System.Text;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;
using System.Collections.Generic;

namespace Xamarin.AsyncTests
{
	public sealed class TestPath
	{
		public TestPath Parent {
			get;
		}

		public TestNode Node {
			get;
		}

		public TestPath (TestPath parent, TestNode node)
		{
			Parent = parent;
			Node = node;

			var nodes = new List<TestNode> ();
			var parts = new List<string> ();
			var arguments = new List<string> ();
			var friendlyArguments = new List<string> ();
			var parameters = new List<string> ();

			for (var path = this; path != null; path = path.Parent) {
				var current = path.Node;
				nodes.Add (current);
				if (current.PathType == TestPathType.Parameter) {
					if (!current.IsHidden) {
						parameters.Add (current.Identifier);
						friendlyArguments.Add (current.ParameterValue); 
						arguments.Add (current.ParameterValue);
					}
				} else if (!current.IsHidden && !string.IsNullOrEmpty (current.Name))
					parts.Add (current.Name);
			}

			LocalName = parts.First ();

			parts.Reverse ();
			nodes.Reverse ();

			parameters.Reverse ();
			arguments.Reverse ();

			Nodes = nodes.ToArray ();

			Name = string.Join (".", parts);
			if (parameters.Count > 0) {
				ParameterList = "(" + string.Join (",", parameters) + ")";
				ArgumentList = "(" + string.Join (",", arguments) + ")";
				FriendlyArgumentList = "(" + string.Join (",", friendlyArguments) + ")";
			} else {
				ParameterList = ArgumentList = FriendlyArgumentList = string.Empty;
			}

			FullName = Name + ArgumentList;
			FullFriendlyName = Name + FriendlyArgumentList;
		}

		public XElement SerializePath (bool debug = true)
		{
			return Write (this, debug);
		}

		static readonly XName ElementName = "TestPath";

		public static XElement Write (TestPath path, bool debug)
		{
			var node = new XElement (ElementName);
			if (debug) {
				node.SetAttributeValue ("ID", path.ID.ToString ());

				if (path != null && path.Parent != null)
					node.SetAttributeValue ("Parent", path.Parent.ID.ToString ());
			}

			while (path != null) {
				var element = TestNode.WriteNode (path.Node);
				node.AddFirst (element);
				path = path.Parent;
			}

			return node;
		}

		public static TestPath Read (XElement root)
		{
			if (!root.Name.Equals (ElementName))
				throw new InvalidOperationException ();

			TestPath current = null;
			foreach (var node in TestNode.ReadAllNodes (root)) {
				current = new TestPath (current, node);
			}
			return current;
		}

		public TestPath Parameterize (ITestParameter parameter)
		{
			var parameterized = Node.Parameterize (parameter);
			return new TestPath (Parent, parameterized);
		}

		internal TestPath Clone ()
		{
			var clonedNode = Node.Clone ();
			return new TestPath (Parent, clonedNode);
		}

		public IReadOnlyList<TestNode> Nodes {
			get;
		}

		public string Name {
			get;
		}

		public string FullName {
			get;
		}

		public string FullFriendlyName {
			get;
		}

		public string LocalName {
			get;
		}

		public string ParameterList {
			get;
		}

		public string ArgumentList {
			get;
		}

		public string FriendlyArgumentList {
			get;
		}

		internal static bool TestEquals (TestPath first, TestPath second, TestContext ctx = null)
		{
			var serializedFirst = first.SerializePath (false).ToString ();
			var serializedSecond = second.SerializePath (false).ToString ();
			if (string.Equals (serializedFirst, serializedSecond))
				return true;

			var message = string.Format ("NOT EQUAL:\n{0}\n{1}\n\n", serializedFirst, serializedSecond);
			if (ctx != null)
				ctx.LogMessage (message);
			else
				System.Diagnostics.Debug.WriteLine (message); 
			return false;
		}

		public static bool Equals (TestPath first, TestPath second)
		{
			while (true) {
				if (first == null || second == null)
					return first == null && second == null;
				if (!first.Node.Matches (second.Node))
					return false;
				first = first.Parent;
				second = second.Parent;
			}
		}

		public static string GetFriendlyName (Type type)
		{
			if (type == null)
				return null;
			var friendlyAttr = type.GetTypeInfo ().GetCustomAttribute<FriendlyNameAttribute> ();
			if (friendlyAttr != null)
				return friendlyAttr.Name;
			return type.Name;
		}

		public readonly int ID = ++next_id;
		static int next_id;

		public override string ToString ()
		{
			string parameter = Node.IsParameterized ? string.Format (", Parameter={0}", Node.ParameterValue ?? "<null>") : string.Empty;
			var parent = Parent != null ? string.Format (", Parent={0}", Parent.ID) : string.Empty;
			return string.Format ("[TestPath({5}): ID={0}, Identifier={1}, Name={2}{3}{4}]", ID, Node.Identifier, Node.Name, parameter, parent, Node.PathType);
		}
	}
}

