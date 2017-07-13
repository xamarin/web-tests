//
// BuiltinListener.cs
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
using System.IO;
using System.Net;
using System.Linq;
using System.Text;
using System.Threading;
using System.Reflection;
using System.Net.Sockets;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Net.Security;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using SD = System.Diagnostics;

using Xamarin.AsyncTests;
using Xamarin.AsyncTests.Portable;

namespace Xamarin.WebTests.Server
{
	using ConnectionFramework;
	using HttpFramework;
	using TestFramework;

	abstract class BuiltinListener
	{
		int currentConnections;
		Exception currentError;
		LinkedList<HttpConnection> connections;
		volatile TaskCompletionSource<bool> tcs;
		volatile CancellationTokenSource cts;
		volatile bool closed;

		static int nextID;
		public readonly int ID = ++nextID;

		internal TestContext TestContext {
			get;
		}

		internal HttpServer Server {
			get;
		}

		internal string ME {
			get;
		}

		public BuiltinListener (TestContext ctx, HttpServer server)
		{
			TestContext = ctx;
			Server = server;
			ME = $"BuiltinListener({ID})";
			connections = new LinkedList<HttpConnection> ();
		}

		public Task Start ()
		{
			if ((Server.Flags & HttpServerFlags.NewListener) != 0)
				throw new NotSupportedException ();

			lock (this) {
				if (cts != null)
					throw new InvalidOperationException ();

				cts = new CancellationTokenSource ();
				tcs = new TaskCompletionSource<bool> ();
			}

			TestContext.LogDebug (5, $"{ME}: START: {currentConnections}");

			return Task.Run (() => {
				Listen (false);
			});
		}

		void Listen (bool singleRequest)
		{
			Interlocked.Increment (ref currentConnections);
			TestContext.LogDebug (5, $"{ME}: LISTEN: {singleRequest} {currentConnections}");
			AcceptAsync (cts.Token).ContinueWith (t => OnAccepted (singleRequest, t));
		}

		void OnAccepted (bool singleRequest, Task<HttpConnection> task)
		{
			if (task.IsCanceled || cts.IsCancellationRequested) {
				OnFinished ();
				return;
			}
			if (task.IsFaulted) {
				TestContext.AddException (ref currentError, task);
				OnFinished ();
				return;
			}

			if (!singleRequest)
				Listen (false);

			var connection = task.Result;

			MainLoop (connection, cts.Token).ContinueWith (t => {
				TestContext.LogDebug (5, $"{ME}: MAIN LOOP DONE: {connection.RemoteEndPoint} {t.Status} {t.Exception?.Message}");
				if (t.IsFaulted) {
					TestContext.LogDebug (5, $"{ME}: MAIN LOOP DONE - EX: {connection.RemoteEndPoint} {t.Exception}");
					TestContext.AddException (ref currentError, t);
				}
				if (t.IsCompleted)
					connection.Dispose ();

				OnFinished ();
			});
		}

		void OnFinished ()
		{
			lock (this) {
				var newCount = Interlocked.Decrement (ref currentConnections);
				var error = Interlocked.Exchange (ref currentError, null);

				TestContext.LogDebug (5, $"{ME}: ON FINISHED: {newCount} {error}");

				if (error != null) {
					tcs.SetException (error);
					return;
				}

				if (newCount > 0)
					return;
				tcs.SetResult (true);
			}
		}

		public virtual void CloseAll ()
		{
			lock (this) {
				closed = true;
				TestContext.LogDebug (5, $"{ME}: CLOSE ALL");

				var iter = connections.First;
				while (iter != null) {
					var node = iter.Value;
					iter = iter.Next;

					node.Dispose ();
					connections.Remove (node);
				}

				cts?.Cancel ();
			}
		}

