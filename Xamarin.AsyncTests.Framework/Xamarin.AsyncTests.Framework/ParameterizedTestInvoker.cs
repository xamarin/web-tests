//
// ParameterizedTestInvoker.cs
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
	class ParameterizedTestInvoker : AggregatedTestInvoker
	{
		public ParameterizedTestHost Host {
			get;
			private set;
		}

		public TestInvoker Inner {
			get;
			private set;
		}

		public ParameterizedTestInvoker (ParameterizedTestHost host, TestInvoker inner)
			: base (host.Flags)
		{
			Host = host;
			Inner = inner;
		}

		ParameterizedTestInstance SetUp (TestContext ctx, TestInstance instance, TestResult result)
		{
			var name = TestInstance.GetTestName (instance);
			ctx.Debug (3, "SetUp({0}): {1} {2}", name, ctx.Print (Host), ctx.Print (instance));

			try {
				return (ParameterizedTestInstance)Host.CreateInstance (ctx, instance);
			} catch (OperationCanceledException) {
				result.Status = TestStatus.Canceled;
				return null;
			} catch (Exception ex) {
				result.AddError (ex);
				ctx.Statistics.OnException (name, ex);
				return null;
			}
		}

		bool MoveNext (TestContext ctx, TestInstance instance, TestResult result)
		{
			var name = TestInstance.GetTestName (instance);
			ctx.Debug (3, "MoveNext({0}): {1} {2}", name, ctx.Print (Host), ctx.Print (instance));

			try {
				return ((ParameterizedTestInstance)instance).MoveNext (ctx);
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
			var parameterizedInstance = SetUp (ctx, instance, result);
			if (parameterizedInstance == null)
				return false;

			bool success = true;
			while (success && parameterizedInstance.HasNext ()) {
				if (cancellationToken.IsCancellationRequested)
					break;

				success = MoveNext (ctx, parameterizedInstance, result);
				if (!success)
					break;

				var name = TestInstance.GetTestName (parameterizedInstance);

				ctx.Debug (5, "InnerInvoke({0}): {1} {2} {3}", name.FullName,
					ctx.Print (Host), ctx.Print (parameterizedInstance), Inner);

				success = await InvokeInner (ctx, parameterizedInstance, result, Inner, cancellationToken);

				ctx.Debug (5, "InnerInvoke({0}) done: {1} {2} {3}", name.FullName,
					ctx.Print (Host), ctx.Print (parameterizedInstance), success);
			}

			return success;
		}
	}
}

