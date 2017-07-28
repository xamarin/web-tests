//
// ParallelListener.cs
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
using System.Collections.Generic;
using Xamarin.AsyncTests;

namespace Xamarin.WebTests.Server
{
	using ConnectionFramework;
	using HttpFramework;

	class ParallelListener : Listener
	{
		LinkedList<ParallelListenerContext> connections;
		bool closed;

		int running;
		CancellationTokenSource cts;
		AsyncManualResetEvent mainLoopEvent;

		int requestParallelConnections;

		internal int RequestParallelConnections {
			get { return requestParallelConnections; }
			set {
				lock (this) {
					if (requestParallelConnections == value)
						return;
					requestParallelConnections = value;
					Run ();
				}
			}
		}

		public ParallelListener (TestContext ctx, HttpServer server, ListenerBackend backend)
			: base (ctx, server, backend)
		{
			connections = new LinkedList<ParallelListenerContext> ();
			mainLoopEvent = new AsyncManualResetEvent (false);
			cts = new CancellationTokenSource ();

			ctx.LogDebug (5, $"{ME} TEST: {ctx.Result}");
		}

		public void Run ()
		{
			lock (this) {
				if (Interlocked.CompareExchange (ref running, 1, 0) == 0)
					MainLoop ();

				mainLoopEvent.Set ();
			}
		}

		async void MainLoop ()
		{
			while (!closed) {
				Debug ($"MAIN LOOP");

				var taskList = new List<Task> ();
				var connectionArray = new List<ParallelListenerContext> ();
				lock (this) {
					RunScheduler ();

					taskList.Add (mainLoopEvent.WaitAsync ());
					foreach (var context in connections) {
						Task task = null;
						switch (context.State) {
						case ConnectionState.None:
							task = context.Run (TestContext, cts.Token);
							break;
						case ConnectionState.HasRequest:
							task = context.HandleRequest (TestContext, cts.Token);
							break;
						}
						if (task != null) {
							connectionArray.Add (context);
							taskList.Add (task);
						}
					}
				}

				Debug ($"MAIN LOOP #0: {connectionArray.Count}");

				var ret = await Task.WhenAny (taskList).ConfigureAwait (false);
				Debug ($"MAIN LOOP #1: {ret.Status} {ret == taskList[0]}");

				lock (this) {
					if (ret == taskList[0]) {
						mainLoopEvent.Reset ();
						continue;
					}

					int idx = -1;
					for (int i = 0; i < connectionArray.Count; i++) {
						if (ret == taskList[i + 1]) {
							idx = i;
							break;
						}
					}

					bool ok = false;
					var context = connectionArray[idx];
					Debug ($"MAIN LOOP #2: {context.State} {context.Connection.ME}");

					if (ret.Status == TaskStatus.Canceled || ret.Status == TaskStatus.Faulted) {
						Debug ($"MAIN LOOP #2 FAILED: {ret.Status} {ret.Exception?.Message}");
						connections.Remove (context);
						context.Dispose ();
						continue;
					}

					HttpRequest request;
					ParallelListenerOperation operation;

					switch (context.State) {
					case ConnectionState.Accepted:
						request = ((Task<HttpRequest>)ret).Result;
						operation = (ParallelListenerOperation)GetOperation (context, request);
						if (operation == null)
							break;
						context.StartOperation (operation, request);
						ok = true;
						break;
					case ConnectionState.HasRequest:
						ok = RequestComplete (context, ret);
						break;
					}

					if (!ok) {
						connections.Remove (context);
						context.Dispose ();
					}
				}
			}
		}

		void RunScheduler ()
		{
			while (connections.Count < requestParallelConnections) {
				Debug ($"RUN SCHEDULER: {connections.Count}");
				var connection = Backend.CreateConnection ();
				connections.AddLast (new ParallelListenerContext (this, connection));
				Debug ($"RUN SCHEDULER #1: {connection.ME}");
			}
		}

		bool RequestComplete (ParallelListenerContext context, Task task)
		{
			var me = $"{nameof (RequestComplete)}({context.Connection.ME})";
			if (task.Status == TaskStatus.Canceled || task.Status == TaskStatus.Faulted) {
				Debug ($"{me} FAILED: {task.Status} {task.Exception?.Message}");
				return false;
			}

			Debug ($"{me}");

			return false;
		}

		protected override ListenerOperation CreateOperation (HttpOperation operation, Uri uri)
		{
			return new ParallelListenerOperation (this, operation, uri);
		}

		protected override void Close ()
		{
			closed = true;
			cts.Cancel ();

			var iter = connections.First;
			while (iter != null) {
				var node = iter.Value;
				iter = iter.Next;

				node.Connection.Dispose ();
				connections.Remove (node);
			}

			cts.Dispose ();
			mainLoopEvent.Set ();
		}
	}
}
