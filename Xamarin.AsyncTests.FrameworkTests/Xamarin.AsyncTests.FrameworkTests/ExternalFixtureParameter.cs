//
// ExternalFixtureParameter.cs
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
using Xamarin.AsyncTests.Constraints;

namespace Xamarin.AsyncTests.FrameworkTests
{
	[ForkedSupport]
	[AsyncTestFixture (Prefix = "FrameworkTests")]
	public class ExternalFixtureParameter : IForkedTestInstance
	{
		public ExternalFixtureParameter ()
		{
			Interlocked.Increment (ref StaticVariable);
			IsForked = 1;
		}

		public int IsForked {
			get;
			private set;
		}

		public static int StaticVariable;

		internal static string ME = "EXTERNAL FIXTURE PARAMETER";

		[AsyncTest]
		[Martin (null, UseFixtureName = true)]
		public static void Run (TestContext ctx, [Fork (ForkType.Domain)] ExternalFixtureParameter instance)
		{
			ctx.LogMessage ($"{ME} RUN: {instance.IsForked}");
			ctx.Assert (instance.IsForked, Is.EqualTo (2));

			ctx.Assert (RunExternalFixtureParameter.StaticVariable, Is.EqualTo (0), "extern static variable");
			ctx.Assert (StaticVariable, Is.EqualTo (1), "our static variable");
			RunExternalFixtureParameter.StaticVariable = 2;
		}

		public void Serialize (TestContext ctx, XElement element)
		{
			ctx.LogMessage ($"{ME} SERIALIZE: {IsForked}");
			var test = new XElement ("Test");
			test.Add (new XAttribute ("Hello", "World"));
			element.Add (test);
		}

		public void Deserialize (TestContext ctx, XElement element)
		{
			ctx.LogMessage ($"{ME} DESERIALIZE: {IsForked}");
			IsForked = 2;
		}
	}
}
