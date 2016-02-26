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
	abstract class TestBuilder : IPathNode
	{
		public string Name {
			get;
			private set;
		}

		public string Identifier {
			get;
			private set;
		}

		public ITestParameter Parameter {
			get;
			private set;
		}

		string IPathNode.ParameterType {
			get { return Identifier; }
		}

		public TestName TestName {
			get;
			private set;
		}

		public virtual TestBuilder Parent {
			get { return null; }
		}

		public virtual string FullName {
			get { return TestName.FullName; }
		}

		public virtual string TypeKey {
			get { return GetType ().FullName; }
		}

		protected TestBuilder (string identifier, string name, ITestParameter parameter)
		{
			Identifier = identifier;
			Name = name;
			TestName = name != null ? new TestName (name) : TestName.Empty;
			Parameter = parameter;
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

		internal TestPathTree TreeRoot {
			get {
				if (!resolvedTree)
					throw new InternalErrorException ();
				return treeRoot;
			}
		}

		internal TestPathTree Tree {
			get {
				if (!resolvedTree)
					throw new InternalErrorException ();
				return tree;
			}
		}

		bool resolving;
		bool resolved;
		bool resolvedTree;
		bool resolvedChildren;

		TestBuilderHost host;
		IList<TestBuilder> children;
		TestPathTree tree;
		TestPathTree treeRoot;

		protected virtual void ResolveMembers ()
		{
		}

		protected void Resolve ()
		{
			if (resolved || resolving)
				return;

			resolving = true;

			ResolveMembers ();

			TestPathTree parentTree = null;
			if (Parent != null)
				parentTree = Parent.Tree;

			children = CreateChildren ().ToList ();

			host = CreateHost ();

			var parameterHosts = new LinkedList<TestHost> ();

			tree = treeRoot = new TestPathTree (this, host, parentTree);

			foreach (var current in CreateParameterHosts ()) {
				parameterHosts.AddFirst (current);
			}

			var parameterIter = parameterHosts.Last;
			while (parameterIter != null) {
				tree = tree.Add (parameterIter.Value);
				parameterIter = parameterIter.Previous;
			}

			resolvedTree = true;

			foreach (var child in children) {
				child.Resolve ();
			}

			resolvedChildren = true;

			resolved = true;
		}

		internal bool RunFilter (TestContext ctx)
		{
			if (Filter == null)
				return true;

			bool enabled;
			var matched = Filter.Filter (ctx, out enabled);
			if (!matched)
				enabled = !Filter.MustMatch;

			return enabled;
		}

		internal abstract TestInvoker CreateInnerInvoker (TestPathNode node);

		protected abstract IEnumerable<TestBuilder> CreateChildren ();

		protected abstract IEnumerable<TestHost> CreateParameterHosts ();

		protected TestBuilderHost CreateHost ()
		{
			return new TestBuilderHost (this, this);
		}
	}
}

