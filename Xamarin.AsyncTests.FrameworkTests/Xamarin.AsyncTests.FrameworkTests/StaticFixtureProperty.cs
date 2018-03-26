//
// StaticFixtureProperty.cs
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

namespace Xamarin.AsyncTests.FrameworkTests
{
	[AsyncTestFixture]
	public class StaticFixtureProperty
	{
		public StaticFixtureProperty ()
		{
			FrameworkTestFeatures.Instance.StaticFixturePropertyConstructor++;
		}

		public SimpleEnum Property {
			get { return property; }
			set {
				switch (value) {
				case SimpleEnum.Foo:
					FrameworkTestFeatures.Instance.StaticFixturePropertyFooSetter++;
					break;
				case SimpleEnum.Bar:
					FrameworkTestFeatures.Instance.StaticFixturePropertyBarSetter++;
					break;
				default:
					throw new InternalErrorException ();
				}
				property = value;
			}
		}

		SimpleEnum property;

		[AsyncTest]
		public static void Run (TestContext ctx, StaticFixtureProperty instance)
		{
			switch (instance.Property) {
			case SimpleEnum.Foo:
				FrameworkTestFeatures.Instance.StaticFixturePropertyFoo++;
				break;
			case SimpleEnum.Bar:
				FrameworkTestFeatures.Instance.StaticFixturePropertyBar++;
				break;
			default:
				throw ctx.AssertFail (instance.Property);
			}
		}
	}
}
