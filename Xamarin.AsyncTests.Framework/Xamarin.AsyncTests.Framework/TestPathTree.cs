//
// TestPathTree.cs
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

namespace Xamarin.AsyncTests.Framework
{
	class TestPathTree : IPathResolvable, IPathResolver
	{
		public TestBuilder Builder {
			get;
			private set;
		}

		public TestPathTree Next {
			get;
			private set;
		}

		public TestPath Node {
			get;
			private set;
		}

		internal TestPathTree (TestBuilder builder, TestPathTree next, TestPath node)
		{
			Builder = builder;
			Next = next;
			Node = node;
		}

		IPathResolver IPathResolvable.GetResolver ()
		{
			return this;
		}

		IPathNode IPathResolver.Node {
			get { return Node.Host; }
		}

		IPathResolvable IPathResolver.Resolve (IPathNode node, string parameter)
		{
			return ResolveTree (node, parameter);
		}

		internal TestPathTree ResolveTree (IPathNode node, string parameter)
		{
			TestSerializer.Debug ("RESOLVE: {0} {1}", this, node);
			TestSerializer.Debug ("RESOLVE #1: {0} {1}", Next != null, Node.BrokenBuilder != null);

			if (Next != null) {
				if (!node.Identifier.Equals (Node.Host.Identifier))
					throw new InternalErrorException ();
				if (Node.IsParameterized != (node.ParameterType != null))
					throw new InternalErrorException ();
				if (node.ParameterType != null && !node.ParameterType.Equals (Node.Host.ParameterType))
					throw new InternalErrorException ();
				if (Node.BrokenBuilder != null)
					throw new InternalErrorException ();
				return Next;
			}

			if (Node.BrokenBuilder == null)
				throw new InternalErrorException ();

			if (parameter == null)
				return this;

			foreach (var child in Node.BrokenBuilder.Children) {
				TestSerializer.Debug ("  RESOLVE #3: {0}", child);

				if (!node.Identifier.Equals (child.Identifier))
					throw new InternalErrorException ();
				if (!node.ParameterType.Equals (child.Identifier))
					throw new InternalErrorException ();

				if (!parameter.Equals (child.Parameter.Value))
					continue;

				return child.Tree;
			}

			throw new InternalErrorException ();
		}

		public override string ToString ()
		{
			return string.Format ("[TestPathTree: Builder={0}, Node={1}, Next={2}]", Builder, Node, Next != null ? Next.Node.ID : 0);
		}
	}
}

