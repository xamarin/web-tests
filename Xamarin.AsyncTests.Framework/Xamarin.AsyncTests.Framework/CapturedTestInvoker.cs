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

namespace Xamarin.AsyncTests.Framework
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

		public override async Task<bool> Invoke (
			TestContext ctx, TestInstance instance, TestResult result, CancellationToken cancellationToken)
		{
			var oldName = ctx.CurrentTestName;

			try {
				ctx.CurrentTestName.PushName (Name.Name);
				ctx.Log ("CapturedInvoke({0})", ctx.GetCurrentTestName ());
				var success = await Invoker.Invoke (ctx, null, result, cancellationToken);
				ctx.Log ("CapturedInvoke({0}) done: {1}", ctx.GetCurrentTestName (), success);
				return success;
			} catch (Exception ex) {
				ctx.Log ("CapturedInvoke({0}) failed: {1}", ctx.GetCurrentTestName (), ex);
				result.Error = ex;
				ctx.OnTestError (result);
				return false;
			} finally {
				ctx.CurrentTestName.PopName ();
				ctx.CurrentTestName = oldName;
			}
		}
	}
}

