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

namespace Xamarin.AsyncTests.Framework {
	using Internal;
	using Internal.Reflection;

	public class TestSuite {
		TestCaseCollection tests;

		public TestSuite ()
		{
			tests = new TestCaseCollection ();
		}

		public IList<TestCase> Tests {
			get { return tests.Tests; }
		}

		public int CountFixtures {
			get { return tests.Count; }
		}

		public Task<TestCaseCollection> LoadAssembly (Assembly assembly)
		{
			var tcs = new TaskCompletionSource<TestCaseCollection> ();

			Task.Factory.StartNew (() => {
				try {
					var collection = DoLoadAssembly (assembly);
					tests.Add (collection);
					tcs.SetResult (collection);
				} catch (Exception ex) {
					tcs.SetException (ex);
				}
			});

			return tcs.Task;
		}

		TestCaseCollection DoLoadAssembly (Assembly assembly)
		{
			var collection = new TestCaseCollection (assembly.GetName ().Name);

			foreach (var type in assembly.ExportedTypes) {
				var tinfo = type.GetTypeInfo ();
				var attr = tinfo.GetCustomAttribute<AsyncTestFixtureAttribute> (true);
				if (attr == null)
					continue;

				var fixture = new ReflectionTestFixture (this, attr, tinfo);
				fixture.Resolve ();
				collection.Add (fixture);
			}

			return collection;
		}

		bool IsEqualOrSubclassOf<T> (TypeInfo type)
		{
			return type.Equals (typeof (T)) || type.IsSubclassOf (typeof (T));
		}

		public TestCase Resolve (TestContext context)
		{
			var resolved = tests.Resolve (context);
			if (context.Repeat != 0)
				resolved = resolved.CreateRepeatedTest (context, context.Repeat);
			return resolved;
		}

		public Task Run (TestContext context, TestResult result, CancellationToken cancellationToken)
		{
			var resolved = Resolve (context);
			return resolved.Run (context, result, cancellationToken);
		}
	}
}
