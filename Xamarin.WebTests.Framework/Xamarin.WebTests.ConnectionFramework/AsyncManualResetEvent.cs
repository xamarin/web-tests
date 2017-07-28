//
// AsyncManualResetEvent.cs
//
// Author:
//       Martin Baulig <mabaul@microsoft.com>
//
// Copyright (c) 2017 Xamarin Inc. (http://www.xamarin.com)
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

namespace Xamarin.WebTests.ConnectionFramework
{
	// https://blogs.msdn.microsoft.com/pfxteam/2012/02/11/building-async-coordination-primitives-part-1-asyncmanualresetevent/
	class AsyncManualResetEvent
	{
		volatile TaskCompletionSource<bool> m_tcs = new TaskCompletionSource<bool> ();

		public Task WaitAsync () { return m_tcs.Task; }

		public bool WaitOne (int millisecondTimeout)
		{
			return m_tcs.Task.Wait (millisecondTimeout);
		}

		public async Task<bool> WaitAsync (int millisecondTimeout)
		{
			var timeoutTask = Task.Delay (millisecondTimeout);
			var ret = await Task.WhenAny (m_tcs.Task, timeoutTask).ConfigureAwait (false);
			return ret != timeoutTask;
		}

		public void Set ()
		{
			var tcs = m_tcs;
			Task.Factory.StartNew (s => ((TaskCompletionSource<bool>)s).TrySetResult (true),
			    tcs, CancellationToken.None, TaskCreationOptions.PreferFairness, TaskScheduler.Default);
			tcs.Task.Wait ();
		}

		public void Reset ()
		{
			while (true) {
				var tcs = m_tcs;
				if (!tcs.Task.IsCompleted ||
				    Interlocked.CompareExchange (ref m_tcs, new TaskCompletionSource<bool> (), tcs) == tcs)
					return;
			}
		}

		public AsyncManualResetEvent (bool state)
		{
			if (state)
				Set ();
		}
	}
}
