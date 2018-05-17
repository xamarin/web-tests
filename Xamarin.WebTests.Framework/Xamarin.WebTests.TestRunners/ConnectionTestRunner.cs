//
// ConnectionTestRunner.cs
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
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using Xamarin.AsyncTests;
using Xamarin.AsyncTests.Constraints;

namespace Xamarin.WebTests.TestRunners
{
	using ConnectionFramework;
	using TestFramework;
	using HttpFramework;

	public abstract class ConnectionTestRunner : ClientAndServer
	{
		protected abstract string LogCategory {
			get;
		}

		public string ME {
			get;
		}

		protected string TestCompletedString {
			get;
		}

		protected byte[] TestCompletedBlob {
			get;
		}

		protected ConnectionTestRunner ()
		{
			ME = $"[{GetType ().Name}]";
			TestCompletedString = $"TestCompleted({ME})";
			TestCompletedBlob = ConnectionHandler.GetTextBuffer (TestCompletedString);
			clientReadTcs = new TaskCompletionSource<bool> ();
			clientWriteTcs = new TaskCompletionSource<bool> ();
			serverReadTcs = new TaskCompletionSource<bool> ();
			serverWriteTcs = new TaskCompletionSource<bool> ();
		}

		protected void LogDebug (TestContext ctx, int level, string message, params object[] args)
		{
			var sb = new StringBuilder ();
			sb.AppendFormat ($"[{GetType ().Name}]: {message}");
			if (args.Length > 0)
				sb.Append (" -");
			foreach (var arg in args) {
				sb.Append (" ");
				sb.Append (arg);
			}
			var formatted = sb.ToString ();
			ctx.LogDebug (LogCategory, level, formatted);
		}

		#region Start and Stop

		protected override Task StartClient (TestContext ctx, CancellationToken cancellationToken)
		{
			return Client.Start (ctx, null, cancellationToken);
		}

		protected override Task StartServer (TestContext ctx, CancellationToken cancellationToken)
		{
			return Server.Start (ctx, null, cancellationToken);
		}

		protected override Task ClientShutdown (TestContext ctx, CancellationToken cancellationToken)
		{
			return Client.Shutdown (ctx, cancellationToken);
		}

		protected override Task ServerShutdown (TestContext ctx, CancellationToken cancellationToken)
		{
			return Server.Shutdown (ctx, cancellationToken);
		}

		#endregion

		#region Connection Handler

		readonly TaskCompletionSource<bool> clientReadTcs;
		readonly TaskCompletionSource<bool> clientWriteTcs;
		readonly TaskCompletionSource<bool> serverReadTcs;
		readonly TaskCompletionSource<bool> serverWriteTcs;

		public Task ExpectBlob (TestContext ctx, Connection connection, CancellationToken cancellationToken)
		{
			return ExpectBlob (ctx, connection, ConnectionHandler.TheQuickBrownFox, ConnectionHandler.TheQuickBrownFoxBuffer, cancellationToken);
		}

		public async Task ExpectBlob (TestContext ctx, Connection connection, string type, byte[] blob, CancellationToken cancellationToken)
		{
			cancellationToken.ThrowIfCancellationRequested ();

			LogDebug (ctx, 5, "ExpectBlob", connection, type);

			var buffer = new byte [4096];
			var ret = await connection.Stream.ReadAsync (buffer, 0, buffer.Length, cancellationToken);

			LogDebug (ctx, 5, "ExpectBlob #1", connection, type, ret);

			if (ctx.Expect (ret, Is.GreaterThan (0), "read success")) {
				var result = new byte [ret];
				Buffer.BlockCopy (buffer, 0, result, 0, ret);

				ctx.Expect (result, new IsEqualBlob (blob), "blob");
			}

			LogDebug (ctx, 5, "ExpectBlob done", connection, type);
		}

		public Task WriteBlob (TestContext ctx, Connection connection, CancellationToken cancellationToken)
		{
			return WriteBlob (ctx, connection, ConnectionHandler.TheQuickBrownFox, ConnectionHandler.TheQuickBrownFoxBuffer, cancellationToken);
		}

		public async Task WriteBlob (TestContext ctx, Connection connection, string type, byte[] blob, CancellationToken cancellationToken)
		{
			cancellationToken.ThrowIfCancellationRequested ();

			LogDebug (ctx, 5, "WriteBlob", connection, type);

			await connection.Stream.WriteAsync (blob, 0, blob.Length, cancellationToken);

			LogDebug (ctx, 5, "WriteBlob done", connection, type);
		}