		public async Task Stop ()
		{
			TestContext.LogDebug (5, $"{ME}: STOP: {this}");
			cts?.Cancel ();
			Shutdown ();
			TestContext.LogDebug (5, $"{ME}: STOP #1: {currentConnections}");

			if ((Server.Flags & HttpServerFlags.NewListener) != 0)
				return;

			try {
				await tcs.Task;
				TestContext.LogDebug (5, $"{ME}: STOP #2: {currentConnections}");
				OnStop ();

				lock (this) {
					cts.Dispose ();
					cts = null;
					tcs = null;
				}
			} catch (Exception ex) {
				TestContext.LogDebug (5, $"{ME}: STOP ERROR: {ex}");
				throw;
			}
		}

		protected virtual void Shutdown ()
		{
		}

		protected virtual void OnStop ()
		{
		}

		public async Task<T> RunWithContext<T> (TestContext ctx, Func<CancellationToken, Task<T>> func, CancellationToken cancellationToken)
		{
			using (var newCts = CancellationTokenSource.CreateLinkedTokenSource (cancellationToken, cts.Token)) {
				var userTask = func (newCts.Token);
				var serverTask = tcs.Task;
				var result = await Task.WhenAny (userTask, serverTask).ConfigureAwait (false);
				if (result.IsFaulted)
					throw result.Exception;
				if (result.IsCanceled)
					throw new OperationCanceledException ();
				if (result == serverTask)
					throw new ConnectionException ("Listener `{0}' exited before client operation finished.", this);
				if (result.Status == TaskStatus.RanToCompletion)
					return userTask.Result;
				throw new ConnectionException ("User task finished with unknown status `{0}'.", result.Status);
			}
		}

		HttpConnection FindIdleConnection (TestContext ctx, HttpOperation operation)
		{
			var iter = connections.First;
			while (iter != null) {
				var node = iter.Value;
				iter = iter.Next;

				if (node.StartOperation (ctx, operation))
					return node;
			}

			return null;
		}

		public (HttpConnection connection, bool reused) CreateConnection (
			TestContext ctx, HttpOperation operation, bool reuse)
		{
			lock (this) {
				HttpConnection connection = null;
				if (reuse)
					connection = FindIdleConnection (ctx, operation);

				if (connection != null) {
					ctx.LogDebug (5, $"{ME} REUSING CONNECTION: {connection} {connections.Count}");
					return (connection, true);
				}

				connection = CreateConnection ();
				ctx.LogDebug (5, $"{ME} CREATE CONNECTION: {connection} {connections.Count}");
				connections.AddLast (connection);
				connection.ClosedEvent += (sender, e) => {
					lock (this) {
						if (!e)
							connections.Remove (connection);
					}
				};
				if (!connection.StartOperation (ctx, operation))
					throw new InvalidOperationException ();
				return (connection, false);
			}
		}

		protected abstract HttpConnection CreateConnection ();

		public abstract Task<HttpConnection> AcceptAsync (CancellationToken cancellationToken);

		async Task MainLoop (HttpConnection connection, CancellationToken cancellationToken)
		{
			cancellationToken.ThrowIfCancellationRequested ();
			if (!await Server.InitializeConnection (TestContext, connection, cancellationToken).ConfigureAwait (false))
				return;

			while (!cancellationToken.IsCancellationRequested) {
				TestContext.LogDebug (5, $"{ME}: MAIN LOOP: {connection.RemoteEndPoint}");
				var wantToReuse = await Server.HandleConnection (TestContext, null, connection, cancellationToken);
				TestContext.LogDebug (5, $"{ME}: MAIN LOOP #1: {connection.RemoteEndPoint} {wantToReuse}");
				if (!wantToReuse || cancellationToken.IsCancellationRequested)
					break;
	
				bool connectionAvailable = connection.IsStillConnected ();
				TestContext.LogDebug (5, $"{ME}: MAIN LOOP #2: {connection.RemoteEndPoint} {connectionAvailable} {closed} {cancellationToken.IsCancellationRequested}");
				if (!closed && !connectionAvailable && !cancellationToken.IsCancellationRequested)
				{
					TestContext.LogMessage ("Expecting another connection, but socket has been shut down.");
					// throw new ConnectionException ("Expecting another connection, but socket has been shut down.");
					return;
				}
			}
		}
	}
}
