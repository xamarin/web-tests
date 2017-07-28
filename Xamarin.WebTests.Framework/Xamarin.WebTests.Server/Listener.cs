//
// Listener.cs
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
using System.Collections.Generic;
using System.Runtime.ExceptionServices;
using Xamarin.AsyncTests;

namespace Xamarin.WebTests.Server
{
	using ConnectionFramework;
	using HttpFramework;
	using HttpHandlers;
	using TestFramework;

	class Listener : IDisposable
	{
		LinkedList<ListenerContext> connections;
		LinkedList<ListenerTask> listenerTasks;
		bool closed;

		int running;
		CancellationTokenSource cts;
		AsyncManualResetEvent mainLoopEvent;

		int requestParallelConnections;
		TaskCompletionSource<object> finishedEvent;
		Dictionary<string, ListenerOperation> registry;
		volatile bool disposed;
		volatile bool exited;

		static int nextID;
		static long nextRequestID;

		public readonly int ID = ++nextID;

		public ListenerType Type {
			get;
		}

		internal TestContext TestContext {
			get;
		}

		internal ListenerBackend Backend {
			get;
		}

		internal Listener TargetListener {
			get;
		}

		internal Listener ParentListener {
			get;
			private set;
		}

		internal HttpServer Server {
			get;
		}

		internal string ME {
			get;
		}

		public Listener (TestContext ctx, HttpServer server,
				 ListenerType type, ListenerBackend backend)
		{
			TestContext = ctx;
			Server = server;
			Type = type;
			Backend = backend;

			if (backend is ProxyBackend proxyBackend) {
				TargetListener = proxyBackend.Target.Listener;
				TargetListener.ParentListener = this;
			}

			ME = $"{GetType ().Name}({ID}:{Type})";
			registry = new Dictionary<string, ListenerOperation> ();

			connections = new LinkedList<ListenerContext> ();
			listenerTasks = new LinkedList<ListenerTask> ();
			mainLoopEvent = new AsyncManualResetEvent (false);
			finishedEvent = new TaskCompletionSource<object> ();
			cts = new CancellationTokenSource ();
		}

		public bool UsingInstrumentation {
			get { return Type == ListenerType.Instrumentation || Type == ListenerType.Proxy; }
		}

		public int ParallelConnections {
			get { return requestParallelConnections; }
			set {
				lock (this) {
					if (value == requestParallelConnections)
						return;
					requestParallelConnections = value;
					if (running != 0)
						mainLoopEvent.Set ();
				}
			}
		}

		public void Start ()
		{
			lock (this) {
				if (Interlocked.CompareExchange (ref running, 1, 0) != 0)
					throw new InvalidOperationException ();

				if (Type == ListenerType.Instrumentation)
					requestParallelConnections = -1;
				else if (requestParallelConnections == 0)
					requestParallelConnections = 10;

				mainLoopEvent.Set ();
				MainLoop ();
			}
		}

		async void MainLoop ()
		{
			while (!closed) {
				Debug ($"MAIN LOOP");

				var taskList = new List<Task> ();
				var contextList = new List<ListenerTask> ();
				lock (this) {
					RunScheduler ();

					taskList.Add (mainLoopEvent.WaitAsync ());
					PopulateTaskList (contextList, taskList);

					Debug ($"MAIN LOOP #0: {taskList.Count}");
				}

				var finished = await Task.WhenAny (taskList).ConfigureAwait (false);
				Debug ($"MAIN LOOP #1: {finished.Status} {finished == taskList[0]} {taskList[0].Status}");

				lock (this) {
					if (closed)
						break;
					if (finished == taskList[0]) {
						mainLoopEvent.Reset ();
						continue;
					}

					int idx = -1;
					for (int i = 0; i < contextList.Count; i++) {
						if (finished == taskList[i + 1]) {
							idx = i;
							break;
						}
					}

					var task = contextList[idx];
					var context = task.Context;
					listenerTasks.Remove (task);

					Debug ($"MAIN LOOP #2: {idx} {context.State}");

					try {
						context.MainLoopListenerTaskDone (TestContext, cts.Token);
					} catch {
						connections.Remove (context);
						context.Dispose ();
					}
				}
			}

			Debug ($"MAIN LOOP COMPLETE");

			lock (this) {
				var iter = connections.First;
				while (iter != null) {
					var node = iter.Value;
					iter = iter.Next;

					node.Dispose ();
					connections.Remove (node);
				}

				cts.Dispose ();
				exited = true;
			}

			Debug ($"MAIN LOOP COMPLETE #1");

			finishedEvent.SetResult (null);

			void PopulateTaskList (List<ListenerTask> contextList, List<Task> taskList)
			{
				var iter = listenerTasks.First;
				while (iter != null) {
					var node = iter;
					iter = iter.Next;

					contextList.Add (node.Value);
					taskList.Add (node.Value.Task);
				}
			}
		}

