//
// AggregatedTestInvoker.cs
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
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Xamarin.AsyncTests.Framework
{
	abstract class AggregatedTestInvoker : TestInvoker
	{
		public TestFlags Flags {
			get;
			private set;
		}

		public bool ContinueOnError {
			get { return (Flags & TestFlags.ContinueOnError) != 0; }
		}

		public bool IsHidden {
			get { return (Flags & TestFlags.Hidden) != 0; }
		}

		public AggregatedTestInvoker (TestFlags flags = TestFlags.None)
		{
			Flags = flags;
		}

		public static TestInvoker Create (TestFlags flags, params TestInvoker[] invokers)
		{
			return Create (flags, null, invokers);
		}

		public static TestInvoker Create (TestHost host, params TestInvoker[] invokers)
		{
			return Create (host.Flags, host, invokers);
		}

		static TestInvoker Create (TestFlags flags, TestHost host, params TestInvoker[] invokers)
		{
			TestInvoker invoker = new CollectionTestInvoker (flags, invokers);
			if (host != null)
				invoker = host.CreateInvoker (invoker);
			if ((flags & (TestFlags.Browsable | TestFlags.FlattenHierarchy)) == TestFlags.Browsable)
				invoker = new ResultGroupTestInvoker (invoker);
			return invoker;
		}

		protected async Task<bool> InvokeInner (
			InvocationContext ctx, TestInstance instance, TestInvoker invoker,
			CancellationToken cancellationToken)
		{
			ctx.LogDebug (3, "Running({0}): {1}", ctx.Name, invoker);

			try {
				cancellationToken.ThrowIfCancellationRequested ();
				var success = await invoker.Invoke (ctx, instance, cancellationToken);
				return success || ContinueOnError;
			} catch (OperationCanceledException) {
				ctx.OnTestFinished (TestStatus.Canceled);
				return false;
			} catch (Exception ex) {
				ctx.OnError (ex);
				return false;
			}
		}
	}
}

