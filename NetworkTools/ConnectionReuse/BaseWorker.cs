//
// BaseWorker.cs
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
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ConnectionReuse
{
	public abstract class BaseWorker
	{
		int requestCount, errorCount;
		List<CancellationTokenSource> workers = new List<CancellationTokenSource> ();

		public int NumWorkers {
			get {
				lock (workers)
					return workers.Count;
			}
		}

		public int RequestCount {
			get { return requestCount; }
		}

		public int ErrorCount {
			get { return errorCount; }
		}

		public void StartOne ()
		{
			var token = new CancellationTokenSource ();
			lock (workers)
				workers.Add (token);
			Task.Factory.StartNew (() => Worker (token.Token), token.Token);
		}


		public void StopOne ()
		{
			lock (workers) {
				if (workers.Count == 0)
					return;
				var index = new Random ().Next (workers.Count);
				var token = workers [index];
				workers.RemoveAt (index);
				token.Cancel ();
			}
		}

		void Worker (CancellationToken token)
		{
			for (;;) {
				token.ThrowIfCancellationRequested ();
				++requestCount;
				try {
					DoRequest (token);
				} catch {
					++errorCount;
				}
			}
		}

		protected abstract string DoRequest (CancellationToken token);
	}
}

