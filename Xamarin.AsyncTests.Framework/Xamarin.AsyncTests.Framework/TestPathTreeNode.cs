﻿//
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
using System.Xml.Linq;
using System.Collections.Generic;

namespace Xamarin.AsyncTests.Framework
{
	class TestPathTreeNode : IPathResolver
	{
		public TestPathTree Tree {
			get;
		}

		public TestPath Path {
			get;
		}

		public TestNodeInternal Node {
			get;
		}

		public TestPathTreeNode (TestPathTree tree, TestPath path, TestNodeInternal node)
		{
			Tree = tree;
			Path = path;
			Node = node;
		}

		public TestPathTreeNode (TestPathTree tree, TestPath path)
		{
			Tree = tree;
			Path = path;
			Node = (TestNodeInternal)path.Node;
		}

		bool resolved;
		bool resolvedContext;
		bool parameterized;
		List<TestPathTreeNode> parameters;
		List<TestPathTreeNode> children;

		public bool HasParameters => Node.HasParameters;

		public bool HasChildren => Tree.Builder.HasChildren;

		public bool IsPathHidden => (Node.Flags & TestFlags.PathHidden) != 0;

		public void Resolve ()
		{
			if (resolved)
				return;

			children = new List<TestPathTreeNode> ();
			foreach (var child in Tree.Builder.Children) {
				var node = new TestNodeInternal (child.Host, child.Parameter);
				var childPath = new TestPath (Path, node);
				children.Add (new TestPathTreeNode (child.Tree, childPath, node));
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
			if (Node.IsBrowseable) {
				foreach (var parameter in Tree.Host.GetParameters (innerCtx)) {
					parameters.Add (Parameterize (parameter));
				}
			}

			resolvedContext = true;
		}

		TestPathTreeNode Parameterize (ITestParameter parameter)
		{
			var newPath = Path.Parameterize (parameter);
			var newNode = new TestPathTreeNode (Tree, newPath, (TestNodeInternal)newPath.Node);
			newNode.parameterized = true;
			return newNode;
		}

		ITestParameter GetParameter (string value)
		{
			if (string.IsNullOrEmpty (value))
				return null;
			if (Node.PathType == TestPathType.Fork)
				return TestSerializer.GetStringParameter (value);

			if (parameters == null || parameters.Count == 0)
				return null;

			foreach (var parameter in parameters.ToArray ()) {
				if (parameter.Node.Parameter.Value == value)
					return parameter.Node.Parameter;
			}

			return null;
		}

		TestPathTreeNode Parameterize (string value, XElement customValue)
		{
			var parameter = GetParameter (value);
			if (customValue == null)
				return Parameterize (parameter);
			var newPath = Path.Parameterize (parameter, customValue);
			var newNode = new TestPathTreeNode (Tree, newPath, (TestNodeInternal)newPath.Node);
			newNode.parameterized = true;
			return newNode;
		}

		public IReadOnlyList<TestPathTreeNode> GetParameters (TestContext ctx)
		{
			Resolve (ctx);

			if (parameterized || parameters == null || parameters.Count == 0)
				return new TestPathTreeNode[0];

			return parameters;
		}

		public IReadOnlyList<TestPathTreeNode> GetChildren ()
		{
			Resolve ();

			if (parameterized && Node.PathType != TestPathType.Fork)
				return new TestPathTreeNode[0];

			return children;
		}

		TestPathTreeNode IPathResolver.Node {
			get { return this; }
		}

		public IPathResolver Resolve (TestContext ctx, TestNode node)
		{
			return Resolve (ctx, node, false);
		}

		IPathResolver Resolve (TestContext ctx, TestNode node, bool recursive)
		{
			Resolve (ctx);

			if (children.Count == 1) {
				if (children[0].IsPathHidden)
					return children[0].Resolve (ctx, node, true);
				if (IsPathHidden) {
					if (!recursive)
						throw new InternalErrorException ();
					return children[0];
				}
			}

			if (node.ParameterValue == null && node.CustomParameter == null)
				throw new InternalErrorException ();

			foreach (var child in children) {
				child.Resolve (ctx);

				if (child.IsPathHidden)
					throw new InternalErrorException ();
				if (!TestNodeInternal.Matches (child.Node, node))
					throw new InternalErrorException ();
				if (!TestNodeInternal.Matches (child.Tree.Host, node))
					throw new InternalErrorException ();

				if (child.Node.Parameter != null) {
					if (!node.ParameterValue.Equals (child.Node.Parameter.Value))
						continue;
					return child;
				}

				return child.Parameterize (node.ParameterValue, node.CustomParameter);
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
			var path = Path.Parent;
			while (path != root) {
				var flags = path.Node.Flags;
				if (flattenHierarchy)
					flags |= TestFlags.FlattenHierarchy;
				var node = (TestNodeInternal)path.Node;
				invoker = node.CreateInvoker (invoker, flags);
				path = path.Parent;
			}

			return invoker;
		}

		TestInvoker CreateInvoker (TestContext ctx, TestPath root, bool flattenHierarchy)
		{
			Resolve (ctx);

			var invoker = Tree.Builder.CreateInnerInvoker (this);

			var flags = Node.Flags;
			if (flattenHierarchy)
				flags |= TestFlags.FlattenHierarchy;
			invoker = Node.CreateInvoker (invoker, flags);
			return invoker;
		}

		public override string ToString ()
		{
			return string.Format ("[TestPathNode: Tree={0}, Path={1}]", Tree, Path);
		}
	}
}

