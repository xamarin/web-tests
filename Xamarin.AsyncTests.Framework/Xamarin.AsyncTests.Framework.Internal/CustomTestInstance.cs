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

namespace Xamarin.AsyncTests.Framework.Internal
{
	class CustomTestInstance<T> : ParameterizedTestInstance
		where T : ITestInstance
	{
		ITestHost<T> customHost;
		T instance;

		public Type HostType {
			get;
			private set;
		}

		public CustomTestInstance (ParameterizedTestHost host, TestInstance parent, Type hostType)
			: base (host, parent)
		{
			HostType = hostType;
		}

		ITestHost<T> GetHost (TestContext context)
		{
			if (HostType != null)
				return (ITestHost<T>)Activator.CreateInstance (HostType);

			return (ITestHost<T>)GetFixtureInstance ().Instance;
		}

		public override async Task Initialize (TestContext context, CancellationToken cancellationToken)
		{
			customHost = GetHost (context);
			instance = await customHost.Initialize (context, cancellationToken);
		}

		public override async Task PreRun (TestContext context, CancellationToken cancellationToken)
		{
			customHost = GetHost (context);
			await customHost.PreRun (context, instance, cancellationToken);
		}

		public override async Task PostRun (TestContext context, CancellationToken cancellationToken)
		{
			customHost = GetHost (context);
			await customHost.PostRun (context, instance, cancellationToken);
		}

		public override bool HasNext ()
		{
			return false;
		}

		public override Task MoveNext (TestContext context, CancellationToken cancellationToken)
		{
			return Task.FromResult<object> (null);
		}

		public override Task Destroy (TestContext context, CancellationToken cancellationToken)
		{
			return customHost.Destroy (context, instance, cancellationToken);
		}

		public override object Current {
			get { return instance; }
		}
	}
}

