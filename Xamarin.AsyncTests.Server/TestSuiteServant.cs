//
// TestSuiteServant.cs
//
// Author:
//       Martin Baulig <martin.baulig@xamarin.com>
//
// Copyright (c) 2015 Xamarin, Inc.
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

namespace Xamarin.AsyncTests.Server
{
	using Framework;

	class TestSuiteServant : ObjectServant, RemoteTestSuite
	{
		public TestFrameworkServant Framework {
			get;
			private set;
		}

		public TestSuite Suite {
			get;
			private set;
		}

		public TestSession Session {
			get;
			private set;
		}

		public override string Type {
			get { return "TestSuite"; }
		}

		public TestSuiteServant (ServerConnection connection, TestFrameworkServant framework)
			: base (connection)
		{
			Framework = framework;
		}

		public async Task Initialize (TestLogger logger, CancellationToken cancellationToken)
		{
			cancellationToken.ThrowIfCancellationRequested ();
			Suite = await Framework.LoadTestSuite (cancellationToken);

			Session = new TestSession (Suite, Framework.LocalFramework.PortableSupport, logger);

			Session.Context.LogMessage ("Hello from Server!");
		}

		public Task<TestCase> Resolve (CancellationToken cancellationToken)
		{
			return Suite.Resolve (Session.Context, cancellationToken);
		}

		TestSuiteClient RemoteObject<TestSuiteClient,TestSuiteServant>.Client {
			get { throw new ServerErrorException (); }
		}

		TestSuiteServant RemoteObject<TestSuiteClient,TestSuiteServant>.Servant {
			get { return this; }
		}
	}
}

