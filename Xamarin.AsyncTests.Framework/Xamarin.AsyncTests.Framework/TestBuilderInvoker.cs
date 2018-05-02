﻿//
// TestBuilderInvoker.cs
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
	sealed class TestBuilderInvoker : AggregatedTestInvoker
	{
		public TestBuilderHost Host {
			get;
		}

		public TestNode Node {
			get;
		}

		public TestInvoker Inner {
			get;
		}

		internal string ME {
			get;
		}

		public TestBuilderInvoker (TestBuilderHost host, TestNode node, TestInvoker inner)
		{
			Host = host;
			Node = node;
			Inner = inner;

			ME = $"TestBuilderInvoker({host.Builder})";
		}

		TestBuilderInstance SetUp (TestContext ctx, TestInstance instance)
		{
			ctx.LogDebug (LogCategory, 10, $"{ME}.SetUp({TestLogger.Print (instance)})");

			try {
				return (TestBuilderInstance)Host.CreateInstance (ctx, Node, instance);
			} catch (OperationCanceledException) {
				ctx.OnTestCanceled ();
				return null;
			} catch (Exception ex) {
				ctx.OnError (ex);
				return null;
			}
		}

		bool TearDown (TestContext ctx, TestBuilderInstance instance)
		{
			ctx.LogDebug (LogCategory, 10, $"{ME}.TearDown({TestLogger.Print (instance)})");

			try {
				instance.Destroy (ctx);
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
			var innerInstance = SetUp (ctx, instance);
			if (innerInstance == null)
				return false;

			var innerPath = innerInstance.GetCurrentPath ();
			var innerCtx = ctx.CreateChild (innerPath);

			ctx.LogDebug (LogCategory, 10, $"{ME}.Invoke({TestLogger.Print (instance)}): {innerInstance}");
			TestInstance.LogDebug (ctx, innerInstance, 10);

			var success = await InvokeInner (innerCtx, innerInstance, Inner, cancellationToken);

			if (!TearDown (ctx, innerInstance))
				success = false;

			return success;
		}
	}
}

