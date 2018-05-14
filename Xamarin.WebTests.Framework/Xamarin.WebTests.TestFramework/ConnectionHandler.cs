﻿//
// ConnectionHandler.cs
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
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Security.Cryptography.X509Certificates;
using Xamarin.AsyncTests;
using Xamarin.AsyncTests.Constraints;
using Xamarin.WebTests.ConnectionFramework;
using Xamarin.WebTests.HttpFramework;

namespace Xamarin.WebTests.TestFramework
{
	public abstract class ConnectionHandler : IDisposable
	{
		public ClientAndServer Runner {
			get;
			private set;
		}

		protected Connection Client {
			get { return Runner.Client; }
		}

		protected Connection Server {
			get { return Runner.Server; }
		}

		public ConnectionHandler (ClientAndServer runner)
		{
			Runner = runner;

			clientReadTcs = new TaskCompletionSource<bool> ();
			clientWriteTcs = new TaskCompletionSource<bool> ();
			serverReadTcs = new TaskCompletionSource<bool> ();
			serverWriteTcs = new TaskCompletionSource<bool> ();
		}

		TaskCompletionSource<bool> clientReadTcs;
		TaskCompletionSource<bool> clientWriteTcs;
		TaskCompletionSource<bool> serverReadTcs;
		TaskCompletionSource<bool> serverWriteTcs;

		public void InitializeConnection (TestContext ctx)
		{
		}

		protected static Task FinishedTask {
			get { return Task.FromResult<object> (null); }
		}

		protected void LogDebug (TestContext ctx, int level, string message, params object[] args)
		{
			var sb = new StringBuilder ();
			sb.AppendFormat ("[{0}]: {1}", GetType ().Name, message);
			if (args.Length > 0)
				sb.Append (" -");
			foreach (var arg in args) {
				sb.Append (" ");
				sb.Append (arg);
			}
			var formatted = sb.ToString ();
			ctx.LogDebug (LogCategories.Listener, level, formatted);
		}

		public Task ExpectBlob (TestContext ctx, Connection connection, CancellationToken cancellationToken)
		{
			return ExpectBlob (ctx, connection, TheQuickBrownFox, TheQuickBrownFoxBuffer, cancellationToken);
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
			return WriteBlob (ctx, connection, TheQuickBrownFox, TheQuickBrownFoxBuffer, cancellationToken);
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
			await clientWriteTcs.Task;
			cancellationToken.ThrowIfCancellationRequested ();

			LogDebug (ctx, 4, "HandleClientWrite");
			await HandleClientWrite (ctx, cancellationToken);
			LogDebug (ctx, 4, "HandleClientWrite - done");
		}

		async Task MyHandleServerRead (TestContext ctx, CancellationToken cancellationToken)
		{
			await serverReadTcs.Task;
			cancellationToken.ThrowIfCancellationRequested ();

			LogDebug (ctx, 4, "HandleServerRead");
			await HandleServerRead (ctx, cancellationToken);
			LogDebug (ctx, 4, "HandleServerRead - done");
		}

		async Task MyHandleServerWrite (TestContext ctx, CancellationToken cancellationToken)
		{
			await serverWriteTcs.Task;
			cancellationToken.ThrowIfCancellationRequested ();

			LogDebug (ctx, 4, "HandleServerWrite");
			await HandleServerWrite (ctx, cancellationToken);
			LogDebug (ctx, 4, "HandleServerWrite - done");
		}

		protected void StartClientRead ()
		{
			clientReadTcs.SetResult (true);
		}

		protected void StartClientWrite ()
		{
			clientWriteTcs.SetResult (true);
		}

		protected void StartServerRead ()
		{
			serverReadTcs.SetResult (true);
		}

		protected void StartServerWrite ()
		{
			serverWriteTcs.SetResult (true);
		}

		protected abstract Task HandleClientRead (TestContext ctx, CancellationToken cancellationToken);

		protected abstract Task HandleClientWrite (TestContext ctx, CancellationToken cancellationToken);

		protected abstract Task HandleServerRead (TestContext ctx, CancellationToken cancellationToken);

		protected abstract Task HandleServerWrite (TestContext ctx, CancellationToken cancellationToken);

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

			await Task.WhenAll (readTask, writeTask, t1, t2);
			cancellationToken.ThrowIfCancellationRequested ();

			LogDebug (ctx, 3, "HandleConnection done", connection);
		}