		void RunScheduler ()
		{
			do {
				Cleanup ();

				if (!UsingInstrumentation)
					CreateConnections ();
			} while (!StartTasks ());

			void Cleanup ()
			{
				var iter = connections.First;
				while (iter != null) {
					var node = iter;
					var context = node.Value;
					iter = iter.Next;

					if (context.State == ConnectionState.Closed) {
						connections.Remove (node);
						context.Dispose ();
					} else if (context.State == ConnectionState.ReuseConnection) {
						connections.Remove (node);
						var newContext = context.ReuseConnection ();
						connections.AddLast (newContext);
					}
				}
			}

			void CreateConnections ()
			{
				while (connections.Count < requestParallelConnections) {
					Debug ($"RUN SCHEDULER: {connections.Count}");
					var connection = Backend.CreateConnection ();
					connections.AddLast (new ListenerContext (this, connection, false));
					Debug ($"RUN SCHEDULER #1: {connection.ME}");
				}
			}

			bool StartTasks ()
			{
				var listening = false;
				ListenerContext listeningContext = null;
				ListenerContext redirectContext = null;

				var iter = connections.First;
				while (iter != null) {
					var node = iter;
					var context = node.Value;
					iter = iter.Next;

					if (UsingInstrumentation && listening && context.State == ConnectionState.Listening)
						continue;

					if (context.CurrentTask != null)
						continue;

					switch (context.State) {
					case ConnectionState.WaitingForContext:
						if (!UsingInstrumentation)
							throw new InvalidOperationException ();
						continue;

					case ConnectionState.NeedContextForRedirect:
					case ConnectionState.CannotReuseConnection:
						if (redirectContext == null)
							redirectContext = context;
						continue;
					}

					var task = context.MainLoopListenerTask (TestContext, cts.Token);
					listenerTasks.AddLast (task);

					if (context.Listening && !listening) {
						listeningContext = context;
						listening = true;
					}
				}

				if (redirectContext == null)
					return true;

				if (listeningContext == null) {
					var connection = Backend.CreateConnection ();
					listeningContext = new ListenerContext (this, connection, false);
					connections.AddLast (listeningContext);
				}

				redirectContext.Redirect (listeningContext);
				return false;
			}
		}

		(ListenerContext context, bool reused) FindOrCreateContext (HttpOperation operation, bool reuse)
		{
			lock (this) {
				var iter = connections.First;
				while (reuse && iter != null) {
					var node = iter.Value;
					iter = iter.Next;

					if (node.StartOperation (operation)) {
						mainLoopEvent.Set ();
						return (node, true);
					}
				}

				var connection = Backend.CreateConnection ();
				var context = new ListenerContext (this, connection, false);
				context.StartOperation (operation);
				connections.AddLast (context);
				mainLoopEvent.Set ();
				return (context, false);
			}
		}

