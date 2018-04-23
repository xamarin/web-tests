//
// ExternalHost.cs
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
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using Xamarin.AsyncTests.Constraints;

namespace Xamarin.AsyncTests.FrameworkTests
{
	[ExternalHost]
	public class ExternalHost : ITestInstance, IForkedTestInstance
	{
		public int IsForked {
			get;
			private set;
		}

		public ExternalHost ()
		{
			IsForked = 1;
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

		internal static string ME = "EXTERNAL HOST";

		static Task FinishedTask => Task.FromResult<object> (null);

		static int preRunCalled, postRunCalled;

		public Task Initialize (TestContext ctx, CancellationToken cancellationToken)
		{
			ctx.LogMessage ($"{ME} INIT: {IsForked}");
			return FinishedTask;
		}

		public Task PreRun (TestContext ctx, CancellationToken cancellationToken)
		{
			ctx.LogMessage ($"{ME} PRE RUN: {IsForked}");
			ctx.Assert (IsForked, Is.EqualTo (2), "only called in forked instance");
			ctx.Assert (Interlocked.Increment (ref preRunCalled), Is.EqualTo (1));
			return FinishedTask;
		}

		public Task PostRun (TestContext ctx, CancellationToken cancellationToken)
		{
			ctx.LogMessage ($"{ME} POST RUN: {IsForked}");
			ctx.Assert (IsForked, Is.EqualTo (2), "only called in forked instance");
			ctx.Assert (Interlocked.Increment (ref postRunCalled), Is.EqualTo (1));
			return FinishedTask;
		}

		public Task Destroy (TestContext ctx, CancellationToken cancellationToken)
		{
			ctx.LogMessage ($"{ME} DESTROY: {IsForked}");
			return FinishedTask;
		}
	}
}
