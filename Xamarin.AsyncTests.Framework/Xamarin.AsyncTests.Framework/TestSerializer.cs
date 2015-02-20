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

		public static XElement Serialize (TestInstance instance)
		{
			var path = TestPath.CreateFromInstance (instance);

			var name = TestPath.GetTestName (path);

			var root = new XElement (InstanceName);
			root.Add (new XAttribute ("Name", name.FullName));

			while (path != null) {
				var element = SerializeBuilder (ref path);
				root.AddFirst (element);
			}

			return root;
		}

		static void Debug (string message, params object[] args)
		{
			System.Diagnostics.Debug.WriteLine (string.Format (message, args));
		}

		static XElement SerializeBuilder (ref TestPath path)
		{
			var parameterPath = new LinkedList<TestPath> ();
			TestBuilderPath builderPath = null;

			while (path != null && builderPath == null) {
				parameterPath.AddLast (path);

				builderPath = path as TestBuilderPath;
				path = path.Parent;
			}

			if (builderPath == null)
				throw new InternalErrorException ();

			var builder = builderPath.Builder;

			var node = new XElement (BuilderName);
			node.Add (new XAttribute ("Type", builder.GetType ().Name));
			node.Add (new XAttribute ("Name", builder.FullName));

			var pathIter = parameterPath.Last.Previous;

			while (pathIter != null) {
				var element = new XElement (ParameterName);
				element.Add (new XAttribute ("Type", pathIter.Value.Type));

				if (!pathIter.Value.Serialize (element))
					throw new InternalErrorException ();

				node.Add (element);

				pathIter = pathIter.Previous;
			}

			return node;
		}

		public static TestInvoker Deserialize (TestSuite suite, XElement root)
		{
			if (!root.Name.Equals (InstanceName))
				throw new InternalErrorException ();

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
	}
}

