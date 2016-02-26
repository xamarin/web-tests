//
// ClientAndServer.cs
//
// Author:
//       Martin Baulig <martin.baulig@xamarin.com>
//
// Copyright (c) 2015 Xamarin, Inc.
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
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xamarin.AsyncTests;
using Xamarin.AsyncTests.Constraints;
using Xamarin.AsyncTests.Portable;

namespace Xamarin.WebTests.ConnectionFramework
{
	public abstract class ClientAndServer : Connection
	{
		IServer server;
		IClient client;

		public IServer Server {
			get { return server; }
		}

		public IClient Client {
			get { return client; }
		}

		public override bool SupportsCleanShutdown {
			get { return server.SupportsCleanShutdown && client.SupportsCleanShutdown; }
		}

		public override ProtocolVersions SupportedProtocols {
			get { return server.SupportedProtocols & client.SupportedProtocols; }
		}

		public ProtocolVersions? GetRequestedProtocol ()
		{
			var supported = SupportedProtocols;
			var requested = Parameters.ProtocolVersion;

			if (requested != null) {
				requested &= supported;
				return requested;
			}

			return null;
		}

		public ClientAndServer (IServer server, IClient client, ConnectionParameters parameters)
			: base (server.EndPoint, parameters)
		{
			this.server = server;
			this.client = client;

			var requested = GetRequestedProtocol ();
			if (requested != null) {
				if (requested == ProtocolVersions.Unspecified)
					throw new NotSupportedException ("Incompatible protocol versions between client and server.");
				Parameters.ProtocolVersion = requested.Value;
			}
		}

		public bool IsManualClient {
			get { return Client.Provider.Type == ConnectionProviderType.Manual; }
		}

		public bool IsManualServer {
			get { return Server.Provider.Type == ConnectionProviderType.Manual; }
		}

		public bool IsManualConnection {
			get { return IsManualClient || IsManualServer; }
		}

		public async Task Run (TestContext ctx, CancellationToken cancellationToken)
		{
			try {
				await WaitForConnection (ctx, cancellationToken);
			} catch (ConnectionFinishedException) {
				return;
			}

			cancellationToken.ThrowIfCancellationRequested ();
			await OnRun (ctx, cancellationToken);

			cancellationToken.ThrowIfCancellationRequested ();
			await MainLoop (ctx, cancellationToken);

			cancellationToken.ThrowIfCancellationRequested ();
			if (SupportsCleanShutdown)
				await Shutdown (ctx, cancellationToken);
		}

		protected virtual void InitializeConnection (TestContext ctx)
		{
		}

		protected virtual Task OnRun (TestContext ctx, CancellationToken cancellationToken)
		{
			return FinishedTask;
		}

		protected abstract Task MainLoop (TestContext ctx, CancellationToken cancellationToken);

		protected virtual void OnWaitForServerConnectionCompleted (TestContext ctx, Task task)
		{
			if (task.IsFaulted)
				throw task.Exception;

			ctx.Assert (task.Status, Is.EqualTo (TaskStatus.RanToCompletion), "expecting success");
		}

		protected virtual void OnWaitForClientConnectionCompleted (TestContext ctx, Task task)
		{
			if (task.IsFaulted)
				throw task.Exception;

			ctx.Assert (task.Status, Is.EqualTo (TaskStatus.RanToCompletion), "expecting success");
		}

		public override async Task Start (TestContext ctx, CancellationToken cancellationToken)
		{
			ctx.LogMessage ("Starting client and server: {0} {1} {2}", client, server, server.EndPoint);
			InitializeConnection (ctx);
			await server.Start (ctx, cancellationToken);
			await client.Start (ctx, cancellationToken);
		}

		protected virtual Task WaitForServerConnection (TestContext ctx, CancellationToken cancellationToken)
		{
			var task = server.WaitForConnection (ctx, cancellationToken);
			return task.ContinueWith (t => OnWaitForServerConnectionCompleted (ctx, t));
		}

		protected virtual Task WaitForClientConnection (TestContext ctx, CancellationToken cancellationToken)
		{
			var task = client.WaitForConnection (ctx, cancellationToken);
			return task.ContinueWith (t => OnWaitForClientConnectionCompleted (ctx, t));
		}

		static void CopyError (TestContext ctx, ref Exception error, Task task)
		{
			if (!task.IsFaulted)
				return;

			var aggregate = task.Exception;

		again:
			if (aggregate.InnerExceptions.Count > 1) {
				error = aggregate;
				return;
			}

			var inner = aggregate.InnerExceptions [0];
			var aggregate2 = inner as AggregateException;
			if (aggregate2 != null && aggregate2 != aggregate) {
				aggregate = aggregate2;
				goto again;
			}

			if (inner is ObjectDisposedException)
				return;

			var io = inner as IOException;
			if (io != null) {
				if (io.InnerException is ObjectDisposedException)
					return;
				if (error != null)
					return;
			}

			if (error == null) {
				error = inner;
				return;
			}

			var newInner = new List<Exception> ();

			var oldAggregate = error as AggregateException;
			if (oldAggregate != null)
				newInner.AddRange (oldAggregate.InnerExceptions);
			newInner.Add (inner);

			error = new AggregateException (newInner);
		}

		public override async Task WaitForConnection (TestContext ctx, CancellationToken cancellationToken)
		{
			var serverTask = WaitForServerConnection (ctx, cancellationToken);
			var clientTask = WaitForClientConnection (ctx, cancellationToken);

			var t1 = clientTask.ContinueWith (t => {
				if (t.IsFaulted || t.IsCanceled)
					server.Dispose ();
			});
			var t2 = serverTask.ContinueWith (t => {
				if (t.IsFaulted || t.IsCanceled)
					client.Dispose ();
			});

			try {
				await Task.WhenAll (serverTask, clientTask, t1, t2);
			} catch (ConnectionFinishedException) {
				throw;
			} catch (Exception ex) {
				Exception error = null;
				CopyError (ctx, ref error, clientTask);
				CopyError (ctx, ref error, serverTask);
				if (error == null)
					error = ex;
				throw error;
			}
		}

		protected override void Stop ()
		{
			client.Dispose ();
			server.Dispose ();
		}

		public override async Task<bool> Shutdown (TestContext ctx, CancellationToken cancellationToken)
		{
			var clientShutdown = client.Shutdown (ctx, cancellationToken);
			var serverShutdown = server.Shutdown (ctx, cancellationToken);
			await Task.WhenAll (clientShutdown, serverShutdown);
			return clientShutdown.Result && serverShutdown.Result;
		}

		protected class ConnectionFinishedException : Exception
		{
		}
	}
}

