//
// ExternalWithHost.cs
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

namespace Xamarin.AsyncTests.FrameworkTests
{
	// [Martin ("Test")]
	[ForkedSupport]
	[AsyncTestFixture (Prefix = "FrameworkTests")]
	public class ExternalWithHost
	{
		public ExternalWithHost ()
		{
			System.Diagnostics.Debug.WriteLine ($"CONSTRUCTOR!");
		}

		[AsyncTest]
		// [Martin (null, UseFixtureName = true)]
		public static void Test (
			TestContext ctx, MyHost host,
			[Fork (ForkType.Internal)] IFork fork)
		{
			ctx.LogMessage ($"Martin Test: {ctx.FriendlyName} {host} {fork}");
			ctx.Assert (host.IsForked);
		}

		static Task FinishedTask => Task.FromResult<object> (null);

		class MyHostAttribute : TestHostAttribute, ITestHost<MyHost>
		{
			public MyHostAttribute () : base (typeof (MyHostAttribute))
			{
				
			}

			public MyHost CreateInstance (TestContext context)
			{
				return new MyHost ();
			}
		}

		[MyHost]
		public class MyHost : ITestInstance, IForkedTestInstance
		{
			public bool IsForked {
				get;
				private set;
			}

			public void Serialize (TestContext ctx, XElement element)
			{
				element.Add (new XElement ("Hello"));
				element.Add (new XElement ("World"));
				ctx.LogMessage ($"MY HOST SERIALIZE");
			}

			public void Deserialize (TestContext ctx, XElement element)
			{
				IsForked = true;
				ctx.LogMessage ($"MY HOST DESERIALIZE: {element}");
			}

			public Task Initialize (TestContext ctx, CancellationToken cancellationToken)
			{
				ctx.LogMessage ($"MY HOST INIT");
				return FinishedTask;
			}

			public Task PreRun (TestContext ctx, CancellationToken cancellationToken)
			{
				ctx.LogMessage ($"MY HOST PRE RUN");
				return FinishedTask;
			}

			public Task PostRun (TestContext ctx, CancellationToken cancellationToken)
			{
				ctx.LogMessage ($"MY HOST POST RUN");
				return FinishedTask;
			}

			public Task Destroy (TestContext ctx, CancellationToken cancellationToken)
			{
				ctx.LogMessage ($"MY HOST DESTROY");
				return FinishedTask;
			}
		}
	}
}
