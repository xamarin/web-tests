//
// Xamarin.AsyncTests.Framework.TestSuite
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
using System.IO;
using System.Linq;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Reflection;
using System.Collections.Generic;

namespace Xamarin.AsyncTests.Framework
{
	using Reflection;

	public abstract class TestSuite : TestCase, ITestSuite
	{
		public TestSuite (TestName name)
			: base (name)
		{
		}

		TestCase ITestSuite.Test {
			get { return this; }
		}

		public static Task<TestSuite> LoadAssembly (TestApp ctx, Assembly assembly)
		{
			return ReflectionTestSuite.Create (ctx, assembly);
		}

		public static TestCase CreateRepeatedTest (TestCase test, int count)
		{
			var repeatHost = new RepeatedTestHost (count, TestFlags.ContinueOnError | TestFlags.Browsable, "$iteration");
			var invoker = AggregatedTestInvoker.Create (repeatHost, new TestCaseInvoker (test));
			invoker = NamedTestInvoker.Create (test.Name, invoker);
			return new InvokableTestCase (test, invoker);
		}

		public static TestCase CreateProxy (TestCase test, TestName proxy)
		{
			var invoker = NamedTestInvoker.Create (proxy, new TestCaseInvoker (test));
			return new InvokableTestCase (test, invoker);
		}

		class TestCaseInvoker : TestInvoker
		{
			public TestCase Test {
				get;
				private set;
			}

			public TestCaseInvoker (TestCase test)
			{
				Test = test;
			}

			public override Task<bool> Invoke (
				TestContext ctx, TestInstance instance, CancellationToken cancellationToken)
			{
				while (instance != null) {
					if (!(instance.Host is RepeatedTestHost || instance.Host is NamedTestHost))
						throw new InvalidOperationException ();
					instance = instance.Parent;
				}

				return Test.Run (ctx, cancellationToken);
			}
		}
	}
}
