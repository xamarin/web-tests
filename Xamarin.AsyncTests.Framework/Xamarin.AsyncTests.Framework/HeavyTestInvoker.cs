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
		}

		public TestNode Node {
			get;
		}

		public TestInvoker Inner {
			get;
		}

		static int next_id;
		public readonly int ID = ++next_id;

		public HeavyTestInvoker (HeavyTestHost host, TestNode node, TestInvoker inner, TestFlags? flags)
			: base (flags ?? host.Flags)
		{
			Host = host;
			Node = node;
			Inner = inner;
		}

		async Task<HeavyTestInstance> SetUp (
			TestContext ctx, TestInstance instance, CancellationToken cancellationToken)
		{
			ctx.LogDebug (LogCategory, 10, "SetUp({0}): {1} {2}", ctx.FriendlyName, TestLogger.Print (Host), TestLogger.Print (instance));

			try {
				cancellationToken.ThrowIfCancellationRequested ();
				var childInstance = (HeavyTestInstance)Host.CreateInstance (ctx, Node, instance);
				if (childInstance == null)
					return null;
				childInstance.Initialize (ctx);
				await childInstance.Initialize (ctx, cancellationToken);
				return childInstance;
			} catch (OperationCanceledException) {
				ctx.OnTestCanceled ();
				return null;
			} catch (Exception ex) {
				ctx.OnError (ex);
				return null;
			}
		}

		async Task<bool> TearDown (
			TestContext ctx, HeavyTestInstance instance, CancellationToken cancellationToken)
		{
			ctx.LogDebug (LogCategory, 10, "TearDown({0}): {1} {2}", ctx.FriendlyName, TestLogger.Print (Host), TestLogger.Print (instance));

			try {
				await instance.Destroy (ctx, cancellationToken);
				return true;
			} catch (OperationCanceledException) {
				ctx.OnTestCanceled ();
				return false;
			} catch (Exception ex) {
				ctx.OnError (ex);
				return false;
			}
		}

		public override Task<bool> Invoke (
			TestContext ctx, TestInstance instance, CancellationToken cancellationToken)
		{
			return ctx.RunWithDisposableContext (async disposableCtx => {
				var innerInstance = await SetUp (disposableCtx, instance, cancellationToken);
				if (innerInstance == null)
					return false;

				var currentPath = innerInstance.GetCurrentPath ();

				var innerCtx = disposableCtx.CreateChild (currentPath);

				var success = await InvokeInner (innerCtx, innerInstance, Inner, cancellationToken);

				if (!await TearDown (disposableCtx, innerInstance, cancellationToken))
					success = false;

				return success;
			});
		}
	}
}

