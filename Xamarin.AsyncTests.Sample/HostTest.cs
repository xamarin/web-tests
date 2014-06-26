//
// HostTest.cs
//
// Author:
//       Martin Baulig <martin.baulig@xamarin.com>
//
// Copyright (c) 2014 Xamarin Inc. (http://www.xamarin.com)
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

namespace Xamarin.AsyncTests.Sample
{
	[AsyncTestFixture]
	public class HostTest
	{
		[TestHost (typeof (MyHost))]
		public class MyInstance : ITestInstance
		{
			public string Hello {
				get;
				private set;
			}

			public MyInstance (string hello)
			{
				Hello = hello;
			}

			public override string ToString ()
			{
				return string.Format ("[MyInstance: Hello={0}]", Hello);
			}
		}

		public class MyHost : ITestHost<MyInstance>
		{
			public async Task<MyInstance> Initialize (TestContext context, CancellationToken cancellationToken)
			{
				var instance = new MyInstance ("Berlin");
				context.Log ("INITIALIZE: {0}!", instance);
				await Task.Delay (2500, cancellationToken);
				context.Log ("INITIALIZE DONE: {0}!", instance);
				return instance;
			}

			public async Task ReuseInstance (TestContext context, MyInstance instance, CancellationToken cancellationToken)
			{
				context.Log ("REUSE INSTANCE: {0}!", instance);
				await Task.Delay (500, cancellationToken);
				context.Log ("REUSE INSTANCE DONE: {0}!", instance);
			}

			public async Task Destroy (TestContext context, MyInstance instance, CancellationToken cancellationToken)
			{
				context.Log ("DESTROY: {0}!", instance);
				await Task.Delay (2500, cancellationToken);
				context.Log ("DESTROY DONE: {0}!", instance);
			}
		}

		[AsyncTest]
		public void Test (TestContext context, [Repeat (3)] int outer, MyInstance instance, [Repeat (10)] int iteration)
		{
			context.Log ("TEST: {0} {1} {2}", outer, instance, iteration);
		}
	}
}

