﻿﻿﻿//
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

namespace Xamarin.WebTests.Server {
	using ConnectionFramework;
	using HttpFramework;
	using TestFramework;

	abstract class BuiltinListener {
		int currentConnections;
		Exception currentError;
		volatile TaskCompletionSource<bool> tcs;
		volatile CancellationTokenSource cts;

		internal TestContext TestContext {
			get;
		}

		internal HttpServer Server {
			get;
		}

		public BuiltinListener (TestContext ctx, HttpServer server)
		{
			TestContext = ctx;
			Server = server;
		}

		public Task Start ()
		{
			lock (this) {
				if (cts != null)
					throw new InvalidOperationException ();

				cts = new CancellationTokenSource ();
				tcs = new TaskCompletionSource<bool> ();
			}

			return Task.Run (() => {
				Listen ();
			});
		}

		void Listen ()
		{
			Interlocked.Increment (ref currentConnections);
			TestContext.LogDebug (5, "LISTEN: {0} {1}", this, currentConnections);
			AcceptAsync (cts.Token).ContinueWith (OnAccepted);
		}

		void OnAccepted (Task<HttpConnection> task)
		{
			if (task.IsCanceled || cts.IsCancellationRequested) {
				OnFinished ();
				return;
			}
			if (task.IsFaulted) {
				ConnectionTestHelper.CopyError (ref currentError, task);
				return;
			}

			Listen ();

			var connection = task.Result;

			MainLoop (connection, cts.Token).ContinueWith (t => {
				TestContext.LogDebug (5, "MAIN LOOP DONE: {0} {1}", this, t.Status);
				if (t.IsFaulted)
					ConnectionTestHelper.CopyError (ref currentError, t);
				if (t.IsCompleted)
					connection.Dispose ();

				OnFinished ();
			});
		}

		void OnFinished ()
		{
			lock (this) {
				var connections = Interlocked.Decrement (ref currentConnections);
				var error = Interlocked.Exchange (ref currentError, null);

				TestContext.LogDebug (5, "ON FINISHED: {0} {1} {2}", this, connections, error);

				if (error != null) {
					tcs.SetException (error);
					return;
				}

				if (connections > 0)
					return;
				tcs.SetResult (true);
			}
		}

		public async Task Stop ()
		{
			TestContext.LogDebug (5, "STOP: {0}", this);
			cts.Cancel ();
			Shutdown ();
			await tcs.Task;
			OnStop ();

			lock (this) {
				cts.Dispose ();
				cts = null;
				tcs = null;
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

		public abstract Task<HttpConnection> AcceptAsync (CancellationToken cancellationToken);

		protected abstract Task<bool> HandleConnection (HttpConnection connection, CancellationToken cancellationToken);

		async Task MainLoop (HttpConnection connection, CancellationToken cancellationToken)
		{
			cancellationToken.ThrowIfCancellationRequested ();
			if (!await Server.InitializeConnection (TestContext, connection, cancellationToken).ConfigureAwait (false))
				return;

			while (!cancellationToken.IsCancellationRequested) {
				var wantToReuse = await HandleConnection (connection, cancellationToken);
				if (!wantToReuse || cancellationToken.IsCancellationRequested)
					break;

				bool connectionAvailable = connection.IsStillConnected ();
				if (!connectionAvailable && !cancellationToken.IsCancellationRequested)
					throw new ConnectionException ("Expecting another connection, but socket has been shut down.");
			}
		}
	}
}
