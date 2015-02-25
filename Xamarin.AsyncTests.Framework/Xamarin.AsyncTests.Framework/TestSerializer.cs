//
// TestSerializer.cs
//
// Author:
//       Martin Baulig <martin.baulig@xamarin.com>
//
// Copyright (c) 2014 Xamarin Inc. (http://www.xamarin.com)
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
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using System.Xml.Linq;

namespace Xamarin.AsyncTests.Framework
{
	using Reflection;

	public static class TestSerializer
	{
		static readonly XName ParameterName = "TestParameter";
		static readonly XName PathName = "TestPath";

		internal const string TestCaseIdentifier = "test";
		internal const string FixtureInstanceIdentifier = "instance";
		internal const string TestFixtureIdentifier = "fixture";
		internal const string TestSuiteIdentifier = "suite";

		internal static void Debug (string message, params object[] args)
		{
			System.Diagnostics.Debug.WriteLine (string.Format (message, args));
		}

		internal static XElement SerializePath (TestPath path)
		{
			var node = new XElement (PathName);

			while (path != null) {
				var element = WritePathNode (path.Host, path.Parameter);
				node.AddFirst (element);
				path = path.Parent;
			}

			return node;
		}

		internal static TestPathNode DeserializePath (TestContext ctx, XElement root)
		{
			if (!root.Name.Equals (PathName))
				throw new InternalErrorException ();

			var resolver = (IPathResolver)ctx.Suite;

			foreach (var element in root.Elements (ParameterName)) {
				var node = ReadPathNode (element);
				var parameterAttr = element.Attribute ("Parameter");
				var parameter = parameterAttr != null ? parameterAttr.Value : null;

				resolver = resolver.Resolve (ctx, node, parameter);
			}

			TestSerializer.Debug ("DESERIALIZE: {0} {1}\n{2}", resolver, resolver.Node, root);
			return resolver.Node;
		}

		public static string GetFriendlyName (Type type)
		{
			var friendlyAttr = type.GetTypeInfo ().GetCustomAttribute<FriendlyNameAttribute> ();
			if (friendlyAttr != null)
				return friendlyAttr.Name;
			return type.FullName;
		}

		public static ITestParameter GetStringParameter (string value)
		{
			return new ParameterWrapper { Value = value };
		}

		class ParameterWrapper : ITestParameter
		{
			public string Value {
				get; set;
			}
		}

		static IPathNode ReadPathNode (XElement element)
		{
			var node = new PathNodeWrapper ();
			node.TypeKey = element.Attribute ("Type").Value;
			node.Identifier = element.Attribute ("Identifier").Value;

			var nameAttr = element.Attribute ("Name");
			if (nameAttr != null)
				node.Name = nameAttr.Value;

			var paramTypeAttr = element.Attribute ("ParameterType");
			if (paramTypeAttr != null)
				node.ParameterType = paramTypeAttr.Value;

			var paramValueAttr = element.Attribute ("Parameter");
			if (paramValueAttr != null)
				node.ParameterValue = paramValueAttr.Value;

			return node;
		}

		static XElement WritePathNode (IPathNode node, ITestParameter parameter)
		{
			var element = new XElement (ParameterName);
			element.Add (new XAttribute ("Type", node.TypeKey));
			element.Add (new XAttribute ("Identifier", node.Identifier));
			if (node.Name != null)
				element.Add (new XAttribute ("Name", node.Name));
			if (node.ParameterType != null)
				element.Add (new XAttribute ("ParameterType", node.ParameterType));
			if (parameter != null)
				element.Add (new XAttribute ("Parameter", parameter.Value));
			return element;
		}

		class PathNodeWrapper : IPathNode
		{
			public string TypeKey {
				get; set;
			}
			public string Identifier {
				get; set;
			}
			public string Name {
				get; set;
			}
			public string ParameterType {
				get; set;
			}
			public string ParameterValue {
				get; set;
			}

			public override string ToString ()
			{
				string parameter = ParameterValue != null ? string.Format (", Parameter={0}", ParameterValue) : string.Empty;
				return string.Format ("[TestPathNode: Type={0}, Identifier={1}, Name={2}{3}]", TypeKey, Identifier, Name, parameter);
			}
		}
	}
}

