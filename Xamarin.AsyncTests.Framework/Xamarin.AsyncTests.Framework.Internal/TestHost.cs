//
// TestHost.cs
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
	abstract class TestHost
	{
		TestInstance parentInstance;
		TestInstance currentInstance;

		public TestFlags Flags {
			get; set;
		}

		internal bool HasInstance {
			get { return currentInstance != null; }
		}

		internal TestInstance CurrentInstance {
			get {
				if (currentInstance == null)
					throw new InvalidOperationException ();
				return currentInstance;
			}
		}

		internal async Task CreateInstance (TestContext context, CancellationToken cancellationToken)
		{
			if (currentInstance != null)
				throw new InvalidOperationException ();

			var instance = CreateInstance (context);
			try {
				currentInstance = instance;
				parentInstance = context.Instance;
				context.Instance = instance;
				await Initialize (context, cancellationToken);
			} catch {
				context.Instance = parentInstance;
				currentInstance = null;
				parentInstance = null;
				throw;
			}
		}

		internal async Task DestroyInstance (TestContext context, CancellationToken cancellationToken)
		{
			if (currentInstance == null)
				throw new InvalidOperationException ();
			if (context.Instance != currentInstance)
				throw new InvalidOperationException ();
			try {
				await Destroy (context, cancellationToken);
			} finally {
				context.Instance = parentInstance;
				currentInstance = null;
				parentInstance = null;
			}
		}

		protected abstract TestInstance CreateInstance (TestContext context);

		protected abstract Task Initialize (TestContext context, CancellationToken cancellationToken);

		protected abstract Task Destroy (TestContext context, CancellationToken cancellationToken);

		internal TestInvoker CreateInvoker (TestInvoker inner)
		{
			return new AggregatedTestInvoker (inner.Name, Flags, this, inner);
		}
	}
}

