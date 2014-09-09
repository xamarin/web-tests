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
		static readonly XName ParameterName = "TestParameter";

		public static XElement Serialize (TestInstance instance)
		{
			var name = TestInstance.GetTestName (instance);

			Debug ("SERIALIZE: {0} - {1} {2}", name, instance, instance.Host);
			Dump (instance);
			Debug (Environment.NewLine);

			var node = new XElement (InstanceName);
			node.Add (new XAttribute ("Name", name.FullName));

			while (instance != null) {
				var element = new XElement (ParameterName);
				element.Add (new XAttribute ("Type", instance.GetType ().Name));
				element.Add (new XAttribute ("Host", instance.Host.GetType ().Name));
				node.AddFirst (element);

				if (!instance.Host.Serialize (element, instance)) {
					Debug ("SERIALIZE INSTANCE FAILED: {0}", instance.Host);
					instance.Host.Serialize (element, instance);
					return null;
				}

				instance = instance.Parent;
			}

			Debug ("SERIALIZE DONE: {0}", node);
			return node;
		}

		static TestBuilder FindBuilder (TestInstance instance)
		{
			while (instance != null) {
				var builderInstance = instance as TestBuilderInstance;
				if (builderInstance != null)
					return builderInstance.Builder;
			}

			return null;
		}

		public static TestInvoker Deserialize (TestSuite suite, XElement root)
		{
			if (!root.Name.Equals (InstanceName))
				throw new InternalErrorException ();

			var name = root.Attribute ("Name").Value;

			Debug ("DESERIALIZE: {0}", name);

			var parameters = new LinkedList<XElement> (root.Elements (ParameterName));

			var builders = parameters.Where (p => p.Attribute ("Type").Value.Equals ("TestBuilderInstance"));
			Debug ("DESERIALIZE #1: {0}", builders.Count ());

			var reflectionSuite = (ReflectionTestSuite)suite;

			TestBuilder current = null;
			foreach (var builderNode in builders) {
				var builderHost = builderNode.Attribute ("Host").Value;
				var builderName = builderNode.Attribute ("Name").Value;

				Debug ("DESERIALIZE #2: {0} - {1} {2}", builderNode, builderHost, builderName);
				if (current == null)
					current = reflectionSuite.FindFixture (builderName);
				else
					current = current.FindChild (builderName);

				if (current == null)
					throw new InternalErrorException ();
			}

			Debug ("DESERIALIZE #3: {0}", current);

			Dump (current.Host);
			Dump (current.ParameterHost);

			TestHost deserialized = null;

			var hostList = new LinkedList<TestHost> ();
			for (var h = current.ParameterHost; h != null; h = h.Parent)
				hostList.AddLast (h);

			var hostIter = hostList.Last;
			var paramIter = parameters.First;

			while (hostIter != null) {
				var host = hostIter.Value;
				XElement param = null;
				if (paramIter != null) {
					param = paramIter.Value;
					paramIter = paramIter.Next;
				}

				Debug ("DESERIALIZE #4: {0} {1}", param != null ? param.ToString () : "<null>", host);

				if (param != null) {
					var paramType = param.Attribute ("Type").Value;
					var paramHost = param.Attribute ("Host").Value;
					if (paramType.Equals ("TestBuilderInstance"))
						continue;

					if (!host.GetType ().Name.Equals (paramHost)) {
						Debug ("DESERIALIZE #4a - {0} {1}", paramHost, host);
						throw new InternalErrorException ();
					}
				}

				var old = deserialized;
				deserialized = hostIter.Value.Deserialize (param, deserialized);
				if (deserialized == null) {
					hostIter.Value.Deserialize (param, old);
					throw new InternalErrorException ();
				}

				hostIter = hostIter.Previous;
			}

			Debug ("LOOP DONE: {0} {1}", paramIter, hostIter);

			if (paramIter != null || hostIter != null)
				throw new InternalErrorException ();

			Debug ("DESERIALIZE #6: {0}", deserialized);
			Dump (deserialized);

			var invoker = current.Deserialize (deserialized);

			Debug ("DESERIALIZE #7: {0}", invoker);

			return invoker;
		}

		internal static void Dump (TestInstance instance)
		{
			Debug (Environment.NewLine);
			Debug ("DUMPING INSTANCE");
			while (instance != null) {
				Debug ("INSTANCE: {0} {1}", instance, instance.Host);
				instance = instance.Parent;
			}
		}

		internal static void Dump (TestHost host)
		{
			Debug (Environment.NewLine);
			Debug ("DUMPING HOST");
			while (host != null) {
				Debug ("HOST: {0}", host);
				host = host.Parent;
			}
		}

		internal static void Debug (string message, params object[] args)
		{
			System.Diagnostics.Debug.WriteLine (string.Format (message, args));
		}

	}
}

