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
		protected internal static Task FinishedTask {
			get { return Task.FromResult<object> (null); }
		}

		public static async Task WaitWithTimeout (TestContext ctx, int timeout, params Task[] tasks)
		{
			var timeoutTask = Task.Delay (timeout);
			var allTasks = new Task[tasks.Length + 1];
			allTasks[0] = timeoutTask;
			tasks.CopyTo (allTasks, 1);
			var ret = await Task.WhenAny (allTasks).ConfigureAwait (false);
			if (ret == timeoutTask)
				throw ctx.AssertFail ("Timeout!");
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

		[StackTraceEntryPoint]
		public void Close ()
		{
			if (Interlocked.CompareExchange (ref stopped, 1, 0) != 0)
				return;
			Stop ();
		}

		protected abstract void Stop ();

		public void Dispose ()
		{
			Dispose (true);
			GC.SuppressFinalize (this);
		}

		int disposed;
		int stopped;

		protected virtual void Dispose (bool disposing)
		{
			if (Interlocked.CompareExchange (ref disposed, 1, 0) != 0)
				return;
			Close ();
		}

		~AbstractConnection ()
		{
			Dispose (false);
		}
	}
}

