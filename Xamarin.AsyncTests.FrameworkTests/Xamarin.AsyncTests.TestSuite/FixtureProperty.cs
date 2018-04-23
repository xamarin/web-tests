﻿//
// FixtureProperty.cs
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
using Xamarin.AsyncTests.Constraints;

namespace Xamarin.AsyncTests.TestSuite
{
	[AsyncTestFixture]
	public class FixtureProperty
	{
		public FixtureProperty ()
		{
			FrameworkTestFeatures.Counters.FixturePropertyConstructor++;
		}

		public SimpleEnum Property {
			get; set;
		}

		[AsyncTest]
		public void Run (TestContext ctx)
		{
			switch (Property) {
			case SimpleEnum.Foo:
				FrameworkTestFeatures.Counters.FixturePropertyFoo++;
				break;
			case SimpleEnum.Bar:
				FrameworkTestFeatures.Counters.FixturePropertyBar++;
				break;
			default:
				throw ctx.AssertFail (Property);
			}
		}

		// [AsyncTest]
		public void TestContextParameter (TestContext ctx)
		{
			var parameter = ctx.GetParameter<SimpleEnum> ();
			ctx.Assert (parameter, Is.EqualTo (Property));
		}
	}
}
