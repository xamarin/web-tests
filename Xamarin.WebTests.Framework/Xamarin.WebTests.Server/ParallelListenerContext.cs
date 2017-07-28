//
// ParallelListenerContext.cs
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

	class ParallelListenerContext : ListenerContext
	{
		public ParallelListenerContext (ParallelListener listener, HttpConnection connection)
			: base (listener)
		{
			this.connection = connection;
		}

		public override HttpConnection Connection {
			get { return connection; }
		}

		public ParallelListenerOperation Operation {
			get { return currentOperation; }
		}

		public HttpRequest Request {
			get;
			private set;
		}

		ParallelListenerOperation currentOperation;
		HttpConnection connection;

		public void StartOperation (ParallelListenerOperation operation, HttpRequest request)
		{
			if (Interlocked.CompareExchange (ref currentOperation, operation, null) != null)
				throw new InvalidOperationException ();
			Request = request;
			State = ConnectionState.HasRequest;
		}

		public override void Continue ()
		{
			currentOperation = null;
		}

		public override Task ServerInitTask => throw new NotImplementedException ();

		public override Task ServerStartTask => throw new NotImplementedException ();

		TaskCompletionSource<HttpRequest> initTask;

		public override Task Run (TestContext ctx, CancellationToken cancellationToken)
		{
			return MyRun (ctx, cancellationToken);
		}

		Task<HttpRequest> MyRun (TestContext ctx, CancellationToken cancellationToken)
		{
			var tcs = new TaskCompletionSource<HttpRequest> ();
			var old = Interlocked.CompareExchange (ref initTask, tcs, null);
			if (old != null)
				return old.Task;

			Run_inner ().ContinueWith (t => {
				State = ConnectionState.Accepted;
				if (t.Status == TaskStatus.Canceled)
					tcs.TrySetCanceled ();
				else if (t.Status == TaskStatus.Faulted)
					tcs.TrySetException (t.Exception);
				else
					tcs.TrySetResult (t.Result);
			});

			return tcs.Task;

			async Task<HttpRequest> Run_inner ()
			{
				var me = $"{Listener.ME}({Connection.ME}) RUN";
				cancellationToken.ThrowIfCancellationRequested ();
				await Connection.AcceptAsync (ctx, cancellationToken).ConfigureAwait (false);

				ctx.LogDebug (5, $"{me} #1");

				cancellationToken.ThrowIfCancellationRequested ();
				await Connection.Initialize (ctx, null, cancellationToken);

				ctx.LogDebug (5, $"{me} #2");

				var reader = new HttpStreamReader (Connection.SslStream);
				cancellationToken.ThrowIfCancellationRequested ();
				var header = await reader.ReadLineAsync (cancellationToken);
				var (method, protocol, path) = HttpMessage.ReadHttpHeader (header);
				ctx.LogDebug (5, $"{me} #3: {method} {protocol} {path}");

				var request = new HttpRequest (protocol, method, path, reader);
				return request;
			}
		}

		public Task HandleRequest (TestContext ctx, CancellationToken cancellationToken)
		{
			return Operation.HandleRequest (ctx, Connection, Request, cancellationToken);
		}

		public override void PrepareRedirect (TestContext ctx, HttpConnection connection, bool keepAlive)
		{
			throw new NotImplementedException ();
		}

		protected override void Close ()
		{
			if (connection != null) {
				connection.Dispose ();
				connection = null;
			}
		}
	}
}
