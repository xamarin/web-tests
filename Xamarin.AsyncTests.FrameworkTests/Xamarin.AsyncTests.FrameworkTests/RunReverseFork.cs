//
// RunReverseFork.cs
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
using System.Linq;
using System.Collections.Generic;
using Xamarin.AsyncTests.Framework;
using Xamarin.AsyncTests.Constraints;

namespace Xamarin.AsyncTests.FrameworkTests
{
	[ForkedSupport]
	public class RunReverseFork : FrameworkInvoker
	{
		protected override void SetupSession (
			TestContext ctx, SettingsBag settings, TestSession session)
		{
			session.Configuration.CurrentCategory = TestCategory.Martin;
			session.Configuration.SetIsEnabled (TestFeature.ForkedSupport, true);
			settings.MartinTest = "FrameworkTests.ReverseFork";
		}

		bool hasResult;

		protected override bool ForwardLogging => false;

		protected override void VisitResult (TestContext ctx, TestResult result)
		{
			base.VisitResult (ctx, result);
			ctx.Assert (hasResult, Is.False);
			hasResult = true;
			ctx.Assert (result.Status, Is.EqualTo (TestStatus.Success));
			ctx.Assert (result.Path, Is.Not.Null);
			ctx.Assert (result.Path.Node.PathType, Is.EqualTo (TestPathType.Fork));
		}

		protected override void CheckFinalStatus (TestContext ctx)
		{
			ctx.Assert (hasResult);
			base.CheckFinalStatus (ctx);
		}

		protected override void CheckLogging (TestContext ctx, FrameworkLogger logger)
		{
			var filteredEvents = logger.Events.Where (
				e => e.Kind == TestLoggerBackend.EntryKind.Error);
			ctx.Assert (filteredEvents.Count, Is.EqualTo (0));
			ctx.Assert (logger.Statistics.Count, Is.EqualTo (4));
			ctx.Assert (logger.Statistics[0].Type,
				    Is.EqualTo (TestLoggerBackend.StatisticsEventType.Running));
			ctx.Assert (logger.Statistics[0].Status,
				    Is.EqualTo (TestStatus.Success));
			ctx.Assert (logger.Statistics[1].Type,
			            Is.EqualTo (TestLoggerBackend.StatisticsEventType.Running));
			ctx.Assert (logger.Statistics[1].Status,
				    Is.EqualTo (TestStatus.Success));
			ctx.Assert (logger.Statistics[2].Type,
				    Is.EqualTo (TestLoggerBackend.StatisticsEventType.Finished));
			ctx.Assert (logger.Statistics[2].Status,
				    Is.EqualTo (TestStatus.Success));
			ctx.Assert (logger.Statistics[3].Type,
				    Is.EqualTo (TestLoggerBackend.StatisticsEventType.Finished));
			ctx.Assert (logger.Statistics[3].Status,
				    Is.EqualTo (TestStatus.Success));

			var messageEvents = logger.Events.Where (
				e => e.Kind == TestLoggerBackend.EntryKind.Message).ToList ();

			ctx.Assert (messageEvents.Count, Is.EqualTo (15));

			ctx.Assert (messageEvents[0].Text, Is.EqualTo ($"{ReverseForkedHost.ME} SERIALIZE: 0 1"));
			ctx.Assert (messageEvents[1].Text, Is.EqualTo ($"{ReverseForkedHost.ME} INIT: 1 2"));
			ctx.Assert (messageEvents[2].Text, Is.EqualTo ($"{ReverseForkedHost.ME} DESERIALIZE: 0 1"));
			ctx.Assert (messageEvents[3].Text, Is.EqualTo ($"{ReverseForkedHost.ME} INIT: 2 5"));
			ctx.Assert (messageEvents[4].Text, Is.EqualTo ($"{ReverseForkedHost.ME} PRE RUN: 2 5"));
			ctx.Assert (messageEvents[5].Text, Is.EqualTo ($"{ReverseForkedHost.ME} SERIALIZE: 2 5"));
			ctx.Assert (messageEvents[6].Text, Is.EqualTo ($"{ReverseForkedHost.ME} DESERIALIZE: 1 1"));
			ctx.Assert (messageEvents[7].Text, Is.EqualTo ($"{ReverseForkedHost.ME} INIT: 1 6"));
			ctx.Assert (messageEvents[8].Text, Is.EqualTo ($"{ReverseForkedHost.ME} PRE RUN: 1 6"));
			ctx.Assert (messageEvents[9].Text, Is.EqualTo ($"{ReverseForkedHost.ME} RUN: 1 6"));
			ctx.Assert (messageEvents[10].Text, Is.EqualTo ($"{ReverseForkedHost.ME} POST RUN: 1 6"));
			ctx.Assert (messageEvents[11].Text, Is.EqualTo ($"{ReverseForkedHost.ME} DESTROY: 1 6"));
			ctx.Assert (messageEvents[12].Text, Is.EqualTo ($"{ReverseForkedHost.ME} POST RUN: 3 6"));
			ctx.Assert (messageEvents[13].Text, Is.EqualTo ($"{ReverseForkedHost.ME} DESTROY: 3 6"));
			ctx.Assert (messageEvents[14].Text, Is.EqualTo ($"{ReverseForkedHost.ME} DESTROY: 1 2"));

			base.CheckLogging (ctx, logger);
		}
	}
}