		public virtual async Task MainLoop (TestContext ctx, CancellationToken cancellationToken)
		{
			cancellationToken.ThrowIfCancellationRequested ();

			Task clientTask;
			if (Runner.IsManualClient)
				clientTask = FinishedTask;
			else if (Runner.IsManualServer)
				clientTask = HandleClientWithManualServer (ctx, cancellationToken);
			else
				clientTask = MyHandleClient (ctx, cancellationToken);

			Task serverTask;
			if (Runner.IsManualServer)
				serverTask = FinishedTask;
			else if (Runner.IsManualClient)
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

		protected abstract Task HandleMainLoop (TestContext ctx, CancellationToken cancellationToken);

		protected abstract Task HandleClient (TestContext ctx, CancellationToken cancellationToken);

		async Task MyHandleClient (TestContext ctx, CancellationToken cancellationToken)
		{
			var readTask = MyHandleClientRead (ctx, cancellationToken);
			var writeTask = MyHandleClientWrite (ctx, cancellationToken);

			await HandleClient (ctx, cancellationToken);
			cancellationToken.ThrowIfCancellationRequested ();

			await HandleConnection (ctx, Client, readTask, writeTask, cancellationToken);
		}

		protected abstract Task HandleServer (TestContext ctx, CancellationToken cancellationToken);

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

		protected virtual async Task HandleClientWithManualServer (TestContext ctx, CancellationToken cancellationToken)
		{
			var clientStream = new StreamWrapper (Client.Stream);

			LogDebug (ctx, 3, "HandleClientWithManualServer");

			await clientStream.WriteLineAsync ("Hello World!");

			var line = await clientStream.ReadLineAsync ();
			LogDebug (ctx, 3, "HandleClientWithManualServer done", line);
		}

		protected virtual async Task HandleServerWithManualClient (TestContext ctx, CancellationToken cancellationToken)
		{
			LogDebug (ctx, 3, "HandleServerWithManualClient");

			var serverStream = new StreamWrapper (Server.Stream);
			await serverStream.WriteLineAsync ("Hello World!");

			LogDebug (ctx, 3, "HandleServerWithManualClient reading");

			var line = await serverStream.ReadLineAsync ();
			LogDebug (ctx, 3, "HandleServerWithManualClient done", line);
		}

		public const string TheQuickBrownFox = "The quick brown fox jumps over the lazy dog";
		public static readonly byte[] TheQuickBrownFoxBuffer = new byte[] {
			0x54, 0x68, 0x65, 0x20, 0x71, 0x75, 0x69, 0x63, 0x6b, 0x20, 0x62, 0x72, 0x6f, 0x77, 0x6e, 0x20,
			0x66, 0x6f, 0x78, 0x20, 0x6a, 0x75, 0x6d, 0x70, 0x73, 0x20, 0x6f, 0x76, 0x65, 0x72, 0x20, 0x74,
			0x68, 0x65, 0x20, 0x6c, 0x61, 0x7a, 0x79, 0x20, 0x64, 0x6f, 0x67
		};

		public static readonly StringContent TheQuickBrownFoxContent = new StringContent (TheQuickBrownFox);

		public static string GetLargeText (int count)
		{
			var sb = new StringBuilder ();
			for (int i = 0; i < count; i++) {
				if (i > 0)
					sb.Append (" ");
				sb.Append ($"The quick brown fox jumps over the lazy dog {count - i} times.");
			}
			return sb.ToString ();
		}

		public static byte[] GetLargeTextBuffer (int count)
		{
			return Encoding.UTF8.GetBytes (GetLargeText (count));
		}

		public static StringContent GetLargeStringContent (int count)
		{
			return new StringContent (GetLargeText (count));
		}

		public static ChunkedContent GetLargeChunkedContent (int count)
		{
			var chunks = new List<string> ();
			for (int i = 0; i < count; i++)
				chunks.Add ($"The quick brown fox jumps over the lazy dog {count - i, 2} times.");
			return new ChunkedContent (chunks);
		}

		public static byte[] GetTextBuffer (string type)
		{
			var text = string.Format ("@{0}:{1}", type, TheQuickBrownFox);
			return Encoding.UTF8.GetBytes (text);
		}

		public void Dispose ()
		{
			Dispose (true);
			GC.SuppressFinalize (this);
		}

		bool disposed;

		protected virtual void DoDispose ()
		{
			clientReadTcs.TrySetCanceled ();
			clientWriteTcs.TrySetCanceled ();
			serverReadTcs.TrySetCanceled ();
			serverWriteTcs.TrySetCanceled ();
		}

		void Dispose (bool disposing)
		{
			lock (this) {
				if (disposed)
					return;
				disposed = true;
				DoDispose ();
			}

			Runner.Dispose ();
		}
	}
}

