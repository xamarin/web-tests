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
using SD = System.Diagnostics;
using System.Collections.Generic;

namespace Xamarin.AsyncTests.Framework
{
	abstract class TestBuilder : ITestFilter
	{
		public string Name {
			get;
			private set;
		}

		public TestPathType PathType {
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

		public TestFlags Flags {
			get;
			private set;
		}

		public virtual TestBuilder Parent {
			get { return null; }
		}

		public virtual string FullName {
			get { return Name; }
		}

		public virtual string TypeKey {
			get { return GetType ().FullName; }
		}

		const TestFlags DefaultFlags = TestFlags.Browsable;

		protected TestBuilder (TestPathType type, string identifier, string name, ITestParameter parameter, TestFlags flags = DefaultFlags)
		{
			PathType = type;
			Identifier = identifier;
			Name = name;
			Flags = flags;
			Parameter = parameter;
		}

		public abstract TestFilter Filter {
			get;
		}

		public TestHost Host {
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

		TestHost host;
		IList<TestBuilder> children;
		TestPathTree tree;

		protected virtual void ResolveMembers ()
		{
		}

		protected bool Resolve ()
		{
			if (resolved)
				return !SkipThisTest;
			if (resolving)
				throw new InternalErrorException ();

			resolving = true;

			ResolveMembers ();

			TestPathTree parentTree = null;
			if (Parent != null)
				parentTree = Parent.Tree;

			IList<TestBuilder> unresolvedChildren = null;
			if (!SkipThisTest)
				unresolvedChildren = CreateChildren ();
			if (unresolvedChildren == null)
				unresolvedChildren = new TestBuilder[0];

			host = CreateHost ();

			tree = new TestPathTree (this, host, Parameter, parentTree);

			resolvedTree = true;

			children = new List<TestBuilder> ();

			foreach (var child in unresolvedChildren) {
				if (child.Parent != this)
					throw new InternalErrorException ($"Child {child} in {this} has wrong parent {child.Parent}.");
				if (child.Resolve ())
					children.Add (child);
			}

			resolvedChildren = true;

			resolved = true;

			return !SkipThisTest;
		}

		protected abstract bool SkipThisTest {
			get;
		}

		internal virtual TestInvoker CreateInnerInvoker (TestPathTreeNode node)
		{
			return new TestCollectionInvoker (this, node);
		}

		protected abstract IList<TestBuilder> CreateChildren ();

		protected virtual TestHost CreateHost ()
		{
			return new TestBuilderHost (this);
		}

		bool ITestFilter.Filter (TestContext ctx, TestInstance instance)
		{
			return RunFilter (ctx, instance);
		}

		internal bool RunFilter (TestContext ctx, TestInstance instance)
		{
			if (SkipThisTest)
				throw new InternalErrorException ();
			if (Filter != null) {
				if (Filter.Filter (ctx, instance, out var enabled))
					return enabled;
			}
			if (!HasChildren)
				return true;
			foreach (var child in Children) {
				if (child.RunFilter (ctx, instance))
					return true;
			}
			return false;
		}

		internal static TestBuilder GetFixtureBuilder (TestBuilder builder)
		{
			while (builder != null) {
				if (builder.PathType == TestPathType.Fixture)
					return builder;
				builder = builder.Parent;
			}
			return null;
		}

		internal static TestBuilder GetCaseBuilder (TestBuilder builder)
		{
			while (builder != null) {
				if (builder.PathType == TestPathType.Test)
					return builder;
				builder = builder.Parent;
			}
			return null;
		}

		public override string ToString () => $"[{GetType ().Name}:{PathType}:{Name}]";
	}
}
