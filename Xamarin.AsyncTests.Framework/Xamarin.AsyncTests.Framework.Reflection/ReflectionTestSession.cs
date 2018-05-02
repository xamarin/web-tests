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
	using Remoting;

	sealed class ReflectionTestSession : TestSession
	{
		public override string Name {
			get;
		}

		public override ITestConfigurationProvider ConfigurationProvider {
			get;
		}

		public override TestConfiguration Configuration {
			get;
		}

		public override TestSuite Suite {
			get;
		}

		public override TestCase RootTestCase => Suite.RootTestCase;

		internal TestContext RootContext {
			get;
		}

		internal Connection RemoteConnection {
			get;
		}

		TaskCompletionSource<object> shutdownTcs;

		public ReflectionTestSession (TestApp app, ReflectionTestFramework framework, TestContext rootCtx)
			: base (app)
		{
			RootContext = rootCtx;
			ConfigurationProvider = framework.ConfigurationProvider;
			Configuration = new TestConfiguration (ConfigurationProvider, app.Settings);
			Name = framework.Name;
			Suite = new ReflectionTestSuite (this, framework);
			shutdownTcs = new TaskCompletionSource<object> ();
		}

		internal ReflectionTestSession (TestApp app, ReflectionTestFramework framework, TestContext rootCtx, Connection connection)
			: this (app, framework, rootCtx)
		{
			RemoteConnection = connection;
		}

		public override Task<TestCase> ResolveFromPath (XElement node, CancellationToken cancellationToken)
		{
			return Task.Run<TestCase> (() => {
				var path = TestSerializer.DeserializePath (Suite, RootContext, node);
				return new ReflectionTestCase (path);
			});
		}

		public override Task<IReadOnlyCollection<TestCase>> GetTestParameters (TestCase test, CancellationToken cancellationToken)
		{
			return ((ReflectionTestCase)test).GetParameters (this, cancellationToken);
		}

		public override Task<IReadOnlyCollection<TestCase>> GetTestChildren (TestCase test, CancellationToken cancellationToken)
		{
			return test.GetChildren (cancellationToken);
		}

		public override Task<TestResult> Run (TestCase test, CancellationToken cancellationToken)
		{
			return ((ReflectionTestCase)test).Run (this, cancellationToken);
		}

		public override Task UpdateSettings (CancellationToken cancellationToken)
		{
			return Task.Run (() => {
				Configuration.Reload ();
				OnConfigurationChanged ();
			});
		}

		public override Task WaitForShutdown (CancellationToken cancellationToken)
		{
			return shutdownTcs.Task;
		}

		public override Task Shutdown (CancellationToken cancellationToken)
		{
			shutdownTcs.TrySetResult (null);
			return Task.FromResult<object> (null);
		}
	}
}

