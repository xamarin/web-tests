//
// RunExternalWithParameter.cs
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
	public class RunExternalWithParameter : FrameworkInvoker
	{
		protected override void SetupSession (
			TestContext ctx, SettingsBag settings, TestSession session)
		{
			session.Configuration.CurrentCategory = TestCategory.Martin;
			session.Configuration.SetIsEnabled (TestFeature.ForkedSupport, true);
			settings.MartinTest = "FrameworkTests.ExternalWithParameter";
		}

		bool seenFirst, seenSecond, seenThird;

		protected override bool ForwardLogging => false;

		protected override void VisitResult (TestContext ctx, TestResult result)
		{
			base.VisitResult (ctx, result);

			ctx.Assert (result.Status, Is.EqualTo (TestStatus.Success));
			ctx.Assert (result.Path, Is.Not.Null);

			var node = result.Path.Node;
			ctx.Assert (node.PathType, Is.EqualTo (TestPathType.Fork));

			var parentNode = result.Path.Parent.Node;
			ctx.Assert (parentNode.PathType, Is.EqualTo (TestPathType.Parameter));
			ctx.Assert (parentNode.Identifier, Is.EqualTo ("test"));
			ctx.Assert (parentNode.ParameterType, Is.EqualTo ("SimpleEnum"));

			switch (parentNode.ParameterValue) {
			case "First":
				ctx.Assert (seenFirst, Is.False);
				seenFirst = true;
				break;
			case "Second":
				ctx.Assert (seenSecond, Is.False);
				seenSecond = true;
				break;
			case "Third":
				ctx.Assert (seenThird, Is.False);
				seenThird = true;
				break;
			default:
				throw ctx.AssertFail ($"Invalid parameter value: `{parentNode.ParameterValue}'");
			}
		}

		protected override void CheckFinalStatus (TestContext ctx)
		{
			ctx.Assert (seenFirst);
			ctx.Assert (seenSecond);
			ctx.Assert (seenThird);
			base.CheckFinalStatus (ctx);
		}

		protected override void CheckLogging (TestContext ctx, FrameworkLogger logger)
		{
			ctx.Assert (logger.Events.Count, Is.GreaterThan (0));
			var filteredEvents = logger.Events.Where (
				e => e.Kind == TestLoggerBackend.EntryKind.Error);
			ctx.Assert (filteredEvents.Count, Is.EqualTo (0));
			ctx.Assert (logger.Statistics.Count, Is.EqualTo (6));
			for (int i = 0; i < 3; i++) {
				ctx.Assert (logger.Statistics[i << 1].Type,
					    Is.EqualTo (TestLoggerBackend.StatisticsEventType.Running));
				ctx.Assert (logger.Statistics[i << 1].Status,
					    Is.EqualTo (TestStatus.Success));
				ctx.Assert (logger.Statistics[(i << 1) + 1].Type,
					    Is.EqualTo (TestLoggerBackend.StatisticsEventType.Finished));
				ctx.Assert (logger.Statistics[(i << 1) + 1].Status,
					    Is.EqualTo (TestStatus.Success));
			}
			base.CheckLogging (ctx, logger);
		}
	}
}
