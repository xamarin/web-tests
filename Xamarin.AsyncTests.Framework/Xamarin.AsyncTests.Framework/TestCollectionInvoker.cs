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
		public TestCollectionHost Host {
			get;
			private set;
		}

		public IList<TestBuilder> Children {
			get { return Host.Builder.Children; }
		}

		public TestCollectionInvoker (TestCollectionHost host)
			: base (host.Flags)
		{
			Host = host;
		}

		bool Filter (TestContext ctx, TestFilter filter)
		{
			bool enabled;
			var matched = filter.Filter (ctx, out enabled);
			if (!matched)
				enabled = !filter.MustMatch;

			return enabled;
		}

		public sealed override async Task<bool> Invoke (
			TestContext ctx, TestInstance instance, CancellationToken cancellationToken)
		{
			if (Children.Count == 0)
				return true;
			if (cancellationToken.IsCancellationRequested)
				return false;

			ctx.LogDebug (10, "Invoke({0}): {1} {2} {3}", ctx.Name,
				Flags, TestLogger.Print (instance), Children.Count);

			bool success = true;
			foreach (var child in Children) {
				if (cancellationToken.IsCancellationRequested)
					break;

				if (child.Filter != null && !Filter (ctx, child.Filter))
					continue;

				ctx.LogDebug (10, "InnerInvoke({0}): {1} {2} {3}", ctx.Name,
					TestLogger.Print (instance), child, Children.Count);

				var invoker = child.Invoker;

				success = await InvokeInner (ctx, instance, invoker, cancellationToken);

				ctx.LogDebug (10, "InnerInvoke({0}) done: {1} {2}", ctx.Name,
					TestLogger.Print (instance), success);

				if (!success)
					break;
			}

			ctx.LogDebug (10, "Invoke({0}) done: {1} {2} {3} - {4}", ctx.Name,
				Flags, TestLogger.Print (instance), Children.Count, success);

			return success;
		}
	}
}

