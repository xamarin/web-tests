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
	abstract class HeavyTestInstance : TestInstance
	{
		new public HeavyTestHost Host {
			get { return (HeavyTestHost)base.Host; }
		}

		public HeavyTestInstance (HeavyTestHost host, TestNode node, TestInstance parent)
			: base (host, node, parent)
		{
		}

		public abstract object Instance {
			get;
		}

		internal TestParameterValue CurrentParameter {
			get;
			private set;
		}

		internal XElement CustomParameter {
			get;
			private set;
		}

		internal sealed override TestParameterValue GetCurrentParameter ()
		{
			return CurrentParameter;
		}

		public override void Initialize (TestContext ctx)
		{
			if (Instance is IForkedTestInstance forked) {
				if (Node.CustomParameter != null) {
					CustomParameter = Node.CustomParameter;
					forked.Deserialize (ctx, CustomParameter);
				} else {
					var element = TestNode.CreateCustomParameterNode ();
					if (forked.Serialize (ctx, element))
						CustomParameter = element;
				}
			}
			CurrentParameter = new HeavyTestValue (this);
			base.Initialize (ctx);
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

