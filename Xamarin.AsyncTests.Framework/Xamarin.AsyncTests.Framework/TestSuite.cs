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

		public TestSuite (string name)
		{
			Name = name;

			tests = new TestCaseCollection (name);
		}

		public string Name {
			get;
			private set;
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

		public async Task<TestResultCollection> Run (TestContext context, CancellationToken cancellationToken)
		{
			var result = new TestResultCollection (Name);

			if (context.Repeat == 0) {
				await Run (context, result, cancellationToken);
			} else {
				for (int iteration = 0; iteration < context.Repeat; iteration++) {
					var name = string.Format ("{0} (iteration {1})", Name, iteration + 1);
					var child = new TestResultCollection (name);
					result.AddChild (child);
					await Run (context, child, cancellationToken);
				}
			}

			OnStatusMessageEvent ("Test suite finished.");

			return result;
		}

		async Task Run (TestContext context, TestResultCollection result, CancellationToken cancellationToken)
		{
			foreach (var fixture in tests.Tests) {
				if (!context.Filter (fixture))
					continue;
				try {
					result.AddChild (await fixture.Run (context, cancellationToken));
				} catch (Exception ex) {
					context.Log ("Test fixture {0} failed: {1}", fixture.Name, ex);
					result.AddChild (new TestError ("Test fixture failed", ex));
				}
			}
		}

		public event EventHandler<StatusMessageEventArgs> StatusMessageEvent;

		protected internal void OnStatusMessageEvent (string message, params object[] args)
		{
			OnStatusMessageEvent (new StatusMessageEventArgs (string.Format (message, args)));
		}

		protected void OnStatusMessageEvent (StatusMessageEventArgs args)
		{
			if (StatusMessageEvent != null)
				StatusMessageEvent (this, args);
		}

		public class StatusMessageEventArgs : EventArgs {
			public string Message {
				get;
				private set;
			}

			public Exception Error {
				get;
				private set;
			}

			public StatusMessageEventArgs (string message)
			{
				this.Message = message;
			}

			public StatusMessageEventArgs (string message, Exception error)
			{
				this.Message = message;
				this.Error = error;
			}
		}
	}
}
