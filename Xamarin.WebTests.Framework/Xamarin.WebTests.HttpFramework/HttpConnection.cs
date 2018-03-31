﻿﻿﻿﻿//
// Connection.cs
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
using System.Net.Security;
using System.Text;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xamarin.AsyncTests;
using Xamarin.AsyncTests.Portable;
using Xamarin.AsyncTests.Constraints;

namespace Xamarin.WebTests.HttpFramework
{
	using ConnectionFramework;
	using Server;

	public abstract class HttpConnection : IDisposable
	{
		public HttpServer Server {
			get;
		}

		public abstract SslStream SslStream {
			get;
		}

		public abstract HttpListenerContext ListenerContext {
			get;
		}

		static int nextId;
		public readonly int ID = ++nextId;

		public string ME {
			get;
		}

		internal HttpConnection (HttpServer server)
		{
			Server = server;
			ME = $"[{GetType ().Name}({ID}:{server.ME})]";
		}

		public event EventHandler ClosedEvent;

		public abstract IPEndPoint RemoteEndPoint {
			get;
		}

		internal abstract bool IsStillConnected ();

		public abstract Task AcceptAsync (TestContext ctx, CancellationToken cancellationToken);

		public abstract Task Initialize (TestContext ctx, HttpOperation operation, CancellationToken cancellationToken);

		public abstract Task<bool> ReuseConnection (TestContext ctx, CancellationToken cancellationToken);

		public async Task<bool> WaitForRequest (int timeout, CancellationToken cancellationToken)
		{
			using (var cts = CancellationTokenSource.CreateLinkedTokenSource (cancellationToken)) {
				cts.CancelAfter (timeout);
				var timeoutTask = Task.Delay (timeout);
				var workerTask = WaitForRequest (cts.Token);
				var ret = await Task.WhenAny (timeoutTask, workerTask).ConfigureAwait (false);
				if (cts.Token.IsCancellationRequested || ret == timeoutTask)
					throw new ConnectionException ("Timeout while waiting for request.");
				return workerTask.Result;
			}
		}

		public abstract Task<bool> WaitForRequest (CancellationToken cancellationToken);

		public async Task<HttpRequest> ReadRequest (TestContext ctx, CancellationToken cancellationToken)
		{
			cancellationToken.ToString ();
			var request = await ReadRequestHeader (ctx, cancellationToken).ConfigureAwait (false);

			cancellationToken.ThrowIfCancellationRequested ();
			await request.Read (ctx, cancellationToken);
			return request;
		}

		public abstract Task<HttpRequest> ReadRequestHeader (TestContext ctx, CancellationToken cancellationToken);

		public abstract Task<HttpResponse> ReadResponse (TestContext ctx, CancellationToken cancellationToken);

		internal abstract Task WriteRequest (TestContext ctx, HttpRequest request, CancellationToken cancellationToken);

		internal abstract Task WriteResponse (TestContext ctx, HttpResponse response, CancellationToken cancellationToken);

		int disposed;

		protected abstract void Close ();

		public void Dispose ()
		{
			if (Interlocked.CompareExchange (ref disposed, 1, 0) != 0)
				return;

			ClosedEvent?.Invoke (this, EventArgs.Empty);
			Close ();
		}
	}
}
