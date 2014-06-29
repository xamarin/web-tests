//
// CaptureContextTestInvoker.cs
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
	class CaptureContextTestInvoker : AggregatedTestInvoker
	{
		public TestHost Host {
			get;
			private set;
		}

		public TestInvoker Inner {
			get;
			private set;
		}

		public CaptureContextTestInvoker (TestHost host, TestInvoker inner)
		{
			Host = host;
			Inner = inner;
		}

		TestCase CaptureContext (TestContext ctx, TestInstance instance, TestInvoker invoker)
		{
			if (ctx.CurrentTestName.IsCaptured || Host is CapturedTestHost || instance == null)
				return null;

			var capture = CaptureContext (ctx.GetCurrentTestName (), instance, invoker);
			ctx.Debug (5, "CaptureContext({0}): {1} {2} {3} -> {4}", ctx.GetCurrentTestName (),
				ctx.Print (instance), ctx.Print (Host), invoker, ctx.Print (capture));
			if (capture == null)
				return null;

			return new CapturedTestCase (new CapturedTestInvoker (ctx.GetCurrentTestName (), capture));
		}

		static TestInvoker CaptureContext (TestName name, TestInstance instance, TestInvoker invoker)
		{
			var parameterizedInstance = instance as ParameterizedTestInstance;
			if (parameterizedInstance != null) {
				var host = new CapturedTestHost (name, parameterizedInstance.Host, parameterizedInstance.Current);
				invoker = AggregatedTestInvoker.Create (host, invoker);
			}

			if (instance.Parent != null)
				return CaptureContext (name, instance.Parent, invoker);

			return AggregatedTestInvoker.Create (instance.Host, invoker);
		}

		public override async Task<bool> Invoke (
			TestContext ctx, TestInstance instance, TestResult result, CancellationToken cancellationToken)
		{
			var capturedTest = CaptureContext (ctx, instance, Inner);
			if (capturedTest != null)
				ctx.CurrentTestName.PushCapture (capturedTest);

			try {
				return await InvokeInner (ctx, instance, result, Inner, cancellationToken);
			} finally {
				if (capturedTest != null)
					ctx.CurrentTestName.PopCapture ();
			}
		}
	}
}

