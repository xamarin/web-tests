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

	static class TestSerializer
	{
		static readonly XName InstanceName = "TestInstance";
		static readonly XName BuilderName = "TestBuilder";
		static readonly XName ParameterName = "TestParameter";
		static readonly XName PathName = "TestPath";

		internal const string TestCaseIdentifier = "test";
		internal const string FixtureInstanceIdentifier = "instance";
		internal const string TestFixtureIdentifier = "fixture";
		internal const string TestSuiteIdentifier = "suite";

		public static XElement Serialize (TestInstance instance)
		{
			var path = TestPath.CreateFromInstance (instance);

			var name = TestPath.GetTestName (path);

			var root = new XElement (InstanceName);
			root.Add (new XAttribute ("Name", name.FullName));

			var pathElement = SerializePath (path);
			root.AddFirst (pathElement);

			while (path != null) {
				var element = SerializeBuilder (ref path);
				root.AddFirst (element);
			}

			return root;
		}

		internal static void Debug (string message, params object[] args)
		{
			System.Diagnostics.Debug.WriteLine (string.Format (message, args));
		}

		static XElement SerializeBuilder (ref TestPath path)
		{
			var parameterPath = new LinkedList<TestPath> ();
			TestBuilder builder = null;

			while (path != null && builder == null) {
				parameterPath.AddLast (path);

				builder = path.BrokenBuilder;
				path = path.Parent;
			}

			if (builder == null)
				throw new InternalErrorException ();

			var node = new XElement (BuilderName);
			node.Add (new XAttribute ("Type", builder.GetType ().Name));
			node.Add (new XAttribute ("Name", builder.FullName));

			var pathIter = parameterPath.Last.Previous;

			while (pathIter != null) {
				var element = WritePathNode (pathIter.Value.Host, pathIter.Value.Parameter);
				node.Add (element);
				pathIter = pathIter.Previous;
			}

			return node;
		}

		static XElement SerializePath (TestPath path)
		{
			var node = new XElement (PathName);

			while (path != null) {
				var element = WritePathNode (path.Host, path.Parameter);
				node.AddFirst (element);
				path = path.Parent;
			}

			return node;
		}

		public static TestInvoker Deserialize (TestSuite suite, XElement root)
		{
			if (!root.Name.Equals (InstanceName))
				throw new InternalErrorException ();

			var resolvable = suite as IPathResolvable;
			if (resolvable != null) {
				var pathElement = root.Element (PathName);
				DeserializePath (suite, resolvable, root, pathElement);
			}

			var name = root.Attribute ("Name").Value;

			var elements = new LinkedList<XElement> (root.Elements (BuilderName));
			var builders = new LinkedList<TestBuilder> ();

			var reflectionSuite = (ReflectionTestSuite)suite;
			TestBuilder builder = reflectionSuite.Builder;
			builders.AddLast (builder);

			var elementIter = elements.First.Next;
			while (elementIter != null) {
				var node = elementIter.Value;
				elementIter = elementIter.Next;

				var builderName = node.Attribute ("Name").Value;
				builder = builder.FindChild (builderName);
				if (builder == null)
					throw new InternalErrorException ();

				builders.AddLast (builder);
			}

			TestInvoker invoker = null;
			elementIter = elements.Last;
			var builderIter = builders.Last;

			while (builderIter != null) {
				builder = builderIter.Value;
				builderIter = builderIter.Previous;

				var node = elementIter.Value;
				elementIter = elementIter.Previous;

				if (!DeserializeBuilder (builder, node, ref invoker))
					throw new InternalErrorException ();
			}

			return invoker;
		}

		static void DeserializePath (TestSuite suite, IPathResolvable resolvable, XElement root, XElement path)
		{
			Debug ("DESERIALIZE: {0}", root);

			foreach (var element in path.Elements (ParameterName)) {
				Debug ("DESERIALIZE PATH: {0} {1}", resolvable, element);

				var node = ReadPathNode (element);
				var parameterAttr = element.Attribute ("Parameter");
				var parameter = parameterAttr != null ? parameterAttr.Value : null;

				var resolver = resolvable.GetResolver ();
				resolvable = resolver.Resolve (node, parameter);
			}
		}

		static bool DeserializeBuilder (TestBuilder builder, XElement node, ref TestInvoker invoker)
		{
			var elements = new LinkedList<XElement> (node.Elements (ParameterName));
			var elementIter = elements.First;
			var hostIter = builder.ParameterHosts.First;

			var current = (TestBuilderHost)builder.Host;
			if (invoker == null)
				invoker = current.CreateInnerInvoker ();

			var parameters = new LinkedList<KeyValuePair<TestHost,XElement>> ();

			while (hostIter != null) {
				var host = hostIter.Value;
				hostIter = hostIter.Next;

				XElement param = null;
				if (elementIter != null) {
					param = elementIter.Value;
					elementIter = elementIter.Next;

					var paramType = param.Attribute ("Type").Value;
					if (!host.TypeKey.Equals (paramType))
						throw new InternalErrorException ();
				}

				parameters.AddLast (new KeyValuePair<TestHost,XElement> (host, param));
			}

			var paramIter = parameters.Last;
			while (paramIter != null) {
				var host = paramIter.Value.Key;
				var param = paramIter.Value.Value;
				paramIter = paramIter.Previous;

				if (param != null)
					invoker = host.Deserialize (param, invoker);
				else
					invoker = host.CreateInvoker (invoker);
				if (invoker == null)
					throw new InternalErrorException ();
			}

			invoker = current.Deserialize (node, invoker);

			return true;
		}

		internal static string GetFriendlyName (Type type)
		{
			var friendlyAttr = type.GetTypeInfo ().GetCustomAttribute<FriendlyNameAttribute> ();
			if (friendlyAttr != null)
				return friendlyAttr.Name;
			return type.FullName;
		}

		internal static ITestParameter GetStringParameter (string value)
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
			var node = new TestPathNode ();
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

		class TestPathNode : IPathNode
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
		}
	}
}

