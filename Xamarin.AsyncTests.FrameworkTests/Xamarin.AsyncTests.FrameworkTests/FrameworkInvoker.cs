//
// FrameworkInvoker.cs
//
// Author:
//       Martin Baulig <mabaul@microsoft.com>
//
// Copyright (c) 2018 Xamarin Inc. (http://www.xamarin.com)
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
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace Xamarin.AsyncTests.FrameworkTests
{
	using Framework;
	using TestSuite;
	using Constraints;
	using Console;

	// [Martin ("Test")]
	[AsyncTestFixture]
	public abstract class FrameworkInvoker
	{
		public string PackageName => "FrameworkInvoker";

		public TestFramework Framework {
			get;
		}

		protected FrameworkInvoker ()
		{
			Framework = TestFramework.GetLocalFramework (
				PackageName, Assembly.GetExecutingAssembly ());
		}

		const string Category = "framework-invoker";

		[AsyncTest]
		public async Task Invoke (TestContext ctx, CancellationToken cancellationToken)
		{
			var settings = SettingsBag.CreateDefault ();
			var app = new MyApp (PackageName, ctx.Logger, settings);
			
			var session = TestSession.CreateLocal (app, Framework);

			SetupSession (ctx, settings, session);

			var test = session.RootTestCase;

			ctx.LogDebug (Category, 1, $"RUN: {test}");

			var result = await session.Run (test, cancellationToken).ConfigureAwait (false);

			ctx.LogDebug (Category, 1, $"RUN DONE: {result}");

			var serialized = TestSerializer.WriteTestResult (result);

			ctx.LogDebug (Category, 3, $"RUN DONE #1:\n\n{serialized}\n");

			using (var writer = new StringWriter ()) {
				var printer = new ResultPrinter (writer, result);
				printer.Print ();
				ctx.LogDebug (Category, 3, $"RUN DONE #2\n<<<<<<<<{writer}>>>>>>>>\n");
			}

			ctx.LogDebug (Category, 1, $"RUN DONE #3");

			Visit (ctx, result);

			ctx.LogDebug (Category, 1, $"RUN DONE #4");

			CheckFinalStatus (ctx);

			ctx.LogDebug (Category, 1, $"RUN DONE #5");

			ctx.Assert (result.Status, Is.EqualTo (TestStatus.Success), "success result");
		}

		protected abstract void SetupSession (
			TestContext ctx, SettingsBag settings, TestSession session);

		protected virtual void Visit (TestContext ctx, TestResult result)
		{
			ctx.LogDebug (Category, 2, $"VISIT: {result}");
			if (result.HasChildren) {
				foreach (var child in result.Children) {
					Visit (ctx, child);
				}
			} else {
				VisitResult (ctx, result);
			}
		}

		protected virtual void VisitResult (TestContext ctx, TestResult result)
		{
			ctx.LogDebug (Category, 2, $"VISIT RESULT: {result}");
		}

		protected virtual void CheckFinalStatus (TestContext ctx)
		{
		}

		class MyApp : TestApp
		{
			public string PackageName {
				get;
			}

			public TestLogger Logger {
				get;
			}

			public SettingsBag Settings {
				get;
			}

			public MyApp (string name, TestLogger logger, SettingsBag settings)
			{
				PackageName = name;
				Logger = logger;
				Settings = settings;
			}
		}
	}
}
