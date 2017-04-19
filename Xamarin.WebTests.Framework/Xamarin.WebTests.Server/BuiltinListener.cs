﻿﻿//
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
	using HttpFramework;

	abstract class BuiltinListener
	{
		int currentConnections;
		volatile Exception currentError;
		volatile TaskCompletionSource<bool> tcs;
		volatile CancellationTokenSource cts;

		internal TestContext TestContext {
			get;
		}

		public BuiltinListener (TestContext ctx)
		{
			TestContext = ctx;
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
			AcceptAsync (cts.Token).ContinueWith (OnAccepted);
		}

		void OnAccepted (Task<BuiltinListenerContext> task)
		{
			if (task.IsCanceled || cts.IsCancellationRequested) {
				OnFinished ();
				return;
			}
			if (task.IsFaulted) {
				OnException (task.Exception);
				return;
			}

			Listen ();

			var context = task.Result;

			MainLoop (context, cts.Token).ContinueWith (t => {
				if (t.IsFaulted)
					OnException (t.Exception);
				if (t.IsCompleted)
					context.Dispose ();

				OnFinished ();
			});
		}

		protected void OnException (Exception error)
		{
			lock (this) {
				if (currentError == null) {
					currentError = error;
					return;
				}

				var aggregated = currentError as AggregateException;
				if (aggregated == null) {
					currentError = new AggregateException (error);
					return;
				}

				var inner = aggregated.InnerExceptions.ToList ();
				inner.Add (error);
				currentError = new AggregateException (inner);
			}
		}

		protected void OnFinished ()
		{
			lock (this) {
				var connections = Interlocked.Decrement (ref currentConnections);

				if (connections > 0)
					return;

				if (currentError != null)
					tcs.SetException (currentError);
				else
					tcs.SetResult (true);
			}
		}

		public async Task Stop ()
		{
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

		public abstract Task<BuiltinListenerContext> AcceptAsync (CancellationToken cancellationToken);

		protected abstract Task<HttpConnection> CreateConnection (BuiltinListenerContext context, CancellationToken cancellationToken);

		protected abstract Task<bool> HandleConnection (BuiltinListenerContext context, HttpConnection connection, CancellationToken cancellationToken);

		async Task MainLoop (BuiltinListenerContext context, CancellationToken cancellationToken)
		{
			cancellationToken.ThrowIfCancellationRequested ();
			var connection = await CreateConnection (context, cancellationToken).ConfigureAwait (false);
			if (connection == null)
				return;

			while (!cancellationToken.IsCancellationRequested) {
				var wantToReuse = await HandleConnection (context, connection, cancellationToken);
				if (!wantToReuse || cancellationToken.IsCancellationRequested)
					break;

				bool connectionAvailable = context.IsStillConnected ();
				if (!connectionAvailable && !cancellationToken.IsCancellationRequested)
					throw new InvalidOperationException ("Expecting another connection, but socket has been shut down.");
			}
		}
	}
}
