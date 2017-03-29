//
// TestPathTreeNode.cs
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
	class TestPathTreeNode : IPathResolver
	{
		public TestPathTree Tree {
			get;
			private set;
		}

		public TestPathInternal Path {
			get;
			private set;
		}

		public TestPathTreeNode (TestPathTree tree, TestPathInternal path)
		{
			Tree = tree;
			Path = path;
		}

		bool resolved;
		bool resolvedContext;
		bool parameterized;
		TestPathTreeNode innerNode;
		List<TestPathTreeNode> parameters;
		List<TestPathTreeNode> children;

		public bool HasParameters {
			get { return Path.Host.HasParameters; }
		}

		public bool HasChildren {
			get { return Tree.Builder.HasChildren; }
		}

		public TestPathTreeNode Clone ()
		{
			return new TestPathTreeNode (Tree, Path.Clone ());
		}

		public void Resolve ()
		{
			if (resolved)
				return;

			if (Tree.Inner != null) {
				var innerPath = new TestPathInternal (Tree.Inner.Host, Path);
				innerNode = new TestPathTreeNode (Tree.Inner, innerPath);
				return;
			}

			children = new List<TestPathTreeNode> ();
			foreach (var child in Tree.Builder.Children) {
				var childPath = new TestPathInternal (child.Host, Path, child.Parameter);
				children.Add (new TestPathTreeNode (child.TreeRoot, childPath));
			}

			resolved = true;
		}

		public void Resolve (TestContext ctx)
		{
			if (resolvedContext)
				return;

			Resolve ();

			var innerCtx = ctx.CreateChild (Path);

			parameters = new List<TestPathTreeNode> ();
			if ((Path.Flags & TestFlags.Browsable) != 0) {
				foreach (var parameter in Tree.Host.GetParameters (innerCtx)) {
					parameters.Add (Parameterize (parameter));
				}
			}

			resolvedContext = true;
		}

		TestPathTreeNode Parameterize (ITestParameter parameter)
		{
			var newPath = Path.Parameterize (parameter);
			var newNode = new TestPathTreeNode (Tree, newPath);
			newNode.parameterized = true;
			return newNode;
		}

		TestPathTreeNode Parameterize (string value)
		{
			if (parameters == null || parameters.Count == 0)
				return this;

			foreach (var parameter in parameters.ToArray ()) {
				if (parameter.Path.Parameter.Value == value)
					return parameter;
			}

			return this;
		}

		public IEnumerable<TestPathTreeNode> GetParameters (TestContext ctx)
		{
			Resolve (ctx);

			if (parameterized || parameters == null || parameters.Count == 0)
				yield break;

			foreach (var parameter in parameters.ToArray ()) {
				yield return parameter;
			}
		}

		public IEnumerable<TestPathTreeNode> GetChildren ()
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

		TestPathTreeNode IPathResolver.Node {
			get { return this; }
		}

		public IPathResolver Resolve (TestContext ctx, PathNode node)
		{
			Resolve (ctx);

			if (innerNode != null) {
				innerNode.Resolve (ctx);

				if (!TestPathInternal.Matches (innerNode.Tree.Host, node))
					throw new InternalErrorException ();
				if (node.ParameterValue != null)
					return innerNode.Parameterize (node.ParameterValue);
				return innerNode;
			}

			if (node.ParameterValue == null)
				throw new InternalErrorException ();

			foreach (var child in children) {
				if (!TestPathInternal.Matches (child.Path.Host, node))
					throw new InternalErrorException ();

				if (!node.ParameterValue.Equals (child.Path.Parameter.Value))
					continue;

				return child;
			}

			throw new InternalErrorException ("#4");
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

		TestInvoker WalkHierarchy (TestInvoker invoker, TestPathInternal root, bool flattenHierarchy)
		{
			var node = Path.InternalParent;
			while (node != root) {
				var flags = node.Flags;
				if (flattenHierarchy)
					flags |= TestFlags.FlattenHierarchy;
				invoker = node.Host.CreateInvoker (node, invoker, flags);
				node = node.InternalParent;
			}

			return invoker;
		}

		TestInvoker CreateInvoker (TestContext ctx, TestPathInternal root, bool flattenHierarchy)
		{
			Resolve (ctx);

			TestInvoker invoker = null;
			if (innerNode != null)
				invoker = innerNode.CreateInvoker (ctx, root, false);
			else
				invoker = CreateInnerInvoker (ctx);

			var flags = Path.Flags;
			if (flattenHierarchy)
				flags |= TestFlags.FlattenHierarchy;
			invoker = Path.Host.CreateInvoker (Path, invoker, flags);
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

