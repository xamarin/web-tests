//
// HeavyTestInstance.cs
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
	abstract class HeavyTestInstance : TestInstance
	{
		new public HeavyTestHost Host {
			get { return (HeavyTestHost)base.Host; }
		}

		public HeavyTestInstance (HeavyTestHost host, TestPath path, TestInstance parent)
			: base (host, path, parent)
		{
		}

		public abstract object Current {
			get;
		}

		internal sealed override ITestParameter GetCurrentParameter ()
		{
			return new InstanceWrapper (Host, Current);
		}

		class InstanceWrapper : ITestParameter, ITestParameterWrapper
		{
			readonly HeavyTestHost host;
			readonly object value;

			public InstanceWrapper (HeavyTestHost host, object value)
			{
				this.host = host;
				this.value = value;
			}

			public string Value {
				get { return host.Identifier; }
			}

			object ITestParameterWrapper.Value {
				get { return value; }
			}
		}

		public override bool ParameterMatches<T> (string name)
		{
			return typeof(T).Equals (Host.Type);
		}

		public override T GetParameter<T> ()
		{
			return (T)Current;
		}

		[StackTraceEntryPoint]
		public abstract Task Initialize (TestContext ctx, CancellationToken cancellationToken);

		[StackTraceEntryPoint]
		public abstract Task PreRun (TestContext ctx, CancellationToken cancellationToken);

		[StackTraceEntryPoint]
		public abstract Task PostRun (TestContext ctx, CancellationToken cancellationToken);

		[StackTraceEntryPoint]
		public abstract Task Destroy (TestContext ctx, CancellationToken cancellationToken);
	}
}

