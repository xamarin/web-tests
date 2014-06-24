//
// Xamarin.AsyncTests.Framework.TestCase
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

namespace Xamarin.AsyncTests.Framework {
	using Internal;

	public abstract class TestCase
	{
		public abstract IEnumerable<string> Categories {
			get;
		}

		public string Name {
			get;
			private set;
		}

		public bool IsEnabled (string category)
		{
			return Categories.Contains (category);
		}

		public TestCase (string name)
		{
			Name = name;
		}

		public virtual TestCase Resolve (TestContext context)
		{
			var invoker = CreateInvoker (context);
			return new InvokableTestCase (this, invoker);
		}

		internal abstract TestInvoker CreateInvoker (TestContext context);

		public abstract Task<bool> Run (TestContext context, TestResult result,
			CancellationToken cancellationToken);

		public TestCase CreateRepeatedTest (TestContext context, int count)
		{
			var invoker = CreateInvoker (context);
			var repeatHost = new RepeatedTestHost (count, TestFlags.ContinueOnError | TestFlags.Browsable, "$iteration");
			var repeatInvoker = new AggregatedTestInvoker (repeatHost, invoker);
			var outerInvoker = new ProxyTestInvoker ("Iteration", repeatInvoker);
			return new InvokableTestCase (this, outerInvoker);
		}
	}
}
