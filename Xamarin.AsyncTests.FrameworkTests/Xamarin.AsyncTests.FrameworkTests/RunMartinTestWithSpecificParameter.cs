﻿//
// RunMartinTestWithSpecificParameter.cs
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
using Xamarin.AsyncTests.Framework;
using Xamarin.AsyncTests.Constraints;

namespace Xamarin.AsyncTests.FrameworkTests
{
	public class RunMartinTestWithSpecificParameter : FrameworkInvoker
	{
		protected override void SetupSession (
			TestContext ctx, SettingsBag settings, TestSession session)
		{
			session.Configuration.CurrentCategory = TestCategory.Martin;
			settings.MartinTest = "FrameworkTests.MartinTestWithParameter:test=Second";
		}

		bool seenResult;

		protected override void VisitResult (TestContext ctx, TestResult result)
		{
			base.VisitResult (ctx, result);

			ctx.Assert (seenResult, Is.False);
			seenResult = true;

			ctx.Assert (result.Status, Is.EqualTo (TestStatus.Success));
			ctx.Assert (result.Path, Is.Not.Null);

			var node = result.Path.Node;
			ctx.Assert (node.PathType, Is.EqualTo (TestPathType.Parameter));
			ctx.Assert (node.Identifier, Is.EqualTo ("test"));
			ctx.Assert (node.ParameterType, Is.EqualTo ("SimpleEnum"));
			ctx.Assert (node.Parameter.Value, Is.EqualTo ("Second"));
		}

		protected override void CheckFinalStatus (TestContext ctx)
		{
			ctx.Assert (seenResult);
			base.CheckFinalStatus (ctx);
		}
	}
}
