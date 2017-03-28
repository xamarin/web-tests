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
using System.Xml.Linq;
using System.Collections.Generic;

namespace Xamarin.AsyncTests
{
	public abstract class TestPath : PathNode
	{
		public abstract TestPath Parent {
			get;
		}

		public XElement SerializePath (bool debug = true)
		{
			return Write (this, debug);
		}

		public bool IsParameterized {
			get { return ParameterType != null; }
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
				var element = PathNode.WriteNode (path);
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
			foreach (var node in PathNode.ReadAllNodes (root)) {
				current = new PathWrapper (current, node);
			}
			return current;
		}

		public IReadOnlyList<PathNode> GetNodes ()
		{
			return GetNodes (this);
		}

		static IReadOnlyList<PathNode> GetNodes (TestPath path)
		{
			var list = new List<PathNode> ();
			while (path != null) {
				list.Add (path); 
				path = path.Parent;
			}
			list.Reverse ();
			return list;
		}

		public static string GetFullName (TestPath path)
		{
			var formatted = new StringBuilder ();

			GetFullName (path, formatted);
			formatted.Append (FormatArguments (path)); 
			return formatted.ToString ();
		}

		static void GetFullName (TestPath path, StringBuilder formatted)
		{
			if (path.Parent != null)
				GetFullName (path.Parent, formatted);

			if (path.PathType == TestPathType.Parameter)
				return;
			if ((path.Flags & TestFlags.Hidden) != 0)
				return;
			if (string.IsNullOrEmpty (path.Name))
				return;

			if (formatted.Length > 0)
				formatted.Append (".");
			formatted.Append (path.Name);
		}

		public static string FormatArguments (TestPath path)
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

		public TestName GetName ()
		{
			return GetName (this);
		}

		static TestName GetName (TestPath path)
		{
			var builder = new TestNameBuilder ();

			for (; path != null; path = path.Parent) {
				if (path.PathType == TestPathType.Parameter) {
					builder.PushParameter (path.Name, path.ParameterValue, (path.Flags & TestFlags.Hidden) != 0);
					continue;
				}
				if (string.IsNullOrEmpty (path.Name))
					continue;
				if ((path.Flags & TestFlags.Hidden) != 0)
						continue;
				builder.PushName (path.Name); 
			}

			return builder.GetName ();
		}

		public readonly int ID = ++next_id;
		static int next_id;

		public override string ToString ()
		{
			string parameter = IsParameterized ? string.Format (", Parameter={0}", ParameterValue ?? "<null>") : string.Empty;
			var parent = Parent != null ? string.Format (", Parent={0}", Parent.ID) : string.Empty;
			return string.Format ("[TestPath({5}): ID={0}, Identifier={1}, Name={2}{3}{4}]", ID, Identifier, Name, parameter, parent, PathType);
		}

		class PathWrapper : TestPath
		{
			readonly TestPath parent;
			readonly PathNode node;

			public PathNode Node {
				get { return node; }
			}

			public PathWrapper (TestPath parent, PathNode node)
			{
				this.parent = parent;
				this.node = node;
			}

			public override TestPath Parent {
				get { return parent; }
			}

			public override TestPathType PathType {
				get { return node.PathType; }
			}

			public override TestFlags Flags {
				get { return node.Flags; }
			}

			public override string Identifier {
				get { return node.Identifier; }
			}

			public override string Name {
				get { return node.Name; }
			}

			public override string ParameterType {
				get { return node.ParameterType; }
			}

			public override string ParameterValue {
				get { return node.ParameterValue; }
			}
		}
	}
}

