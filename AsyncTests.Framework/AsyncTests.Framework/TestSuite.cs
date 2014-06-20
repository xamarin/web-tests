//
// AsyncTests.Framework.TestSuite
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

namespace AsyncTests.Framework {
	using Internal;
	using Internal.Reflection;

	public class TestSuite {
		List<TestFixture> fixtures;

		TestSuite (string name)
		{
			Name = name;
		}

		public string Name {
			get;
			private set;
		}

		public ITestFilter TestFilter {
			get; set;
		}

		public IList<TestFixture> Fixtures {
			get { return fixtures; }
		}

		public int CountFixtures {
			get { return fixtures.Count; }
		}

		public int CountTests {
			get { return fixtures.Sum (fixture => fixture.CountTests); }
		}

		internal bool Filter (TestCase test)
		{
			if (TestFilter != null)
				return TestFilter.Filter (test);
			return true;
		}

		public static Task<TestSuite> Create (Assembly assembly)
		{
			var tcs = new TaskCompletionSource<TestSuite> ();

			Task.Factory.StartNew (() => {
				try {
					var suite = new TestSuite (assembly.FullName);
					suite.DoResolve (assembly);
					tcs.SetResult (suite);
				} catch (Exception ex) {
					tcs.SetException (ex);
				}
			});

			return tcs.Task;
		}

		void DoResolve (Assembly assembly)
		{
			fixtures = new List<TestFixture> ();
			foreach (var type in assembly.ExportedTypes) {
				var tinfo = type.GetTypeInfo ();
				var attr = tinfo.GetCustomAttribute<AsyncTestFixtureAttribute> (true);
				if (attr == null)
					continue;

				fixtures.Add (new ReflectionTestFixture (this, attr, tinfo));
			}

			foreach (var fixture in fixtures) {
				fixture.Resolve ();
			}
		}

		bool IsEqualOrSubclassOf<T> (TypeInfo type)
		{
			return type.Equals (typeof (T)) || type.IsSubclassOf (typeof (T));
		}

		internal int CurrentIteration {
			get;
			private set;
		}

		internal int MaxIterations {
			get;
			private set;
		}

		public Task<TestResultCollection> Run (CancellationToken cancellationToken)
		{
			return Run (1, cancellationToken);
		}

		public async Task<TestResultCollection> Run (int repeatCount, CancellationToken cancellationToken)
		{
			var result = new TestResultCollection (Name);

			if (repeatCount == 1) {
				CurrentIteration = MaxIterations = 1;
				await DoRun (result, cancellationToken);
			} else {
				MaxIterations = repeatCount;
				for (CurrentIteration = 0; CurrentIteration < repeatCount; CurrentIteration++) {
					var name = string.Format ("{0} (iteration {1})", Name, CurrentIteration + 1);
					var iteration = new TestResultCollection (name);
					result.AddChild (iteration);
					await DoRun (iteration, cancellationToken);
				}
			}

			OnStatusMessageEvent ("Test suite finished.");

			return result;
		}

		async Task DoRun (TestResultCollection result, CancellationToken cancellationToken)
		{
			foreach (var fixture in fixtures) {
				if (TestFilter != null && !TestFilter.Filter (fixture))
					continue;
				try {
					result.AddChild (await fixture.Run (cancellationToken));
				} catch (Exception ex) {
					Log ("Test fixture {0} failed: {1}", fixture.Name, ex);
					result.AddChild (new TestError (fixture.Name, "Test fixture failed", ex));
				}
			}
		}

		protected internal void Log (string message, params object[] args)
		{
			Debug.WriteLine (string.Format (message, args), "TestSuite");
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
