﻿//
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
using System.Xml.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Xamarin.AsyncTests.Framework
{
	using Reflection;
	using Remoting;

	abstract class HeavyTestInstance : RemoteTestInstance
	{
		new public HeavyTestHost Host {
			get { return (HeavyTestHost)base.Host; }
		}

		public HeavyTestInstance (HeavyTestHost host, TestNode node, TestInstance parent)
			: base (host, node, parent)
		{
			CurrentParameter = new RemoteTestValue (this, host.Name, null);
		}

		public abstract object Instance {
			get;
		}

		internal sealed override RemoteTestValue CurrentParameter {
			get;
		}

		[StackTraceEntryPoint]
		protected override void Serialize (TestContext ctx)
		{
			base.Deserialize (ctx);
			if (Instance is IForkedTestInstance forked)
				forked.Serialize (ctx, CustomParameter);
		}

		[StackTraceEntryPoint]
		protected override void Deserialize (TestContext ctx)
		{
			base.Serialize (ctx);
			if (Instance is IForkedTestInstance forked) {
				forked.Deserialize (ctx, CustomParameter);
				if (forked is IForkedObject forkedObject)
					forkedObject.ObjectClient = ObjectClient;
			}
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

