//
// InvocationCounters.cs
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
	public class InvocationCounters
	{
		internal int FrameworkTestFeaturesInvoked {
			get; set;
		}

		internal int InstanceMethodInAbstractFixtureInvoked {
			get; set;
		}

		internal int AbstractMethodInAbstractFixtureInvoked {
			get; set;
		}

		internal int StaticMethodInvoked {
			get; set;
		}

		internal int StaticClassMethodInvoked {
			get; set;
		}

		internal int StaticMethodWithParameterConstructorInvoked {
			get; set;
		}

		internal int StaticMethodWithParameterMethodInvoked {
			get; set;
		}

		internal int StaticWithAbstractParametersInvoked {
			get; set;
		}

		internal int FixturePropertyConstructor {
			get; set;
		}

		internal int FixturePropertyFoo {
			get; set;
		}

		internal int FixturePropertyBar {
			get; set;
		}

		internal int TwoStaticMethodsConstructor {
			get; set;
		}

		internal int TwoStaticMethodsFirst {
			get; set;
		}

		internal int TwoStaticMethodsSecond {
			get; set;
		}

		internal int StaticFixturePropertyConstructor {
			get; set;
		}

		internal int StaticFixturePropertyFoo {
			get; set;
		}

		internal int StaticFixturePropertyBar {
			get; set;
		}

		internal int StaticFixturePropertyFooSetter {
			get; set;
		}

		internal int StaticFixturePropertyBarSetter {
			get; set;
		}

		internal int ConstructorArgumentConstructorFoo {
			get; set;
		}

		internal int ConstructorArgumentConstructorBar {
			get; set;
		}

		internal int ConstructorArgumentMethodFoo {
			get; set;
		}

		internal int ConstructorArgumentMethodBar {
			get; set;
		}

		internal int StaticConstructorArgumentConstructorFoo {
			get; set;
		}

		internal int StaticConstructorArgumentConstructorBar {
			get; set;
		}

		internal int StaticConstructorArgumentMethodFoo {
			get; set;
		}

		internal int StaticConstructorArgumentMethodBar {
			get; set;
		}

		internal int AbstractPropertyFoo {
			get; set;
		}

		internal int AbstractPropertyBar {
			get; set;
		}

		internal int ConditionalFixture {
			get; set;
		}

		internal int ConditionalMethod {
			get; set;
		}

		internal int TestEnumValueInvokedFilter {
			get; set;
		}

		internal int TestEnumValueFilterRunFirst {
			get; set;
		}

		internal int TestEnumValueFilterRunSecond {
			get; set;
		}

		internal int TestEnumCategoryFilterRunFirst {
			get; set;
		}

		internal int TestEnumCategoryFilterRunSecond {
			get; set;
		}

		internal int TestEnumCategoryFilterRunThird {
			get; set;
		}

		[StackTraceEntryPoint]
		public void CheckInvocationCounts (TestContext ctx)
		{
			ctx.Assert (FrameworkTestFeaturesInvoked, Is.EqualTo (1),
				    "FrameworkTestFeaturesInvoked");
			ctx.Assert (InstanceMethodInAbstractFixtureInvoked, Is.EqualTo (2),
				    "InstanceMethodInAbstractFixtureInvoked");
			ctx.Assert (AbstractMethodInAbstractFixtureInvoked, Is.EqualTo (2),
				    "AbstractMethodInAbstractFixtureInvoked");
			ctx.Assert (StaticMethodInvoked, Is.EqualTo (1),
				    "StaticMethodInvoked");
			ctx.Assert (StaticClassMethodInvoked, Is.EqualTo (1),
				    "StaticClassMethodInvoked");
			ctx.Assert (StaticMethodWithParameterConstructorInvoked,
				    Is.EqualTo (1),
				    "StaticMethodWithParameterConstructorInvoked");
			ctx.Assert (StaticMethodWithParameterMethodInvoked,
				    Is.EqualTo (1),
				    "StaticMethodWithParameterMethodInvoked");
			ctx.Assert (StaticWithAbstractParametersInvoked,
				    Is.EqualTo (2),
				    "StaticWithAbstractParametersInvoked");
			ctx.Assert (FixturePropertyConstructor, Is.EqualTo (1),
				    "FixturePropertyConstructor");
			ctx.Assert (FixturePropertyFoo, Is.EqualTo (1),
				    "FixturePropertyFoo");
			ctx.Assert (FixturePropertyBar, Is.EqualTo (1),
				    "FixturePropertyBar");
			ctx.Assert (TwoStaticMethodsConstructor, Is.EqualTo (2),
				    "TwoStaticMethodsConstructor");
			ctx.Assert (TwoStaticMethodsFirst, Is.EqualTo (1),
				    "TwoStaticMethodsFirst");
			ctx.Assert (TwoStaticMethodsSecond, Is.EqualTo (1),
				    "TwoStaticMethodsSecond");
			ctx.Assert (StaticFixturePropertyConstructor, Is.EqualTo (1),
				    "StaticFixturePropertyConstructor");
			ctx.Assert (StaticFixturePropertyFoo, Is.EqualTo (1),
				    "StaticFixturePropertyFoo");
			ctx.Assert (StaticFixturePropertyBar, Is.EqualTo (1),
				    "StaticFixturePropertyBar");
			ctx.Assert (StaticFixturePropertyFooSetter, Is.EqualTo (1),
				    "StaticFixturePropertyFooSetter");
			ctx.Assert (StaticFixturePropertyBarSetter, Is.EqualTo (1),
				    "StaticFixturePropertyBarSetter");
			ctx.Assert (ConstructorArgumentConstructorFoo, Is.EqualTo (1),
				    "ConstructorArgumentConstructorFoo");
			ctx.Assert (ConstructorArgumentConstructorBar, Is.EqualTo (1),
				    "ConstructorArgumentConstructorBar");
			ctx.Assert (ConstructorArgumentMethodFoo, Is.EqualTo (1),
				    "ConstructorArgumentMethodFoo");
			ctx.Assert (ConstructorArgumentMethodBar, Is.EqualTo (1),
				    "ConstructorArgumentMethodBar");
			ctx.Assert (StaticConstructorArgumentConstructorFoo, Is.EqualTo (1),
				    "StaticConstructorArgumentConstructorFoo");
			ctx.Assert (StaticConstructorArgumentConstructorBar, Is.EqualTo (1),
				    "StaticConstructorArgumentConstructorBar");
			ctx.Assert (StaticConstructorArgumentMethodFoo, Is.EqualTo (1),
				    "StaticConstructorArgumentMethodFoo");
			ctx.Assert (StaticConstructorArgumentMethodBar, Is.EqualTo (1),
				    "StaticConstructorArgumentMethodBar");
			ctx.Assert (AbstractPropertyFoo, Is.EqualTo (1),
				    "AbstractPropertyFoo");
			ctx.Assert (AbstractPropertyBar, Is.EqualTo (1),
				    "AbstractPropertyBar");
			ctx.Assert (TestEnumValueInvokedFilter,
				    Is.GreaterThanOrEqualTo (1),
				    "TestEnumValueFilterFilterInvoked");
			ctx.Assert (TestEnumValueFilterRunFirst, Is.EqualTo (1),
				    "TestEnumValueFilterFilterRunFirst");
			ctx.Assert (TestEnumValueFilterRunSecond, Is.EqualTo (1),
				    "TestEnumValueFilterFilterRunSecond");
			ctx.Assert (TestEnumCategoryFilterRunFirst, Is.EqualTo (1),
			            "TestEnumCategoryFilterRunFirst");
			ctx.Assert (TestEnumCategoryFilterRunSecond, Is.EqualTo (1),
			            "TestEnumCategoryFilterRunSecond");

			ctx.Assert (ConditionalFixture, Is.EqualTo (0),
				    "ConditionalFixture");
			ctx.Assert (ConditionalMethod, Is.EqualTo (0),
				    "ConditionalMethod");
			ctx.Assert (TestEnumCategoryFilterRunThird, Is.EqualTo (0),
				    "TestEnumCategoryFilterRunThird");
		}

		[StackTraceEntryPoint]
		public void CheckConditionalInvocationCounts (TestContext ctx)
		{
			ctx.Assert (ConditionalFixture, Is.EqualTo (1),
				    "ConditionalFixture");
			ctx.Assert (ConditionalMethod, Is.EqualTo (1),
				    "ConditionalMethod");

			ctx.Assert (TestEnumCategoryFilterRunFirst, Is.EqualTo (1),
				    "TestEnumCategoryFilterRunFirst");
			ctx.Assert (TestEnumCategoryFilterRunSecond, Is.EqualTo (1),
				    "TestEnumCategoryFilterRunSecond");
			ctx.Assert (TestEnumCategoryFilterRunThird, Is.EqualTo (1),
				    "TestEnumCategoryFilterRunThird");
		}
	}
}
