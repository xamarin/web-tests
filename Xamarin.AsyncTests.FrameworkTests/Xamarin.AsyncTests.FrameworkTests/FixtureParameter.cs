//
// FixtureParameter.cs
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
using System.Collections.Generic;
using Xamarin.AsyncTests.Constraints;

namespace Xamarin.AsyncTests.FrameworkTests
{
	using TestSuite;

	[AsyncTestFixture (Prefix = "FrameworkTests")]
	public class FixtureParameter
	{
		[FixtureParameter]
		public TripleValue Value => TripleValue.Two;

		public SimpleEnum IgnoredValue => SimpleEnum.Foo;

		[AsyncTest]
		[Martin (null, UseFixtureName = true)]
		public void Run (TestContext ctx)
		{
			ctx.LogMessage ($"{ctx.FriendlyName}: {Value}");
			ctx.Assert (Value, Is.EqualTo (TripleValue.Two));

			ctx.Assert (ctx.TryGetParameter<SimpleEnum> (out var _), Is.False, "No SimpleEnum parameter");
			ctx.Assert (ctx.TryGetParameter<TripleValue> (out var value), Is.True, "Has TripleValue parameter");
			ctx.Assert (value, Is.EqualTo (TripleValue.Two));
		}
	}
}
