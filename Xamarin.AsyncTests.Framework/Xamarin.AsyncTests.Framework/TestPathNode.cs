//
// TestPathNode.cs
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
using System.Collections.Generic;

namespace Xamarin.AsyncTests.Framework
{
	class TestPathNode : IPathResolver
	{
		public TestPathTree Tree {
			get;
			private set;
		}

		public TestPath Path {
			get;
			private set;
		}

		public TestPathNode (TestPathTree tree, TestPath path)
		{
			Tree = tree;
			Path = path;
		}

		bool resolved;
		bool resolvedContext;
		bool parameterized;
		TestPathNode innerNode;
		List<TestPathNode> parameters;
		List<TestPathNode> children;

		public bool HasParameters {
			get { return Path.Host.HasParameters; }
		}

		public bool HasChildren {
			get { return Tree.Builder.HasChildren; }
		}

		public TestPathNode Clone ()
		{
			return new TestPathNode (Tree, Path.Clone ());
		}

		public void Resolve ()
		{
			if (resolved)
				return;

			if (Tree.Inner != null) {
				var innerPath = new TestPath (Tree.Inner.Host, Path);
				innerNode = new TestPathNode (Tree.Inner, innerPath);
				return;
			}

			children = new List<TestPathNode> ();
			foreach (var child in Tree.Builder.Children) {
				var childPath = new TestPath (child.Host, Path, child.Parameter);
				children.Add (new TestPathNode (child.TreeRoot, childPath));
			}

			resolved = true;
		}

		public void Resolve (TestContext ctx)
		{
			if (resolvedContext)
				return;

			Resolve ();

			var innerCtx = ctx.CreateChild (Path.Name, Path);

			parameters = new List<TestPathNode> ();
			if ((Path.Flags & TestFlags.Browsable) != 0) {
				foreach (var parameter in Tree.Host.GetParameters (innerCtx)) {
					parameters.Add (Parameterize (parameter));
				}
			}

			resolvedContext = true;
		}

		TestPathNode Parameterize (ITestParameter parameter)
		{
			var newPath = Path.Parameterize (parameter);
			var newNode = new TestPathNode (Tree, newPath);
			newNode.parameterized = true;
			return newNode;
		}

		TestPathNode Parameterize (string value)
		{
			if (parameters == null || parameters.Count == 0)
				return this;

			foreach (var parameter in parameters) {
				if (parameter.Path.Parameter.Value == value)
					return parameter;
			}

			return this;
		}

		public IEnumerable<TestPathNode> GetParameters (TestContext ctx)
		{
			Resolve (ctx);

			if (parameterized)
				yield break;

			foreach (var parameter in parameters) {
				yield return parameter;
			}
		}

		public IEnumerable<TestPathNode> GetChildren ()
		{
			Resolve ();

			if (parameterized)
				yield break;

			if (innerNode != null) {
				yield return innerNode;
				yield break;
			}

			foreach (var child in children) {
				yield return child;
			}
		}

		TestPathNode IPathResolver.Node {
			get { return this; }
		}

		public IPathResolver Resolve (TestContext ctx, IPathNode node, string parameter)
		{
			Resolve (ctx);

			if (innerNode != null) {
				innerNode.Resolve (ctx);

				if (!TestPath.Matches (innerNode.Tree.Host, node))
					throw new InternalErrorException ();
				if (parameter != null)
					return innerNode.Parameterize (parameter);
				return innerNode;
			}

			if (parameter == null)
				throw new InternalErrorException ();

			foreach (var child in children) {
				if (!TestPath.Matches (child.Path.Host, node))
					throw new InternalErrorException ();

				if (!parameter.Equals (child.Path.Parameter.Value))
					continue;

				return child;
			}

			throw new InternalErrorException ();
		}

		internal TestInvoker CreateInvoker (TestContext ctx)
		{
			var invoker = CreateInvoker (ctx, null, true);

			invoker = WalkHierarchy (invoker, null, true);

			return invoker;
		}

		internal TestInvoker CreateChildInvoker (TestContext ctx)
		{
			return CreateInvoker (ctx, Path, false);
		}

		TestInvoker WalkHierarchy (TestInvoker invoker, TestPath root, bool flattenHierarchy)
		{
			var node = Path.Parent;
			while (node != root) {
				var cloned = node.Clone ();
				if (flattenHierarchy)
					cloned.Flags |= TestFlags.FlattenHierarchy;
				invoker = node.Host.CreateInvoker (cloned, invoker);
				node = node.Parent;
			}

			return invoker;
		}

		TestInvoker CreateInvoker (TestContext ctx, TestPath root, bool flattenHierarchy)
		{
			Resolve (ctx);

			TestInvoker invoker = null;
			if (innerNode != null)
				invoker = innerNode.CreateInvoker (ctx, root, flattenHierarchy);
			else
				invoker = CreateInnerInvoker (ctx);

			var node = Path.Clone ();
			if (flattenHierarchy)
				node.Flags |= TestFlags.FlattenHierarchy;
			invoker = node.Host.CreateInvoker (node, invoker);
			return invoker;
		}

		TestInvoker CreateInnerInvoker (TestContext ctx)
		{
			Resolve (ctx);

			var node = Clone ();
			var invoker = Tree.Builder.CreateInnerInvoker (node);
			var collectionInvoker = invoker as TestCollectionInvoker;
			if (collectionInvoker != null)
				collectionInvoker.ResolveChildren (ctx);
			return invoker;
		}

		internal bool RunFilter (TestContext ctx)
		{
			Resolve (ctx);

			if (innerNode != null)
				return true;
			else
				return Tree.Builder.RunFilter (ctx);
		}

		public override string ToString ()
		{
			return string.Format ("[TestPathNode: Tree={0}, Path={1}]", Tree, Path);
		}
	}
}

