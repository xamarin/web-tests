//
// TestSession.cs
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

namespace Xamarin.AsyncTests.Framework
{
	using Reflection;

	public abstract class TestSession
	{
		public TestApp App {
			get;
			private set;
		}

		public abstract string Name {
			get;
		}

		public abstract ITestConfigurationProvider ConfigurationProvider {
			get;
		}

		public abstract TestConfiguration Configuration {
			get;
		}

		public abstract TestSuite Suite {
			get;
		}

		public abstract TestCase RootTestCase {
			get;
		}

		public TestSession (TestApp app)
		{
			App = app;
		}

		public static TestSession CreateLocal (TestApp app, TestFramework framework)
		{
			var name = new TestName (framework.Name);
			var config = new TestConfiguration (framework.ConfigurationProvider, app.Settings);
			var rootCtx = new TestContext (app.Settings, app.Logger, config, name);

			return new ReflectionTestSession (app, (ReflectionTestFramework)framework, rootCtx);
		}

		public static TestSession CreateLocal (TestApp app, TestFramework framework, TestContext rootCtx)
		{
			return new ReflectionTestSession (app, (ReflectionTestFramework)framework, rootCtx);
		}

		public abstract Task<TestCase> ResolveFromPath (XElement path, CancellationToken cancellationToken);

		public abstract Task<IReadOnlyCollection<TestCase>> GetTestParameters (TestCase test, CancellationToken cancellationToken);

		public abstract Task<IReadOnlyCollection<TestCase>> GetTestChildren (TestCase test, CancellationToken cancellationToken);

		public abstract Task<TestResult> Run (TestCase test, CancellationToken cancellationToken);

		public abstract Task UpdateSettings (CancellationToken cancellationToken);
	}
}

