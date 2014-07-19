//
// HostInstanceTestInvoker.cs
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
	class HeavyTestInvoker : AggregatedTestInvoker
	{
		public HeavyTestHost Host {
			get;
			private set;
		}

		public TestInvoker Inner {
			get;
			private set;
		}

		public HeavyTestInvoker (HeavyTestHost host, TestInvoker inner)
			: base (host.Flags)
		{
			Host = host;
			Inner = inner;
		}

		async Task<HeavyTestInstance> SetUp (
			TestContext ctx, TestInstance instance, TestResult result, CancellationToken cancellationToken)
		{
			var name = TestInstance.GetTestName (instance);
			ctx.Debug (3, "SetUp({0}): {1} {2}", name, ctx.Print (Host), ctx.Print (instance));

			try {
				cancellationToken.ThrowIfCancellationRequested ();
				var childInstance = (HeavyTestInstance)Host.CreateInstance (ctx, instance);
				await childInstance.Initialize (ctx, cancellationToken);
				return childInstance;
			} catch (OperationCanceledException) {
				result.Status = TestStatus.Canceled;
				return null;
			} catch (Exception ex) {
				result.AddError (ex);
				ctx.Statistics.OnException (name, ex);
				return null;
			}
		}

		async Task<bool> TearDown (
			TestContext ctx, HeavyTestInstance instance, TestResult result, CancellationToken cancellationToken)
		{
			var name = TestInstance.GetTestName (instance);
			ctx.Debug (3, "TearDown({0}): {1} {2}", name, ctx.Print (Host), ctx.Print (instance));

			try {
				await instance.Destroy (ctx, cancellationToken);
				return true;
			} catch (OperationCanceledException) {
				result.Status = TestStatus.Canceled;
				return false;
			} catch (Exception ex) {
				result.AddError (ex);
				ctx.Statistics.OnException (name, ex);
				return false;
			}
		}

		public override async Task<bool> Invoke (
			TestContext ctx, TestInstance instance, TestResult result, CancellationToken cancellationToken)
		{
			var innerInstance = await SetUp (ctx, instance, result, cancellationToken);
			if (innerInstance == null)
				return false;

			var success = await InvokeInner (ctx, innerInstance, result, Inner, cancellationToken);

			if (!await TearDown (ctx, innerInstance, result, cancellationToken))
				success = false;

			return success;
		}
	}
}

