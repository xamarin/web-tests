//
// StaticWithConstructorArgument.cs
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
namespace Xamarin.AsyncTests.TestSuite
{
	[AsyncTestFixture]
	public class StaticWithConstructorArgument
	{
		[AsyncTest]
		public StaticWithConstructorArgument (SimpleEnum value)
		{
			switch (value) {
			case SimpleEnum.Foo:
				FrameworkTestFeatures.Counters.StaticConstructorArgumentConstructorFoo++;
				break;
			case SimpleEnum.Bar:
				FrameworkTestFeatures.Counters.StaticConstructorArgumentConstructorBar++;
				break;
			default:
				throw new InternalErrorException ();
			}
			Value = value;
		}

		public SimpleEnum Value {
			get;
		}

		[AsyncTest]
		public static void Run (
			TestContext ctx, StaticWithConstructorArgument instance)
		{
			switch (instance.Value) {
			case SimpleEnum.Foo:
				FrameworkTestFeatures.Counters.StaticConstructorArgumentMethodFoo++;
				break;
			case SimpleEnum.Bar:
				FrameworkTestFeatures.Counters.StaticConstructorArgumentMethodBar++;
				break;
			default:
				throw ctx.AssertFail (instance.Value);
			}
		}
	}
}
