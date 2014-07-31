//
// ResultGroupTestInvoker.cs
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
	class ResultGroupTestInvoker : AggregatedTestInvoker
	{
		public TestInvoker Inner {
			get;
			private set;
		}

		public ResultGroupTestInvoker (TestInvoker inner)
		{
			Inner = inner;
		}

		public override async Task<bool> Invoke (
			TestContext ctx, TestInstance instance, TestResult result, CancellationToken cancellationToken)
		{
			var innerResult = new TestResult (TestInstance.GetTestName (instance));

			if (instance != null)
				innerResult.Test = CaptureContext (innerResult.Name, instance);

			try {
				return await InvokeInner (ctx, instance, innerResult, Inner, cancellationToken);
			} finally {
				result.AddChild (innerResult);
			}
		}

		TestCase CaptureContext (TestName name, TestInstance instance)
		{
			TestInvoker invoker = new ResultGroupTestInvoker (Inner);
			invoker = new PrePostRunTestInvoker (invoker);
			invoker = CaptureContext (name, instance, invoker);
			if (invoker == null)
				return null;

			return new CapturedTestCase (name, invoker);
		}

		static TestInvoker CaptureContext (TestName name, TestInstance instance, TestInvoker invoker)
		{
			if (instance.Host is CapturedTestHost)
				return null;

			var capturedHost = instance.CaptureContext ();
			invoker = capturedHost.CreateInvoker (invoker);

			if (instance.Parent != null)
				return CaptureContext (name, instance.Parent, invoker);

			return invoker;
		}
	}
}
