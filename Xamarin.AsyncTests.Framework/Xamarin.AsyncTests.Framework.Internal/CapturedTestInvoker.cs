//
// CapturedTestInvoker.cs
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

namespace Xamarin.AsyncTests.Framework.Internal
{
	class CapturedTestInvoker : TestInvoker
	{
		public TestName Name {
			get;
			private set;
		}

		public TestInvoker Invoker {
			get;
			private set;
		}

		public CapturedTestInvoker (TestName name, TestInvoker invoker)
		{
			Name = name;
			Invoker = invoker;
		}

		public override async Task<bool> Invoke (TestContext context, TestResult result, CancellationToken cancellationToken)
		{
			var oldInstance = context.Instance;
			var oldName = context.CurrentTestName;

			try {
				context.Instance = null;
				context.Log ("CAPTURED INVOKE #0: {0} {1}", Name, context.GetCurrentTestName ());
				context.CurrentTestName.PushName (Name.Name);
				// context.CurrentTestName = TestNameBuilder.CreateFromName (Name);

				context.Log ("CAPTURED INVOKE: {0} {1}", result, result.Status);
				var success = await Invoker.Invoke (context, result, cancellationToken);
				context.Log ("CAPTURED INVOKE DONE: {0} {1}", success, result.Status);
				return success;
			} catch (Exception ex) {
				context.Log ("CAPTURED INVOKE FAILED: {0}", ex);
				return false;
			} finally {
				context.CurrentTestName.PopName ();
				context.Instance = oldInstance;
				context.CurrentTestName = oldName;
			}
		}
	}
}

