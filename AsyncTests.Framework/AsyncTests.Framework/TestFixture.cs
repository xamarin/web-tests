//
// AsyncTests.Framework.TestFixture
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
using System.Reflection;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace AsyncTests.Framework
{
	using Internal;

	public abstract class TestFixture
	{
		public TestSuite Suite {
			get;
			private set;
		}

		public AsyncTestFixtureAttribute Attribute {
			get;
			private set;
		}

		public abstract IList<string> Categories {
			get;
		}

		public abstract IList<TestWarning> Warnings {
			get;
		}

		public TypeInfo Type {
			get;
			private set;
		}

		public string Name {
			get { return Type.Name; }
		}

		public abstract int CountTests {
			get;
		}

		public abstract TestCase[] Tests {
			get;
		}

		internal TestFixture (TestSuite suite, AsyncTestFixtureAttribute attr, TypeInfo type)
		{
			this.Suite = suite;
			this.Attribute = attr;
			this.Type = type;
		}

		public abstract bool Resolve ();

		void AddWarnings (TestResultCollection result)
		{
			result.AddWarnings (Warnings);
			foreach (var test in Tests) {
				result.AddWarnings (test.Warnings);
			}
		}

		public Task<TestResult> Run (CancellationToken cancellationToken)
		{
			var context = new TestContext (Suite);
			return Run (context, cancellationToken);
		}

		public abstract Task<TestResult> Run (TestContext context, CancellationToken cancellationToken);

		internal abstract Task InitializeInstance (TestContext context, CancellationToken cancellationToken);

		internal abstract Task DestroyInstance (TestContext context, CancellationToken cancellationToken);
	}
}
