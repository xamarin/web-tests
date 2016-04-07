//
// AbstractConnection.cs
//
// Author:
//       Martin Baulig <martin.baulig@xamarin.com>
//
// Copyright (c) 2016 Xamarin Inc. (http://www.xamarin.com)

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
using System.IO;
using System.Net;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Xamarin.AsyncTests;
using Xamarin.AsyncTests.Portable;

namespace Xamarin.WebTests.ConnectionFramework
{
	public abstract class AbstractConnection : ITestInstance, IDisposable
	{
		public IPortableEndPoint EndPoint {
			get;
			private set;
		}

		public ConnectionParameters Parameters {
			get;
			private set;
		}

		protected AbstractConnection (IPortableEndPoint endpoint, ConnectionParameters parameters)
		{
			EndPoint = endpoint;
			Parameters = parameters;
		}

		protected internal static Task FinishedTask {
			get { return Task.FromResult<object> (null); }
		}

		#region ITestInstance implementation

		bool initialized;

		Task ITestInstance.Initialize (TestContext ctx, CancellationToken cancellationToken)
		{
			if (initialized)
				throw new InvalidOperationException ();
			initialized = true;

			return Initialize (ctx, cancellationToken);
		}

		protected abstract Task Initialize (TestContext ctx, CancellationToken cancellationToken);

		Task ITestInstance.PreRun (TestContext ctx, CancellationToken cancellationToken)
		{
			return PreRun (ctx, cancellationToken);
		}

		protected abstract Task PreRun (TestContext ctx, CancellationToken cancellationToken);

		Task ITestInstance.PostRun (TestContext ctx, CancellationToken cancellationToken)
		{
			return PostRun (ctx, cancellationToken);
		}

		protected abstract Task PostRun (TestContext ctx, CancellationToken cancellationToken);

		async Task ITestInstance.Destroy (TestContext ctx, CancellationToken cancellationToken)
		{
			try {
				await Destroy (ctx, cancellationToken).ConfigureAwait (false);
			} finally {
				Dispose ();
			}
		}

		protected abstract Task Destroy (TestContext ctx, CancellationToken cancellationToken);

		#endregion

		public void Dispose ()
		{
			Dispose (true);
			GC.SuppressFinalize (this);
		}

		bool disposed;
		bool stopped;

		protected virtual void Dispose (bool disposing)
		{
			lock (this) {
				if (disposed)
					return;
				disposed = true;
				if (stopped)
					return;
				stopped = true;
			}
		}

		~AbstractConnection ()
		{
			Dispose (false);
		}
	}
}

