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

		async Task<bool> MoveNext (
			TestContext ctx, TestInstance instance, TestResult result, CancellationToken cancellationToken)
		{
			ctx.Debug (3, "MoveNext({0}): {1} {2}", ctx.GetCurrentTestName ().FullName,
				ctx.Print (Host), ctx.Print (instance));

			try {
				ctx.CurrentTestName.PushName ("MoveNext");
				cancellationToken.ThrowIfCancellationRequested ();
				await ((ParameterizedTestInstance)instance).MoveNext (ctx, cancellationToken);
				return true;
			} catch (OperationCanceledException) {
				result.Status = TestStatus.Canceled;
				return false;
			} catch (Exception ex) {
				var error = ctx.CreateTestResult (ex);
				result.AddChild (error);
				return false;
			} finally {
				ctx.CurrentTestName.PopName ();
			}
		}

		public override async Task<bool> Invoke (
			TestContext ctx, TestInstance instance, TestResult result, CancellationToken cancellationToken)
		{
			var parameterizedInstance = (ParameterizedTestInstance)instance;

			bool success = true;
			while (success && parameterizedInstance.HasNext ()) {
				if (cancellationToken.IsCancellationRequested)
					break;

				success = await MoveNext (ctx, instance, result, cancellationToken);
				if (!success)
					break;

				if (!IsHidden)
					ctx.CurrentTestName.PushParameter (Host.ParameterName, parameterizedInstance.Current);

				ctx.Debug (5, "InnerInvoke({0}): {1} {2} {3}", ctx.GetCurrentTestName ().FullName,
					ctx.Print (Host), ctx.Print (instance), Inner);

				success = await InvokeInner (ctx, instance, result, Inner, cancellationToken);

				ctx.Debug (5, "InnerInvoke({0}) done: {1} {2} {3}", ctx.GetCurrentTestName ().FullName,
					ctx.Print (Host), ctx.Print (instance), success);

				if (!IsHidden)
					ctx.CurrentTestName.PopParameter ();
			}

			return success;
		}
	}
}

