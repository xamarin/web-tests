//
// PathBasedTestCase.cs
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
using System.Linq;
using System.Xml.Linq;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Xamarin.AsyncTests.Framework
{
	class PathBasedTestCase : TestCase
	{
		public TestPathNode Node {
			get;
			private set;
		}

		public PathBasedTestCase (TestPathNode node)
			: base (node.Tree.Builder.Suite, TestPath.GetTestName (node.Path))
		{
			Node = node;
		}

		public override IEnumerable<TestCase> GetChildren (TestContext ctx)
		{
			return ResolveChildren (ctx);
		}

		List<PathBasedTestCase> children;

		IEnumerable<PathBasedTestCase> ResolveChildren (TestContext ctx)
		{
			if (children != null)
				return children;

			Node.Resolve (ctx);
			children = new List<PathBasedTestCase> ();
			AddChildren (children, ctx, Node);
			return children;
		}

		static void AddChildren (List<PathBasedTestCase> children, TestContext ctx, TestPathNode node)
		{
			foreach (var child in node.GetChildren (ctx)) {
				if ((child.Path.Flags & TestFlags.Hidden) != 0) {
					AddChildren (children, ctx, child);
				} else {
					children.Add (new PathBasedTestCase (child));
				}
			}
		}

		internal override Task<bool> Run (TestContext ctx, CancellationToken cancellationToken)
		{
			TestSerializer.Debug ("RUN: {0}", this);

			var invoker = Node.CreateInvoker (ctx);
			return invoker.Invoke (ctx, null, cancellationToken);
		}

		public override XElement Serialize ()
		{
			var element = TestSerializer.SerializePath (Node.Path);
			TestSerializer.DeserializePath (Suite, element);
			return element;
		}

		public override void Resolve (TestContext ctx)
		{
			var element = Serialize ();
			TestSerializer.Debug ("RESOLVE: {0}\n{1}", this, element);

			var children = ResolveChildren (ctx);

			foreach (var child in children) {
				child.Serialize ();
			}

			foreach (var child in children) {
				child.Resolve (ctx);
			}
		}

		public override string ToString ()
		{
			return string.Format ("[PathBasedTestCase: {0}]", Node);
		}
	}
}

