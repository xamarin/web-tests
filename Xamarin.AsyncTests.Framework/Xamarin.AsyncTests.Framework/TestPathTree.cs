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
using System.Collections.Generic;

namespace Xamarin.AsyncTests.Framework
{
	class TestPathTree : IPathResolvable, IPathResolver
	{
		public TestBuilder Builder {
			get;
			private set;
		}

		public TestHost Host {
			get;
			private set;
		}

		public TestPathTree Parent {
			get;
			private set;
		}

		public TestPathTree Outer {
			get;
			private set;
		}

		public TestPathTree Inner {
			get;
			internal set;
		}

		internal TestPathTree (TestBuilder builder, TestHost host, TestPathTree parent)
		{
			Builder = builder;
			Host = host;
			Parent = parent;
		}

		internal TestPathTree Add (TestHost host)
		{
			var next = new TestPathTree (Builder, host, null);
			if (Inner != null)
				throw new InternalErrorException ();
			Inner = next;
			next.Outer = this;
			return next;
		}

		IPathResolver IPathResolvable.GetResolver ()
		{
			return this;
		}

		IPathNode IPathResolver.Node {
			get { return Host; }
		}

		IPathResolvable IPathResolver.Resolve (IPathNode node, string parameter)
		{
			return ResolveTree (node, parameter);
		}

		internal TestPathTree ResolveTree (IPathNode node, string parameter)
		{
			if (Inner != null) {
				if (!TestPath.Matches (Inner.Host, node))
					throw new InternalErrorException ();
				return Inner;
			}

			if (parameter == null)
				return this;

			foreach (var child in Builder.Children) {
				if (!node.Identifier.Equals (child.Identifier))
					throw new InternalErrorException ();
				if (!node.ParameterType.Equals (child.Identifier))
					throw new InternalErrorException ();

				if (!parameter.Equals (child.Parameter.Value))
					continue;

				return child.TreeRoot;
			}

			throw new InternalErrorException ();
		}

		static int next_id;
		public readonly int ID = ++next_id;

		public override string ToString ()
		{
			var builderName = Builder.GetType ().Name;
			var outer = Outer != null ? string.Format (", Outer={0}", Outer.ID) : string.Empty;
			var inner = Inner != null ? string.Format (", Inner={0}", Inner.ID) : string.Empty;
			var parent = Parent != null ? string.Format (", Parent={0}", Parent.ID) : string.Empty;
			return string.Format ("[TestPathTree({0}): Builder={1}, Host={2}{3}{4}{5}]", ID, builderName, Host, parent, outer, inner);
		}
	}
}

