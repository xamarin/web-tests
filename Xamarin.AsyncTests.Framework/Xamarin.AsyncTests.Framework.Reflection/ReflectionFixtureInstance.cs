//
// ReflectionFixtureInstance.cs
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

namespace Xamarin.AsyncTests.Framework.Reflection
{
	sealed class ReflectionFixtureInstance : FixtureTestInstance
	{
		public override object Instance => fixtureInstance;

		new public ReflectionFixtureHost Host => (ReflectionFixtureHost)base.Host;

		object fixtureInstance;

		public ReflectionFixtureInstance (
			ReflectionFixtureHost host, TestNode node, TestInstance parent)
			: base (host, node, parent)
		{
		}

		public override void Initialize (TestContext ctx)
		{
			fixtureInstance = ReflectionMethodInvoker.InvokeConstructor (
				ctx, Host, Parent);
			if (fixtureInstance == null) {
				ctx.OnTestFinished (TestStatus.Error);
				throw ctx.IgnoreThisTest ();
			}
			base.Initialize (ctx);
		}

		public override async Task Initialize (TestContext ctx, CancellationToken cancellationToken)
		{
			if (fixtureInstance is ITestInstance testInstance)
				await testInstance.Initialize (ctx, cancellationToken);
		}

		public override async Task PreRun (TestContext ctx, CancellationToken cancellationToken)
		{
			if (fixtureInstance is ITestInstance testInstance)
				await testInstance.PreRun (ctx, cancellationToken);
		}

		public override async Task PostRun (TestContext ctx, CancellationToken cancellationToken)
		{
			if (fixtureInstance is ITestInstance testInstance)
				await testInstance.PostRun (ctx, cancellationToken);
		}

		public override async Task Destroy (TestContext ctx, CancellationToken cancellationToken)
		{
			if (fixtureInstance is ITestInstance testInstance)
				await testInstance.Destroy (ctx, cancellationToken);
		}
	}
}
