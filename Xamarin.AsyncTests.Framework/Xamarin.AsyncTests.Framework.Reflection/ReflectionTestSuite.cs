//
// ReflectionTestSuite.cs
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
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Xamarin.AsyncTests.Framework.Reflection
{
	class ReflectionTestSuite : TestSuite
	{
		public Assembly Assembly {
			get;
			private set;
		}

		TestCase test;
		List<ReflectionTestFixtureBuilder> fixtures;

		ReflectionTestSuite (TestName name, Assembly assembly)
			: base (name)
		{
			Assembly = assembly;
		}

		public override TestCase Test {
			get { return test; }
		}

		public static Task<TestSuite> Create (TestApp ctx, Assembly assembly)
		{
			var tcs = new TaskCompletionSource<TestSuite> ();

			Task.Factory.StartNew (() => {
				try {
					var name = new TestName (assembly.GetName ().Name);
					var suite = new ReflectionTestSuite (name, assembly);
					suite.ResolveMembers ();
					ctx.CurrentTestSuite = suite;
					tcs.SetResult (suite);
				} catch (Exception ex) {
					tcs.SetException (ex);
				}
			});

			return tcs.Task;
		}

		void ResolveMembers ()
		{
			fixtures = new List<ReflectionTestFixtureBuilder> ();

			foreach (var type in Assembly.ExportedTypes) {
				var tinfo = type.GetTypeInfo ();
				var attr = tinfo.GetCustomAttribute<AsyncTestFixtureAttribute> (true);
				if (attr == null)
					continue;

				fixtures.Add (new ReflectionTestFixtureBuilder (this, attr, tinfo));
			}

			var invokers = fixtures.Select (f => f.Invoker).ToArray ();
			var suiteInvoker = AggregatedTestInvoker.Create (TestFlags.ContinueOnError, invokers);

			test = new InvokableTestCase (this, suiteInvoker);
		}

		public ReflectionTestFixtureBuilder FindFixture (string name)
		{
			return fixtures.Find (f => f.FullName.Equals (name));
		}
	}
}