		async Task MyHandleClientRead (TestContext ctx, CancellationToken cancellationToken)
		{
			await clientReadTcs.Task;
			cancellationToken.ThrowIfCancellationRequested ();

			LogDebug (ctx, 4, "HandleClientRead");
			await HandleClientRead (ctx, cancellationToken);
			LogDebug (ctx, 4, "HandleClientRead - done");
		}

		async Task MyHandleClientWrite (TestContext ctx, CancellationToken cancellationToken)
		{
			if (!await clientWriteTcs.Task.ConfigureAwait (false))
				return;

			cancellationToken.ThrowIfCancellationRequested ();

			LogDebug (ctx, 4, "HandleClientWrite");
			await HandleClientWrite (ctx, cancellationToken);
			LogDebug (ctx, 4, "HandleClientWrite - done");
		}

		async Task MyHandleServerRead (TestContext ctx, CancellationToken cancellationToken)
		{
			if (!await serverReadTcs.Task.ConfigureAwait (false))
				return;

			cancellationToken.ThrowIfCancellationRequested ();

			LogDebug (ctx, 4, "HandleServerRead");
			await HandleServerRead (ctx, cancellationToken);
			LogDebug (ctx, 4, "HandleServerRead - done");
		}

		async Task MyHandleServerWrite (TestContext ctx, CancellationToken cancellationToken)
		{
			if (!await serverWriteTcs.Task.ConfigureAwait (false))
				return;

			cancellationToken.ThrowIfCancellationRequested ();

			LogDebug (ctx, 4, "HandleServerWrite");
			await HandleServerWrite (ctx, cancellationToken);
			LogDebug (ctx, 4, "HandleServerWrite - done");
		}

		protected void StartClientRead ()
		{
			clientReadTcs.TrySetResult (true);
		}

		protected void StartClientWrite (bool success)
		{
			clientWriteTcs.TrySetResult (success);
		}

		protected void StartServerRead (bool success)
		{
			serverReadTcs.TrySetResult (success);
		}

		protected void StartServerWrite (bool success)
		{
			serverWriteTcs.TrySetResult (success);
		}

		protected void CancelClient ()
		{
			clientReadTcs.TrySetCanceled ();
			clientWriteTcs.TrySetCanceled ();
		}

		protected void CancelServer ()
		{
			serverReadTcs.TrySetCanceled ();
			serverWriteTcs.TrySetCanceled ();
		}

		protected virtual async Task HandleClientRead (TestContext ctx, CancellationToken cancellationToken)
		{
			await ExpectBlob (ctx, Client, TestCompletedString, TestCompletedBlob, cancellationToken).ConfigureAwait (false);
			StartClientWrite (true);
		}

		protected virtual async Task HandleClientWrite (TestContext ctx, CancellationToken cancellationToken)
		{
			await WriteBlob (ctx, Client, TestCompletedString, TestCompletedBlob, cancellationToken);
		}

		protected virtual async Task HandleServerRead (TestContext ctx, CancellationToken cancellationToken)
		{
			await ExpectBlob (ctx, Server, TestCompletedString, TestCompletedBlob, cancellationToken);
		}

		protected virtual async Task HandleServerWrite (TestContext ctx, CancellationToken cancellationToken)
		{
			await WriteBlob (ctx, Server, TestCompletedString, TestCompletedBlob, cancellationToken);
		}

		protected virtual async Task HandleMainLoop (TestContext ctx, CancellationToken cancellationToken)
		{
			await Client.Stream.FlushAsync ().ConfigureAwait (false);
			await Server.Stream.FlushAsync ();
		}

		protected virtual Task HandleClient (TestContext ctx, CancellationToken cancellationToken)
		{
			StartClientRead ();
			return FinishedTask;
		}

		protected virtual Task HandleServer (TestContext ctx, CancellationToken cancellationToken)
		{
			StartServerWrite (true);
			StartServerRead (true);
			return FinishedTask;
		}

