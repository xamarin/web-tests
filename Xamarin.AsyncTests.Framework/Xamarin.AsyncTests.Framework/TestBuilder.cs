//
// TestBuilder.cs
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
using System.Xml.Linq;
using System.Collections.Generic;

namespace Xamarin.AsyncTests.Framework
{
	abstract class TestBuilder
	{
		public TestSuite Suite {
			get;
			private set;
		}

		public TestName Name {
			get;
			private set;
		}

		public virtual TestBuilder Parent {
			get { return null; }
		}

		public virtual string FullName {
			get { return Name.FullName; }
		}

		protected TestBuilder (TestSuite suite, TestName name)
		{
			Suite = suite;
			Name = name;
		}

		public abstract TestFilter Filter {
			get;
		}

		public TestBuilderHost Host {
			get {
				if (!resolved)
					throw new InternalErrorException ();
				return host;
			}
		}


		internal TestInvoker Invoker {
			get {
				if (!resolved)
					throw new InternalErrorException ();
				return invoker;
			}
		}

		internal bool HasChildren {
			get {
				if (!resolvedChildren)
					throw new InternalErrorException ();
				return children.Count > 0;
			}
		}

		internal IList<TestBuilder> Children {
			get {
				if (!resolvedChildren)
					throw new InternalErrorException ();
				return children;
			}
		}

		internal LinkedList<TestHost> ParameterHosts {
			get {
				if (!resolved)
					throw new InternalErrorException ();
				return parameterHosts;
			}
		}

		bool resolving;
		bool resolved;
		bool resolvedChildren;

		TestBuilderHost host;
		TestInvoker invoker;
		IList<TestBuilder> children;
		LinkedList<TestHost> parameterHosts;

		protected virtual void ResolveMembers ()
		{
		}

		protected void Resolve ()
		{
			if (resolved || resolving)
				return;

			resolving = true;

			ResolveMembers ();

			children = CreateChildren ().ToList ();

			host = CreateHost ();

			parameterHosts = new LinkedList<TestHost> ();
			foreach (var current in CreateParameterHosts ())
				parameterHosts.AddLast (current);

			foreach (var child in children) {
				child.Resolve ();
			}

			resolvedChildren = true;

			invoker = CreateParameterInvoker ();

			resolved = true;
		}

		TestInvoker CreateParameterInvoker ()
		{
			var invoker = host.CreateInnerInvoker ();

			var current = parameterHosts.Last;
			while (current != null) {
				invoker = current.Value.CreateInvoker (invoker);
				current = current.Previous;
			}

			invoker = host.CreateInvoker (invoker);

			return invoker;
		}

		public TestBuilder FindChild (string name)
		{
			foreach (var child in Children) {
				if (child.FullName.Equals (name))
					return child;
			}

			throw new InternalErrorException ();
		}

		protected abstract IEnumerable<TestBuilder> CreateChildren ();

		protected abstract IEnumerable<TestHost> CreateParameterHosts ();

		protected abstract TestBuilderHost CreateHost ();

		public static TestCase CaptureContext (TestContext ctx, TestInstance instance)
		{
			var name = TestInstance.GetTestName (instance);

			var node = TestSerializer.Serialize (instance);
			if (node == null)
				throw new InternalErrorException ();

			var test = new CapturedTestCase (ctx.Suite, name, node);
			if (!test.Resolve ())
				throw new InternalErrorException ();

			return test;
		}
	}
}

