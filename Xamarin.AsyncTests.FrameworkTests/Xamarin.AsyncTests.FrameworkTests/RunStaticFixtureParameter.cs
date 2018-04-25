//
// RunStaticFixtureParameter.cs
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
	public class RunStaticFixtureParameter : FrameworkInvoker
	{
		protected override void SetupSession (
			TestContext ctx, SettingsBag settings, TestSession session)
		{
			session.Configuration.CurrentCategory = TestCategory.Martin;
			settings.MartinTest = "FrameworkTests.StaticFixtureParameter";
			settings.DontSaveLogging = true;
		}

		int seenResult;

		protected override bool ForwardLogging => false;

		protected override void VisitResult (TestContext ctx, TestResult result)
		{
			base.VisitResult (ctx, result);

			ctx.Assert (result.Status, Is.EqualTo (TestStatus.Success), "#1");
			ctx.Assert (result.Path, Is.Not.Null, "#2");
			ctx.Assert (result.Path.Node.PathType, Is.EqualTo (TestPathType.Test), "#3");

			var parent = result.Path.Parent;
			ctx.Assert (parent, Is.Not.Null, "#4");
			ctx.Assert (parent.Node.PathType, Is.EqualTo (TestPathType.Collection), "#5");

			var parent2 = parent.Parent;
			ctx.Assert (parent2, Is.Not.Null, "#6");
			ctx.Assert (parent2.Node.PathType, Is.EqualTo (TestPathType.Parameter), "#7");

			var parent3 = parent2.Parent;
			ctx.Assert (parent3, Is.Not.Null, "#8");
			ctx.Assert (parent3.Node.PathType, Is.EqualTo (TestPathType.Fixture), "#9");

			var parent4 = parent3.Parent;
			ctx.Assert (parent4, Is.Not.Null, "#10");
			ctx.Assert (parent4.Node.PathType, Is.EqualTo (TestPathType.Assembly), "#11");

			var parent5 = parent4.Parent;
			ctx.Assert (parent5, Is.Not.Null, "#12");
			ctx.Assert (parent5.Node.PathType, Is.EqualTo (TestPathType.Suite), "#13");

			ctx.Assert (parent5.Parent, Is.Null, "#18");

			switch (seenResult++) {
			case 0:
				ctx.Assert (parent2.Node.ParameterValue, Is.EqualTo ("Two"), "Parameter #1");
				break;
			default:
				throw ctx.AssertFail ("Invalid result");
			}
		}

		protected override void CheckFinalStatus (TestContext ctx)
		{
			ctx.Assert (seenResult, Is.EqualTo (1));
			base.CheckFinalStatus (ctx);
		}

		protected override void CheckLogging (TestContext ctx, FrameworkLogger logger)
		{
			var messageEvents = logger.Events.Where (
				e => e.Kind == TestLoggerBackend.EntryKind.Message).ToList ();
			ctx.Assert (messageEvents.Count, Is.EqualTo (1));

			base.CheckLogging (ctx, logger);
		}
	}
}
