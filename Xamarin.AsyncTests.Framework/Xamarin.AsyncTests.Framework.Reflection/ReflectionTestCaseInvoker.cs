//
// ReflectionTestCaseInvoker.cs
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
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace Xamarin.AsyncTests.Framework.Reflection
{
	class ReflectionTestCaseInvoker : TestInvoker
	{
		public ReflectionTestCaseBuilder Builder {
			get;
			private set;
		}

		public ReflectionTestCaseInvoker (ReflectionTestCaseBuilder builder)
		{
			Builder = builder;
		}

		DateTime startTime;

		public override Task<bool> Invoke (TestContext ctx, TestInstance instance, CancellationToken cancellationToken)
		{
			startTime = DateTime.Now;

			ctx.OnTestRunning ();

			if (Builder.ExpectedExceptionType != null)
				return ExpectingException (ctx, instance, Builder.ExpectedExceptionType, cancellationToken);
			return ExpectingSuccess (ctx, instance, cancellationToken);
		}

		bool CheckFinalStatus (TestContext ctx)
		{
			var elapsedTime = DateTime.Now - startTime;

			if (ctx.HasPendingException) {
				if (Builder.Attribute.Unstable)
					ctx.OnTestFinished (TestStatus.Unstable, elapsedTime);
				else
					ctx.OnTestFinished (TestStatus.Error, elapsedTime);
				return false;
			}
			if (ctx.IsCanceled)
				return false;
			ctx.OnTestFinished (TestStatus.Success, elapsedTime);
			return true;
		}

		[StackTraceEntryPoint]
		async Task<bool> ExpectingSuccess (TestContext ctx, TestInstance instance, CancellationToken cancellationToken)
		{
			object retval;
			try {
				retval = ReflectionMethodInvoker.InvokeMethod (
					ctx, Builder, instance, Builder.Method,
					false, cancellationToken);
			} catch (Exception ex) {
				ctx.OnTestFinished (TestStatus.Error, DateTime.Now - startTime);
				ctx.OnError (ex);
				return false;
			}

			var task = retval as Task;
			if (task == null)
				return CheckFinalStatus (ctx);

			try {
				await task;

				return CheckFinalStatus (ctx);
			} catch (OperationCanceledException) {
				ctx.OnTestCanceled ();
				return false;
			} catch (Exception ex) {
				ctx.OnTestFinished (TestStatus.Error, DateTime.Now - startTime);
				ctx.OnError (ex);
				return false;
			}
		}

		[StackTraceEntryPoint]
		async Task<bool> ExpectingException (
			TestContext ctx, TestInstance instance,
			TypeInfo expectedException, CancellationToken cancellationToken)
		{
			try {
				var retval = ReflectionMethodInvoker.InvokeMethod (
					ctx, Builder, instance, Builder.Method,
					true, cancellationToken);
				var task = retval as Task;
				if (task != null)
					await task;

				var message = string.Format ("Expected an exception of type {0}", expectedException);
				ctx.OnError (new AssertionException (message, ctx.GetStackTrace ()));
				return false;
			} catch (Exception ex) {
				if (ex is TargetInvocationException)
					ex = ((TargetInvocationException)ex).InnerException;
				if (expectedException.IsAssignableFrom (ex.GetType ().GetTypeInfo ())) {
					var elapsedTime = DateTime.Now - startTime;
					ctx.OnTestFinished (TestStatus.Success, elapsedTime);
					return true;
				}
				var message = string.Format ("Expected an exception of type {0}, but got {1}",
					expectedException, ex.GetType ());
				ctx.OnError (new AssertionException (message, ex, ctx.GetStackTrace (ex)));
				return false;
			}
		}
	}
}

