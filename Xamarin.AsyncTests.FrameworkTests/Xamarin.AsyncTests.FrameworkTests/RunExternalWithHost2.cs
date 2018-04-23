//
// RunExternalWithHost2.cs
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
	public class RunExternalWithHost2 : FrameworkInvoker
	{
		protected override void SetupSession (
			TestContext ctx, SettingsBag settings, TestSession session)
		{
			session.Configuration.CurrentCategory = TestCategory.Martin;
			session.Configuration.SetIsEnabled (TestFeature.ForkedSupport, true);
			settings.MartinTest = "FrameworkTests.ExternalWithHost2";
			StaticVariable = 1;
		}

		internal static int StaticVariable;

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
			ctx.Assert (StaticVariable, Is.EqualTo (1), "variable unmodified");
			base.CheckFinalStatus (ctx);
		}

		protected override void CheckLogging (TestContext ctx, FrameworkLogger logger)
		{
			CheckStartAndFinish (ctx, logger);

			var messageEvents = logger.Events.Where (
				e => e.Kind == TestLoggerBackend.EntryKind.Message).ToList ();

			ctx.Assert (messageEvents.Count, Is.GreaterThanOrEqualTo (8));

			ctx.Assert (messageEvents[0].Text, Is.EqualTo ($"{ExternalHost.ME} SERIALIZE: 1"));
			ctx.Assert (messageEvents[1].Text, Is.EqualTo ($"{ExternalHost.ME} INIT: 1"));
			ctx.Assert (messageEvents[2].Text, Is.EqualTo ($"{ExternalHost.ME} DESERIALIZE: 1"));
			ctx.Assert (messageEvents[3].Text, Is.EqualTo ($"{ExternalHost.ME} INIT: 2"));
			ctx.Assert (messageEvents[4].Text, Is.EqualTo ($"{ExternalHost.ME} PRE RUN: 2"));

			int pos = 4;
			while (pos++ < messageEvents.Count) {
				var message = messageEvents[pos].Text;
				if (message.StartsWith (ExternalHost.ME, StringComparison.Ordinal))
					break;
			}

			ctx.Assert (messageEvents[pos++].Text, Is.EqualTo ($"{ExternalHost.ME} POST RUN: 2"));
			ctx.Assert (messageEvents[pos++].Text, Is.EqualTo ($"{ExternalHost.ME} DESTROY: 2"));
			ctx.Assert (messageEvents[pos++].Text, Is.EqualTo ($"{ExternalHost.ME} DESTROY: 1"));

			base.CheckLogging (ctx, logger);
		}
	}
}
