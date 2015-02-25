//
// TestBuilderCollectionInvoker.cs
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
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Xamarin.AsyncTests.Framework
{
	class TestCollectionInvoker : AggregatedTestInvoker
	{
		public TestBuilder Builder {
			get;
			private set;
		}

		public TestPathNode Node {
			get;
			private set;
		}

		public TestCollectionInvoker (TestBuilder builder, TestPathNode node, TestFlags flags = TestFlags.None)
			: base (flags)
		{
			Builder = builder;
			Node = node;
		}

		bool Filter (TestContext ctx, TestFilter filter)
		{
			bool enabled;
			var matched = filter.Filter (ctx, out enabled);
			if (!matched)
				enabled = !filter.MustMatch;

			return enabled;
		}

		LinkedList<Tuple<TestPathNode,TestInvoker>> children;

		internal void ResolveChildren (TestContext ctx)
		{
			children = new LinkedList<Tuple<TestPathNode, TestInvoker>> ();
			foreach (var child in Node.GetChildren (ctx)) {
				var invoker = child.CreateChildInvoker (ctx);
				children.AddLast (new Tuple<TestPathNode, TestInvoker> (child, invoker));
			}
		}

		bool SetUp (TestContext ctx, TestInstance instance)
		{
			ctx.LogDebug (10, "SetUp({0}): {1} {2}", ctx.Name, TestLogger.Print (Builder), TestLogger.Print (instance));

			try {
				ResolveChildren (ctx);
				return true;
			} catch (OperationCanceledException) {
				ctx.OnTestCanceled ();
				return false;
			} catch (Exception ex) {
				ctx.OnError (ex);
				return false;
			}
		}

		public sealed override async Task<bool> Invoke (
			TestContext ctx, TestInstance instance, CancellationToken cancellationToken)
		{
			if (cancellationToken.IsCancellationRequested)
				return false;

			if (!SetUp (ctx, instance))
				return false;

			ctx.LogDebug (10, "Invoke({0}): {1} {2} {3}", ctx.Name,
				Flags, TestLogger.Print (instance), children.Count);

			bool success = true;
			foreach (var child in children) {
				if (cancellationToken.IsCancellationRequested)
					break;

				ctx.LogDebug (10, "InnerInvoke({0}): {1} {2}", ctx.Name,
					TestLogger.Print (instance), child);

				var invoker = child.Item2;

				success = await InvokeInner (ctx, instance, invoker, cancellationToken);

				ctx.LogDebug (10, "InnerInvoke({0}) done: {1} {2}", ctx.Name,
					TestLogger.Print (instance), success);

				if (!success)
					break;
			}

			ctx.LogDebug (10, "Invoke({0}) done: {1} {2} - {3}", ctx.Name,
				Flags, TestLogger.Print (instance), success);

			return success;
		}
	}
}

