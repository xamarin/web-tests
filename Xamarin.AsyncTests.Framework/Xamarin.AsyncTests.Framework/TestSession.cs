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
	using Portable;

	[Obsolete ("Will probably go away.")]
	public class TestSession
	{
		public DateTime Created {
			get;
			private set;
		}

		public string Name {
			get;
			private set;
		}

		public TestSuite Suite {
			get;
			private set;
		}

		public TestResult Result {
			get;
			private set;
		}

		internal IPortableSupport PortableSupport {
			get;
			private set;
		}

		internal TestLogger Logger {
			get;
			private set;
		}

		public TestContext Context {
			get;
			private set;
		}

		public TestSession (TestApp app, TestSuite suite)
			: this (app, suite, new TestResult (suite.Framework.Name))
		{
		}

		public TestSession (TestSuite suite, IPortableSupport support, TestLogger logger, TestResult result = null)
		{
			Suite = suite;
			Result = result ?? new TestResult (suite.Framework.Name);
			Logger = new TestLogger (TestLoggerBackend.CreateForResult (Result, logger));
			PortableSupport = support;

			Created = DateTime.Now;
			Name = string.Format ("[{0:s}]: {1}", Created, Result.Name.Name);

			Context = CreateContext ();
		}

		public TestSession (TestApp app, TestSuite suite, TestResult result)
		{
			Suite = suite;
			Result = result;
			Logger = new TestLogger (TestLoggerBackend.CreateForResult (Result, app.Logger));
			PortableSupport = app.PortableSupport;

			Created = DateTime.Now;
			Name = string.Format ("[{0:s}]: {1}", Created, Result.Name.Name);

			Context = CreateContext ();
		}

		TestContext CreateContext ()
		{
			return new TestContext (PortableSupport, Logger, Suite, Result.Name, Result);
		}

		public async Task<TestResult> Run (TestCase test, CancellationToken cancellationToken)
		{
			try {
				await test.Run (Context, cancellationToken).ConfigureAwait (false);
			} catch (OperationCanceledException) {
				Result.Status = TestStatus.Canceled;
			} catch (Exception ex) {
				Result.AddError (ex);
			}

			return Result;
		}
	}
}

