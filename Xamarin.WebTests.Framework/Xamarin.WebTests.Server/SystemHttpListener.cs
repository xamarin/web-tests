﻿//
// SystemHttpListener.cs
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
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Xamarin.AsyncTests;
using Xamarin.AsyncTests.Portable;
using Xamarin.WebTests.ConnectionFramework;
using Xamarin.WebTests.HttpFramework;

namespace Xamarin.WebTests.Server {
	class SystemHttpListener {
		public HttpListenerServer Server {
			get;
		}

		internal TestContext TestContext {
			get;
		}

		public SystemHttpListener (TestContext ctx, HttpListenerServer server)
		{
			TestContext = ctx;

			Server = server;
		}

		HttpListener listener;
		TaskCompletionSource<bool> tcs;
		CancellationTokenSource cts;

		public Task Start ()
		{
			if (Interlocked.CompareExchange (ref tcs, new TaskCompletionSource<bool> (), null) != null)
				throw new InternalErrorException ();

			return Task.Run (() => {
				TestContext.LogDebug (4, "Starting HttpListener: {0}.", Server.Uri.AbsoluteUri);
				cts = new CancellationTokenSource ();
				listener = new HttpListener ();
				listener.Prefixes.Add (Server.Uri.AbsoluteUri);
				listener.Start ();

				TestContext.LogDebug (4, "Listener running.");

				listener.GetContextAsync ().ContinueWith (OnGetContext);
			});
		}

		async Task OnGetContext (Task<HttpListenerContext> task)
		{
			if (task.IsFaulted) {
				tcs.TrySetException (task.Exception);
				return;
			} else if (task.IsCanceled) {
				tcs.TrySetCanceled ();
				return;
			}

			try {
				var result = await HandleRequest (task.Result, cts.Token).ConfigureAwait (false);
				tcs.TrySetResult (result);
			} catch (OperationCanceledException) {
				tcs.TrySetCanceled ();
			} catch (Exception ex) {
				tcs.TrySetException (ex);
			}
		}

		async Task<bool> HandleRequest (HttpListenerContext context, CancellationToken cancellationToken)
		{
			cancellationToken.ThrowIfCancellationRequested ();
			TestContext.LogDebug (4, "Handle request: {0} {1}", context, context.Request);

			var connection = new HttpListenerConnection (TestContext, Server, context);
			var request = await connection.ReadRequest (cancellationToken);

			cancellationToken.ThrowIfCancellationRequested ();
			TestContext.LogDebug (4, "Handle request #1: {0} {1}", context, request);
			var result = await Server.HandleConnection (TestContext, connection, request, cancellationToken);

			TestContext.LogDebug (4, "Handle request #2: {0} {1} {2}", context, request, result);

			context.Response.Close ();

			return result;
		}

		public async Task Stop ()
		{
			TestContext.LogDebug (4, "Listener stop.");

			cts.Cancel ();
			listener.Abort ();
			listener.Close ();
			listener = null;

			await tcs.Task;

			lock (this) {
				cts.Dispose ();
				cts = null;
				tcs = null;
			}
		}
	}
}
