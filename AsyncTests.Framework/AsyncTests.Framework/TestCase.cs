//
// AsyncTests.Framework.TestCase
//
// Authors:
//      Martin Baulig (martin.baulig@gmail.com)
//
// Copyright 2012 Xamarin Inc. (http://www.xamarin.com)
//
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//
using System;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace AsyncTests.Framework {
	using Internal;

	public abstract class TestCase {
		public abstract TestFlags Flags {
			get;
		}

		public abstract IEnumerable<TestWarning> Warnings {
			get;
		}

		public string Name {
			get;
			private set;
		}

		public abstract bool IsEnabled (string category);

		public abstract TypeInfo ExpectedExceptionType {
			get;
		}

		public TestCase (string name)
		{
			Name = name;
		}

		internal abstract TestInvoker Resolve (TestContext context);

		public Task<TestResult> Invoke (TestContext context, CancellationToken cancellationToken)
		{
			if (ExpectedExceptionType != null)
				return ExpectingException (context, ExpectedExceptionType, cancellationToken);
			else
				return ExpectingSuccess (context, cancellationToken);
		}

		protected abstract object InvokeInner (TestContext context, CancellationToken cancellationToken);

		protected Task<TestResult> ExpectingSuccess (TestContext context, CancellationToken cancellationToken)
		{
			object retval;
			try {
				retval = InvokeInner (context, cancellationToken);
			} catch (Exception ex) {
				return Task.FromResult<TestResult> (new TestError (Name, "Test failed", ex));
			}

			var tresult = retval as Task<TestResult>;
			if (tresult != null)
				return tresult;

			var task = retval as Task;
			if (task == null)
				return Task.FromResult<TestResult> (new TestSuccess (Name));

			return Task.Factory.ContinueWhenAny<TestResult> (new Task[] { task }, t => {
				if (t.IsFaulted)
					return new TestError (Name, "Test failed", t.Exception);
				else if (t.IsCanceled)
					return new TestError (Name, "Test cancelled", t.Exception);
				else if (t.IsCompleted)
					return new TestSuccess (Name);
				throw new InvalidOperationException ();
			});
		}

		protected async Task<TestResult> ExpectingException (TestContext context, TypeInfo expectedException,
			CancellationToken cancellationToken)
		{
			try {
				var retval = InvokeInner (context, cancellationToken);
				var rtask = retval as Task<TestResult>;
				if (rtask != null) {
					var result = await rtask;
					var terror = result as TestError;
					if (terror != null)
						throw terror.Error;
				} else {
					var task = retval as Task;
					if (task != null)
						await task;
				}

				var message = string.Format ("Expected an exception of type {0}", expectedException);
				return new TestError (Name, message, new AssertionException (message));
			} catch (Exception ex) {
				if (ex is TargetInvocationException)
					ex = ((TargetInvocationException)ex).InnerException;
				if (expectedException.IsAssignableFrom (ex.GetType ().GetTypeInfo ()))
					return new TestSuccess (Name);
				var message = string.Format ("Expected an exception of type {0}, but got {1}",
					expectedException, ex.GetType ());
				return new TestError (Name, message, new AssertionException (message, ex));
			}
		}
	}
}