		public async Task<Response> RunWithContext (TestContext ctx, ListenerOperation operation, Request request,
							    ClientFunc clientFunc, CancellationToken cancellationToken)
		{
			var me = $"{ME}({operation.Operation.ME}) RUN WITH CONTEXT";

			ListenerContext context = null;
			ListenerContext targetContext = null;
			var reusing = !operation.Operation.HasAnyFlags (HttpOperationFlags.DontReuseConnection);

			if (UsingInstrumentation) {
				(context, reusing) = FindOrCreateContext (operation.Operation, reusing);

				ctx.LogDebug (2, $"{me} - CREATE CONTEXT: {reusing} {context.ME}");

				await context.ServerStartTask.ConfigureAwait (false);
			}

			if (TargetListener?.UsingInstrumentation ?? false) {
				(targetContext, _) = TargetListener.FindOrCreateContext (operation.Operation, false);
				ctx.LogDebug (2, $"{me} - CREATE TARGET CONTEXT: {reusing} {targetContext.ME}");
				try {
					await targetContext.ServerStartTask.ConfigureAwait (false);
				} catch {
					context?.Dispose ();
					throw;
				}
			}

			var clientTask = clientFunc (ctx, request, cancellationToken);
			var serverInitTask = operation.ServerInitTask;
			var serverFinishedTask = operation.ServerFinishedTask;

			ExceptionDispatchInfo throwMe = null;
			bool initDone = false, serverDone = false, clientDone = false;

			while (!initDone || !serverDone || !clientDone) {
				ctx.LogDebug (2, $"{me} LOOP: init={initDone} server={serverDone} client={clientDone}");

				if (clientDone) {
					if (operation.Operation.HasAnyFlags (
						HttpOperationFlags.AbortAfterClientExits, HttpOperationFlags.ServerAbortsHandshake,
						HttpOperationFlags.ClientAbortsHandshake)) {
						ctx.LogDebug (2, $"{me} - ABORTING");
						break;
					}
					if (!initDone) {
						ctx.LogDebug (2, $"{me} - ERROR: {clientTask.Result}");
						throwMe = ExceptionDispatchInfo.Capture (new ConnectionException (
							$"{me} client exited before server accepted connection."));
						break;
					}
				}

				var tasks = new List<Task> ();
				if (!initDone)
					tasks.Add (serverInitTask);
				if (!serverDone)
					tasks.Add (serverFinishedTask);
				if (!clientDone)
					tasks.Add (clientTask);
				var finished = await Task.WhenAny (tasks).ConfigureAwait (false);

				string which;
				if (finished == serverInitTask) {
					which = "init";
					initDone = true;
				} else if (finished == serverFinishedTask) {
					which = "server";
					serverDone = true;
				} else if (finished == clientTask) {
					which = "client";
					clientDone = true;
				} else {
					throwMe = ExceptionDispatchInfo.Capture (new InvalidOperationException ());
					break;
				}

				ctx.LogDebug (2, $"{me} #4: {which} exited - {finished.Status}");
				if (finished.Status == TaskStatus.Faulted || finished.Status == TaskStatus.Canceled) {
					if (operation.Operation.HasAnyFlags (HttpOperationFlags.ExpectServerException) &&
					    (finished == serverFinishedTask || finished == serverInitTask))
						ctx.LogDebug (2, $"{me} EXPECTED EXCEPTION {finished.Exception.GetType ()}");
					else {
						ctx.LogDebug (2, $"{me} FAILED: {finished.Exception.Message}");
						throwMe = ExceptionDispatchInfo.Capture (finished.Exception);
						break;
					}
				}
			}

			if (throwMe != null) {
				ctx.LogDebug (2, $"{me} THROWING {throwMe.SourceException.Message}");
				lock (this) {
					operation.OnError (throwMe.SourceException);
					if (context != null)
						context.Dispose ();
					if (targetContext != null)
						targetContext.Dispose ();
					mainLoopEvent.Set ();
				}
				throwMe.Throw ();
			}

			return clientTask.Result;
		}

		internal delegate Task<Response> ClientFunc (TestContext ctx, Request request, CancellationToken cancellationToken);

		void Close ()
		{
			if (closed)
				return;
			closed = true;

			Debug ($"CLOSE");
			cts.Cancel ();

			mainLoopEvent.Set ();
		}

		void Debug (string message)
		{
			TestContext.LogDebug (5, $"{ME}: {message}");
		}

		public ListenerOperation RegisterOperation (TestContext ctx, HttpOperation operation, Handler handler, string path)
		{
			lock (this) {
				if (TargetListener != null) {
					var targetOperation = TargetListener.RegisterOperation (ctx, operation, handler, path);
					registry.Add (targetOperation.Uri.LocalPath, targetOperation);
					return targetOperation;
				}
				if (path == null) {
					var id = Interlocked.Increment (ref nextRequestID);
					path = $"/id/{operation.ID}/{handler.GetType ().Name}/";
				}
				var me = $"{nameof (RegisterOperation)}({handler.Value})";
				Debug ($"{me} {path}");
				var uri = new Uri (Server.TargetUri, path);
				var listenerOperation = new ListenerOperation (this, operation, handler, uri);
				registry.Add (path, listenerOperation);
				return listenerOperation;
			}
		}

		internal ListenerOperation GetOperation (ListenerContext context, HttpRequest request)
		{
			ListenerOperation operation;
			lock (this) {
				var me = $"{nameof (GetOperation)}({context.Connection.ME})";
				Debug ($"{me} {request.Method} {request.Path} {request.Protocol}");

				if (!registry.ContainsKey (request.Path)) {
					Debug ($"{me} INVALID PATH: {request.Path}!");
					return null;
				}

				operation = registry[request.Path];
				registry.Remove (request.Path);
				Server.BumpRequestCount ();
			}
			ParentListener?.UnregisterOperation (operation);
			return operation;
		}

		internal void UnregisterOperation (ListenerOperation redirect)
		{
			lock (this) {
				registry.Remove (redirect.Uri.LocalPath);
			}
		}

		internal static Task FailedTask (Exception ex)
		{
			var tcs = new TaskCompletionSource<object> ();
			if (ex is OperationCanceledException)
				tcs.SetCanceled ();
			else
				tcs.SetException (ex);
			return tcs.Task;
		}

		public Task Shutdown ()
		{
			lock (this) {
				if (!closed && !disposed && !exited) {
					closed = true;
					mainLoopEvent.Set ();
				}
				return finishedEvent.Task;
			}
		}

		public void Dispose ()
		{
			lock (this) {
				if (disposed)
					return;
				disposed = true;
				Close ();
				Backend.Dispose ();
			}
		}
	}
}
