//
// AbstractForkedFixture.cs
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
using System.Xml.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xamarin.AsyncTests.Constraints;

namespace Xamarin.AsyncTests.FrameworkTests
{
	[ForkedSupport]
	[Fork (ForkType.FromContext)]
	[AsyncTestFixture (Prefix = "FrameworkTests")]
	public abstract class AbstractForkedFixture : IForkedTestInstance
	{
		[FixtureParameter]
		public virtual ForkType Type => ForkType.None;

		public int IsForked {
			get;
			private set;
		}

		public AbstractForkedFixture ()
		{
			IsForked = 1;
		}

		internal static string ME = "ABSTRACT FORKED FIXTURE";

		[AsyncTest]
		[Martin (null, UseFixtureName = true)]
		public static void Test (TestContext ctx, AbstractForkedFixture instance)
		{
			ctx.LogMessage ($"{ME} RUN: {instance.IsForked}");
		}

		public void Serialize (TestContext ctx, XElement element)
		{
			ctx.LogMessage ($"{ME} SERIALIZE: {IsForked}");
			ctx.Assert (IsForked, Is.EqualTo (1));
			var hello = new XElement ("Hello");
			var attr = new XAttribute ("Text", "World");
			hello.Add (attr);
			element.Add (hello);
		}

		public void Deserialize (TestContext ctx, XElement element)
		{
			ctx.LogMessage ($"{ME} DESERIALIZE: {IsForked}");
			var hello = element.Element ("Hello");
			ctx.Assert (hello, Is.Not.Null, "root element");
			var attr = hello.Attribute ("Text");
			ctx.Assert (attr, Is.Not.Null, "attribute");
			ctx.Assert (attr.Value, Is.EqualTo ("World"), "attribute value");
			ctx.Assert (IsForked, Is.EqualTo (1));
			IsForked = 2;
		}
	}
}
