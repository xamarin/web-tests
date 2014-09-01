//
// TestSession.cs
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
using System.Threading;
using System.Threading.Tasks;

namespace Xamarin.AsyncTests.Framework
{
	public class TestSession : ITestSession
	{
		public DateTime Created {
			get;
			private set;
		}

		public string Name {
			get;
			private set;
		}

		public TestCase Test {
			get;
			private set;
		}

		public ITestConfiguration Configuration {
			get;
			private set;
		}

		public TestResult Result {
			get;
			private set;
		}

		internal TestApp App {
			get;
			private set;
		}

		internal TestLogger Logger {
			get;
			private set;
		}

		public TestSession (TestApp app, TestCase test)
			: this (app, test, new TestResult (test.Name))
		{
		}

		public TestSession (TestApp app, TestCase test, TestResult result)
		{
			App = app;
			Test = test;
			Result = result;
			Logger = new TestLogger (TestLoggerBackend.CreateForResult (Result, app.Logger));
			Configuration = app.Configuration.AsReadOnly ();

			Created = DateTime.Now;
			Name = string.Format ("[{0:s}]: {1}", Created, Result.Name.Name);
		}

		TestContext CreateContext ()
		{
			return new TestContext (
				App.PortableSupport, Configuration, Logger, Result.Name, Result);
		}

		public Task<TestResult> Run (CancellationToken cancellationToken)
		{
			return Run (Test, cancellationToken);
		}

		public Task<TestResult> Repeat (int count, CancellationToken cancellationToken)
		{
			var repeatedTest = TestSuite.CreateRepeatedTest (Test, count);
			return Run (repeatedTest, cancellationToken);
		}

		async Task<TestResult> Run (TestCase test, CancellationToken cancellationToken)
		{
			var ctx = CreateContext ();

			try {
				await test.Run (ctx, cancellationToken).ConfigureAwait (false);
			} catch (OperationCanceledException) {
				Result.Status = TestStatus.Canceled;
			} catch (Exception ex) {
				Result.AddError (ex);
			}

			return Result;
		}
	}
}

