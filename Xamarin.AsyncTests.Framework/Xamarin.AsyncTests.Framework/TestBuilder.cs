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

		public TestFilter Filter {
			get {
				if (!resolvedMembers)
					throw new InvalidOperationException ();
				return filter;
			}
		}

		public TestCase Test {
			get {
				if (!resolved)
					throw new InvalidOperationException ();
				return test;
			}
		}

		public TestHost ParameterHost {
			get {
				if (!resolved)
					throw new InvalidOperationException ();
				return parameterHost;
			}
		}

		public TestBuilderHost Host {
			get {
				if (!resolved)
					throw new InvalidOperationException ();
				return host;
			}
		}

		internal TestInvoker Invoker {
			get {
				if (!resolved)
					throw new InvalidOperationException ();
				return invoker;
			}
		}

		internal bool HasChildren {
			get {
				if (!resolvedChildren)
					throw new InvalidOperationException ();
				return children.Count > 0;
			}
		}

		internal IList<TestBuilder> Children {
			get {
				if (!resolvedChildren)
					throw new InvalidOperationException ();
				return children;
			}
		}

		bool resolving;
		bool resolved;
		bool resolvedMembers;
		bool resolvedChildren;

		TestFilter filter;
		TestHost parameterHost;
		TestBuilderHost host;
		TestInvoker invoker;
		TestCase test;
		IList<TestBuilder> children;

		protected void Resolve ()
		{
			Resolve (null);
		}

		protected virtual void ResolveMembers ()
		{
		}

		void Resolve (TestHost parent)
		{
			if (resolved || resolving)
				return;

			resolving = true;

			ResolveMembers ();

			filter = GetTestFilter ();

			children = CreateChildren ().ToList ();

			parameterHost = new CapturableTestHost (parent);

			parameterHost = CreateParameterHost (parameterHost);

			host = CreateHost (parameterHost);

			resolvedMembers = true;

			foreach (var child in children) {
				child.Resolve (parameterHost);
			}

			resolvedChildren = true;

			TestSerializer.Debug ("RESOLVE: {0}", this);
			TestSerializer.Dump (parameterHost);

			invoker = host.CreateInnerInvoker ();

			invoker = CreateParameterInvoker (host, parent, invoker);

			invoker = CreateInvoker (invoker);

			test = CreateTestCase ();

			resolved = true;
		}

		TestInvoker CreateInvoker (TestInvoker invoker)
		{
			if (Filter != null)
				invoker = new ConditionalTestInvoker (Filter, invoker);

			invoker = new TestBuilderInvoker (host, invoker);

			return invoker;
		}

		static TestInvoker CreateParameterInvoker (TestBuilderHost host, TestHost parent, TestInvoker invoker)
		{
			TestSerializer.Debug ("CREATE PARAM INVOKER: {0} {1}", host, parent);
			TestSerializer.Dump (host);

			TestHost current = host.Parent;
			while (current != null && current != parent) {
				TestSerializer.Debug ("CREATE PARAM INVOKER #1: {0}", current);

				invoker = current.CreateInvoker (invoker);

				current = current.Parent;
			}

			TestSerializer.Debug ("CREATE PARAM INVOKER #2: {0}", invoker);

			return invoker;
		}

		public TestBuilder FindChild (string name)
		{
			foreach (var child in Children) {
				TestSerializer.Debug ("FIND CHILD: {0} - {1}", child.FullName, name);
				if (child.FullName.Equals (name))
					return child;
			}

			throw new InternalErrorException ();
		}

		protected abstract IEnumerable<TestBuilder> CreateChildren ();

		protected abstract TestFilter GetTestFilter ();

		protected abstract TestHost CreateParameterHost (TestHost parent);

		protected abstract TestBuilderHost CreateHost (TestHost parent);

		protected abstract TestCase CreateTestCase ();

		public TestInvoker Deserialize (TestHost parent)
		{
			var host = CreateHost (parent);

			invoker = host.CreateInnerInvoker ();

			invoker = CreateParameterInvoker (host, null, invoker);

			invoker = CreateInvoker (invoker);

			return invoker;
		}

		public static TestCase CaptureContext (TestContext ctx, TestInstance instance)
		{
			var name = TestInstance.GetTestName (instance);
			if (!name.FullName.Contains ("MartinTest"))
				return null;

			TestSerializer.Debug ("CAPTURE CONTEXT: {0}", name);
			TestSerializer.Dump (instance);

			TestSerializer.Debug (Environment.NewLine);

			XElement node;
			try {
				node = TestSerializer.Serialize (instance);
				if (node == null) {
					TestSerializer.Debug ("CAPTURE FAILED");
					return null;
				}
			} catch (Exception ex) {
				TestSerializer.Debug ("CAPTURE ERROR: {0}", ex);
				return null;
			}

			TestSerializer.Debug ("CAPTURE DONE: {0}", node);

			TestInvoker invoker;
			try {
				invoker = TestSerializer.Deserialize (ctx.Suite, node);
				if (invoker == null) {
					TestSerializer.Debug ("DESERIALIZE FAILED");
					return null;
				}
			} catch (Exception ex) {
				TestSerializer.Debug ("DESERIALIZE ERROR: {0}", ex);
				return null;
			}

			TestSerializer.Debug ("DESERIALIZE DONE: {0}", invoker);
			return new CapturedTestCase (ctx.Suite, name, invoker);
		}
	}
}

