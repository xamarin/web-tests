//
// PrePostRunTestInvoker.cs
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
using System.Threading;
using System.Threading.Tasks;

namespace Xamarin.AsyncTests.Framework
{
	class PrePostRunTestInvoker : AggregatedTestInvoker
	{
		public TestInvoker Inner {
			get;
			private set;
		}

		public PrePostRunTestInvoker (TestInvoker inner)
		{
			Inner = inner;
		}

		async Task<bool> PreRun (
			TestContext ctx, TestInstance instance, CancellationToken cancellationToken)
		{
			ctx.LogDebug (10, "PreRun({0}): {1}", ctx.Name, TestLogger.Print (instance));

			try {
				for (var current = instance; current != null; current = current.Parent) {
					cancellationToken.ThrowIfCancellationRequested ();
					var heavy = current as HeavyTestInstance;
					if (heavy != null)
						await heavy.PreRun (ctx, cancellationToken);
				}
				return true;
			} catch (OperationCanceledException) {
				ctx.OnTestCanceled ();
				return false;
			} catch (Exception ex) {
				ctx.OnError (ex);
				return false;
			}
		}

		async Task<bool> PostRun (
			TestContext ctx, TestInstance instance, CancellationToken cancellationToken)
		{
			ctx.LogDebug (10, "PostRun({0}): {1}", ctx.Name, TestLogger.Print (instance));

			try {
				for (var current = instance; current != null; current = current.Parent) {
					var heavy = current as HeavyTestInstance;
					if (heavy != null)
						await heavy.PostRun (ctx, cancellationToken);
				}
				return true;
			} catch (OperationCanceledException) {
				ctx.OnTestCanceled ();
				return false;
			} catch (Exception ex) {
				ctx.OnError (ex);
				return false;
			}
		}

		public override async Task<bool> Invoke (
			TestContext ctx, TestInstance instance, CancellationToken cancellationToken)
		{
			if (!await PreRun (ctx, instance, cancellationToken))
				return false;

			var success = await InvokeInner (ctx, instance, Inner, cancellationToken);

			if (!await PostRun (ctx, instance, cancellationToken))
				success = false;

			return success;
		}
	}
}

