//
// CustomHostInstance.cs
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

namespace Xamarin.AsyncTests.Framework
{
	class CustomTestInstance : HeavyTestInstance
	{
		ITestHost<ITestInstance> customHost;
		ITestInstance instance;

		new public CustomTestHost Host {
			get { return (CustomTestHost)base.Host; }
		}

		public CustomTestInstance (CustomTestHost host, TestNode node, TestInstance parent)
			: base (host, node, parent)
		{
		}

		public override object Current {
			get { return instance; }
		}

		public override async Task Initialize (TestContext ctx, CancellationToken cancellationToken)
		{
			if (Host.UseFixtureInstance)
				customHost = (ITestHost<ITestInstance>)GetFixtureInstance ().Instance;
			else if (Host.StaticHost != null)
				customHost = Host.StaticHost;
			else if (Host.HostType != null)
				customHost = (ITestHost<ITestInstance>)Activator.CreateInstance (Host.HostType);
			else
				throw new InternalErrorException ();

			instance = customHost.CreateInstance (ctx);
			await instance.Initialize (ctx, cancellationToken);
		}

		public override async Task PreRun (TestContext ctx, CancellationToken cancellationToken)
		{
			await instance.PreRun (ctx, cancellationToken);
		}

		public override async Task PostRun (TestContext ctx, CancellationToken cancellationToken)
		{
			await instance.PostRun (ctx, cancellationToken);
		}

		public override async Task Destroy (TestContext ctx, CancellationToken cancellationToken)
		{
			await instance.Destroy (ctx, cancellationToken);
		}
	}
}

