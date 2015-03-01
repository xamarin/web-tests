//
// ReflectionTestSession.cs
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
using System.Xml.Linq;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Xamarin.AsyncTests.Framework.Reflection
{
	class ReflectionTestSession : TestSession
	{
		new public ReflectionTestFramework Framework {
			get { return (ReflectionTestFramework)base.Framework; }
		}

		public override TestSuite Suite {
			get { return suite; }
		}

		internal TestContext RootContext {
			get;
			private set;
		}

		ReflectionTestSuite suite;
		TestCase rootTest;

		public ReflectionTestSession (TestApp app, ReflectionTestFramework framework, TestContext rootCtx)
			: base (app, framework)
		{
			RootContext = rootCtx;
			suite = new ReflectionTestSuite (framework);
			rootTest = new ReflectionTestCase (suite.RootPath);
		}

		public override Task<TestCase> GetRootTestCase (CancellationToken cancellationToken)
		{
			return Task.FromResult (rootTest);
		}

		public override Task<TestCase> ResolveFromPath (XElement node, CancellationToken cancellationToken)
		{
			return Task.Run<TestCase> (() => {
				var path = TestSerializer.DeserializePath (suite, RootContext, node);
				return new ReflectionTestCase (path);
			});
		}

		public override Task<IReadOnlyCollection<TestCase>> GetTestParameters (TestCase test, CancellationToken cancellationToken)
		{
			return test.GetParameters (RootContext, cancellationToken);
		}

		public override Task<IReadOnlyCollection<TestCase>> GetTestChildren (TestCase test, CancellationToken cancellationToken)
		{
			return test.GetChildren (cancellationToken);
		}

		public override Task<TestResult> Run (TestCase test, CancellationToken cancellationToken)
		{
			return test.Run (RootContext, cancellationToken);
		}
	}
}

