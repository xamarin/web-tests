﻿﻿﻿﻿﻿//
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
using Xamarin.WebTests.TestFramework;

namespace Xamarin.WebTests.ConnectionFramework
{
	public abstract class ClientAndServer : AbstractConnection
	{
		public Connection Server {
			get;
			private set;
		}

		public Connection Client {
			get;
			private set;
		}

		public ConnectionParameters Parameters {
			get;
			private set;
		}

		protected abstract ConnectionParameters CreateParameters (TestContext ctx);

		public bool IsManualClient {
			get { return Client.Provider.Type == ConnectionProviderType.Manual; }
		}

		public bool IsManualServer {
			get { return Server.Provider.Type == ConnectionProviderType.Manual; }
		}

		public bool IsManualConnection {
			get { return IsManualClient || IsManualServer; }
		}

		void Initialize (TestContext ctx)
		{
			Parameters = CreateParameters (ctx);

			if (ctx.TryGetParameter<ProtocolVersions> (out var protocolVersion))
				Parameters.ProtocolVersion = protocolVersion;

			var provider = ctx.GetParameter<ConnectionTestProvider> ();

			if (Parameters.EndPoint == null) {
				if (Parameters.ListenAddress != null)
					Parameters.EndPoint = Parameters.ListenAddress;
				else
					Parameters.EndPoint = ConnectionTestHelper.GetEndPoint ();
			}

			Server = provider.CreateServer (Parameters);

			Client = provider.CreateClient (Parameters);

			var supported = Server.SupportedProtocols & Client.SupportedProtocols;
			if (Parameters.ProtocolVersion != null) {
				var requested = Parameters.ProtocolVersion & supported;
				if (requested == ProtocolVersions.Unspecified)
					throw new NotSupportedException ("Incompatible protocol versions between client and server.");

				Parameters.ProtocolVersion = requested;
			}
		}

		protected override async Task Initialize (TestContext ctx, CancellationToken cancellationToken)
		{
			Initialize (ctx);
			ctx.LogDebug (LogCategories.SimpleConnections, 1, $"Starting client and server: {Client} {Server} {Server.EndPoint}");
			InitializeConnection (ctx);
			await StartServer (ctx, cancellationToken);
			await StartClient (ctx, cancellationToken);
		}

		protected override Task PreRun (TestContext ctx, CancellationToken cancellationToken)
		{
			return FinishedTask;
		}

		protected override Task PostRun (TestContext ctx, CancellationToken cancellationToken)
		{
			return FinishedTask;
		}

		protected override Task Destroy (TestContext ctx, CancellationToken cancellationToken)
		{
			return Task.Run (() => Close ());
		}

		[StackTraceEntryPoint]
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

		protected abstract Task StartClient (TestContext ctx, CancellationToken cancellationToken);

		protected abstract Task StartServer (TestContext ctx, CancellationToken cancellationToken);

		protected abstract Task ClientShutdown (TestContext ctx, CancellationToken cancellationToken);

		protected abstract Task ServerShutdown (TestContext ctx, CancellationToken cancellationToken);

		protected virtual Task WaitForServerConnection (TestContext ctx, CancellationToken cancellationToken)
		{
			var task = Server.WaitForConnection (ctx, cancellationToken);
			return task.ContinueWith (t => OnWaitForServerConnectionCompleted (ctx, t));
		}

		protected virtual Task WaitForClientConnection (TestContext ctx, CancellationToken cancellationToken)
		{
			var task = Client.WaitForConnection (ctx, cancellationToken);
			return task.ContinueWith (t => OnWaitForClientConnectionCompleted (ctx, t));
		}

		public async Task WaitForConnection (TestContext ctx, CancellationToken cancellationToken)
		{
			var serverTask = WaitForServerConnection (ctx, cancellationToken);
			var clientTask = WaitForClientConnection (ctx, cancellationToken);

			var t1 = clientTask.ContinueWith (t => {
				if (t.IsFaulted || t.IsCanceled)
					Server.Dispose ();
			});
			var t2 = serverTask.ContinueWith (t => {
				if (t.IsFaulted || t.IsCanceled)
					Client.Dispose ();
			});

			try {
				await Task.WhenAll (serverTask, clientTask, t1, t2);
			} catch (ConnectionFinishedException) {
				throw;
			} catch (Exception ex) {
				Exception error = null;
				TestContext.AddException (ref error, clientTask);
				TestContext.AddException (ref error, serverTask);
				if (error == null)
					error = ex;
				throw error;
			}
		}

		int stopCalled;
		int shutdownCalled;

		protected override void Stop ()
		{
			if (Interlocked.CompareExchange (ref stopCalled, 1, 0) != 0)
				throw new InternalErrorException ();

			Client?.Dispose ();
			Server?.Dispose ();
		}

		public async Task Shutdown (TestContext ctx, CancellationToken cancellationToken)
		{
			if (Interlocked.CompareExchange (ref shutdownCalled, 1, 0) != 0)
				throw new InternalErrorException ();

			var clientShutdown = Client.SupportsCleanShutdown ? ClientShutdown (ctx, cancellationToken) : FinishedTask;
			var serverShutdown = Server.SupportsCleanShutdown ? ServerShutdown (ctx, cancellationToken) : FinishedTask;
			await Task.WhenAll (clientShutdown, serverShutdown);
		}

		protected class ConnectionFinishedException : Exception
		{
		}
	}
}

