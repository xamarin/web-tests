//
// ForkedTestInvoker.cs
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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Xamarin.AsyncTests.Framework
{
	class ForkedTestInvoker : AggregatedTestInvoker
	{
		public ForkedTestHost Host {
			get;
		}

		public TestNode Node {
			get;
		}

		public TestInvoker Inner {
			get;
		}

		public ForkedTestInvoker (ForkedTestHost host, TestNode node, TestInvoker inner)
			: base (host.Flags)
		{
			Host = host;
			Node = node;
			Inner = inner;
		}

		public override async Task<bool> Invoke (TestContext ctx, TestInstance instance, CancellationToken cancellationToken)
		{
			var forks = new ForkedTestInstance [Host.Attribute.Count];
			var tasks = new Task<bool> [forks.Length];

			var random = new Random ();

			for (int i = 0; i < forks.Length; i++) {
				int delay = 0;
				if (Host.Attribute.RandomDelay > 0)
					delay = random.Next (Host.Attribute.RandomDelay);
				var fork = forks [i] = new ForkedTestInstance (Host, Node, instance, i, delay, Inner);

				tasks [i] = Task.Factory.StartNew (
					() => fork.Start (ctx, cancellationToken).Result,
					cancellationToken, TaskCreationOptions.LongRunning, TaskScheduler.Default);
			}

			var retval = await Task.WhenAll (tasks).ConfigureAwait (false);
			return retval.All (r => r);
		}
	}
}

