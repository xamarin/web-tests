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
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Xamarin.AsyncTests.FrameworkTests
{
	using Framework;
	using TestSuite;
	using Constraints;
	using Console;

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

		protected virtual bool ForwardLogging => true;

		[AsyncTest]
		public async Task Invoke (TestContext ctx, CancellationToken cancellationToken)
		{
			var settings = SettingsBag.CreateDefault ();
			settings.Merge (ctx.Settings);

			var logger = new FrameworkLogger (ForwardLogging ? ctx.Logger : null);
			var app = new MyApp (PackageName, logger, settings);
			
			var session = TestSession.CreateLocal (app, Framework);

			SetupSession (ctx, settings, session);

			await session.UpdateSettings (cancellationToken).ConfigureAwait (false);

			var test = session.RootTestCase;

			ctx.LogDebug (Category, 1, $"RUN: {session} {test}");

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

			CheckLogging (ctx, logger);

			ctx.LogDebug (Category, 1, $"RUN DONE #6");

			ctx.Assert (result.Status, Is.EqualTo (ExpectedStatus), "expected status");
		}

		protected virtual TestStatus ExpectedStatus => TestStatus.Success;

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

		protected virtual void CheckLogging (TestContext ctx, FrameworkLogger logger)
		{
			
		}

		protected void CheckStartAndFinish (TestContext ctx, FrameworkLogger logger)
		{
			var filteredEvents = logger.Events.Where (
				e => e.Kind == TestLoggerBackend.EntryKind.Error);
			ctx.Assert (filteredEvents.Count, Is.EqualTo (0));
			ctx.Assert (logger.Statistics.Count, Is.EqualTo (2));
			ctx.Assert (logger.Statistics[0].Type,
				    Is.EqualTo (TestLoggerBackend.StatisticsEventType.Running));
			ctx.Assert (logger.Statistics[0].Status,
				    Is.EqualTo (TestStatus.Success));
			ctx.Assert (logger.Statistics[1].Type,
				    Is.EqualTo (TestLoggerBackend.StatisticsEventType.Finished));
			ctx.Assert (logger.Statistics[1].Status,
				    Is.EqualTo (TestStatus.Success));
		}

		protected void CheckStartAndFinishWithError (TestContext ctx, FrameworkLogger logger)
		{
			var filteredEvents = logger.Events.Where (
				e => e.Kind == TestLoggerBackend.EntryKind.Error);
			ctx.Assert (filteredEvents.Count, Is.EqualTo (1));
			ctx.Assert (logger.Statistics.Count, Is.EqualTo (2));
			ctx.Assert (logger.Statistics[0].Type,
				    Is.EqualTo (TestLoggerBackend.StatisticsEventType.Running));
			ctx.Assert (logger.Statistics[0].Status,
				    Is.EqualTo (TestStatus.Success));
			ctx.Assert (logger.Statistics[1].Type,
				    Is.EqualTo (TestLoggerBackend.StatisticsEventType.Finished));
			ctx.Assert (logger.Statistics[1].Status,
			            Is.EqualTo (TestStatus.Error));
		}

		class MyApp : TestApp
		{
			public string PackageName {
				get;
			}

			public FrameworkLogger LoggerBackend {
				get;
			}

			public TestLogger Logger {
				get;
			}

			public SettingsBag Settings {
				get;
			}

			public MyApp (string name, FrameworkLogger logger, SettingsBag settings)
			{
				PackageName = name;
				LoggerBackend = logger;
				Logger = new TestLogger (logger);
				Settings = settings;
			}
		}

		protected class FrameworkLogger : TestLoggerBackend
		{
			public TestLogger Parent {
				get;
			}

			public List<LogEntry> Events {
				get;
			}

			public List<StatisticsEventArgs> Statistics {
				get;
			}

			public FrameworkLogger (TestLogger parent)
			{
				Parent = parent;
				Events = new List<LogEntry> ();
				Statistics = new List<StatisticsEventArgs> ();
			}

			protected override void OnLogEvent (LogEntry entry)
			{
				Events.Add (entry);
				Parent?.LogEvent (entry);
			}

			protected override void OnStatisticsEvent (StatisticsEventArgs args)
			{
				Statistics.Add (args);
				Parent?.LogStatisticsEvent (args);
			}
		}
	}
}