		async Task HandleConnection (TestContext ctx, Connection connection, Task readTask, Task writeTask, CancellationToken cancellationToken)
		{
			var t1 = readTask.ContinueWith (t => {
				LogDebug (ctx, 3, "HandleConnection - read done", connection, t.Status, t.IsFaulted, t.IsCanceled);
				if (t.IsFaulted || t.IsCanceled)
					Dispose ();
			});
			var t2 = writeTask.ContinueWith (t => {
				LogDebug (ctx, 3, "HandleConnection - write done", connection, t.Status, t.IsFaulted, t.IsCanceled);
				if (t.IsFaulted || t.IsCanceled)
					Dispose ();
			});

			LogDebug (ctx, 3, "HandleConnection", connection);

			try {
				await Task.WhenAll (readTask, writeTask, t1, t2).ConfigureAwait (false);
				cancellationToken.ThrowIfCancellationRequested ();
			} finally {
				LogDebug (ctx, 3, "HandleConnection done", connection);
			}
		}

		protected override async Task MainLoop (TestContext ctx, CancellationToken cancellationToken)
		{
			cancellationToken.ThrowIfCancellationRequested ();

			Task clientTask;
			if (IsManualClient)
				clientTask = FinishedTask;
			else if (IsManualServer)
				clientTask = HandleClientWithManualServer (ctx, cancellationToken);
			else
				clientTask = MyHandleClient (ctx, cancellationToken);

			Task serverTask;
			if (IsManualServer)
				serverTask = FinishedTask;
			else if (IsManualClient)
				serverTask = HandleServerWithManualClient (ctx, cancellationToken);
			else
				serverTask = MyHandleServer (ctx, cancellationToken);

			var t1 = clientTask.ContinueWith (t => {
				LogDebug (ctx, 3, "Client done", t.Status, t.IsFaulted, t.IsCanceled);
				if (t.IsFaulted || t.IsCanceled)
					Dispose ();
			});
			var t2 = serverTask.ContinueWith (t => {
				LogDebug (ctx, 3, "Server done", t.Status, t.IsFaulted, t.IsCanceled);
				if (t.IsFaulted || t.IsCanceled)
					Dispose ();
			});

			try {
				var mainLoopTask = Task.WhenAll (clientTask, serverTask, t1, t2);
				await HandleMainLoop (ctx, cancellationToken);
				cancellationToken.ThrowIfCancellationRequested ();

				LogDebug (ctx, 3, "MainLoop");
				await mainLoopTask;
				cancellationToken.ThrowIfCancellationRequested ();
			} finally {
				LogDebug (ctx, 3, "MainLoop done");
			}
		}

		async Task MyHandleClient (TestContext ctx, CancellationToken cancellationToken)
		{
			var readTask = MyHandleClientRead (ctx, cancellationToken);
			var writeTask = MyHandleClientWrite (ctx, cancellationToken);

			await HandleClient (ctx, cancellationToken);
			cancellationToken.ThrowIfCancellationRequested ();

			await HandleConnection (ctx, Client, readTask, writeTask, cancellationToken);
		}

		async Task MyHandleServer (TestContext ctx, CancellationToken cancellationToken)
		{
			var readTask = MyHandleServerRead (ctx, cancellationToken);
			var writeTask = MyHandleServerWrite (ctx, cancellationToken);

			LogDebug (ctx, 3, "HandleServer");

			await HandleServer (ctx, cancellationToken);
			cancellationToken.ThrowIfCancellationRequested ();

			LogDebug (ctx, 3, "HandleServer #1");

			await HandleConnection (ctx, Server, readTask, writeTask, cancellationToken);

			LogDebug (ctx, 3, "HandleServer done");
		}

		async Task HandleClientWithManualServer (TestContext ctx, CancellationToken cancellationToken)
		{
			var clientStream = new StreamWrapper (Client.Stream);

			LogDebug (ctx, 3, "HandleClientWithManualServer");

			await clientStream.WriteLineAsync ("Hello World!");

			var line = await clientStream.ReadLineAsync ();
			LogDebug (ctx, 3, "HandleClientWithManualServer done", line);
		}

		async Task HandleServerWithManualClient (TestContext ctx, CancellationToken cancellationToken)
		{
			LogDebug (ctx, 3, "HandleServerWithManualClient");

			var serverStream = new StreamWrapper (Server.Stream);
			await serverStream.WriteLineAsync ("Hello World!");

			LogDebug (ctx, 3, "HandleServerWithManualClient reading");

			var line = await serverStream.ReadLineAsync ();
			LogDebug (ctx, 3, "HandleServerWithManualClient done", line);
		}

		protected override void Stop ()
		{
			clientReadTcs.TrySetCanceled ();
			clientWriteTcs.TrySetCanceled ();
			serverReadTcs.TrySetCanceled ();
			serverWriteTcs.TrySetCanceled ();
			base.Stop ();
		}

		#endregion
	}
}

