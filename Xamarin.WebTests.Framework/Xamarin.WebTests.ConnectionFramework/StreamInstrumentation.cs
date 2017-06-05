﻿//
// StreamInstrumentation.cs
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
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Xamarin.AsyncTests;

namespace Xamarin.WebTests.ConnectionFramework
{
	public class StreamInstrumentation : NetworkStream
	{
		public TestContext Context {
			get;
		}

		public string Name {
			get;
		}

		new public Socket Socket {
			get;
		}

		public StreamInstrumentation (TestContext ctx, string name, Socket socket, bool ownsSocket = true)
			: base (socket, ownsSocket)
		{
			Context = ctx;
			Socket = socket;
			Name = string.Format ("StreamInstrumentation({0})", name);
		}

		MyAction writeAction;
		MyAction readAction;

		public void OnNextRead (AsyncReadHandler handler)
		{
			var myAction = new MyAction (handler);
			if (Interlocked.CompareExchange (ref readAction, myAction, null) != null)
				throw new InvalidOperationException ();
		}

		public void OnNextWrite (AsyncWriteHandler handler)
		{
			var myAction = new MyAction (handler);
			if (Interlocked.CompareExchange (ref writeAction, myAction, null) != null)
				throw new InvalidOperationException ();
		}

		public delegate Task AsyncWriteFunc (byte[] buffer, int offset, int count, CancellationToken cancellationToken);
		public delegate Task AsyncWriteHandler (byte[] buffer, int offset, int count, AsyncWriteFunc func, CancellationToken cancellationToken);
		delegate void SyncWriteFunc (byte[] buffer, int offset, int count);

		static bool IsTaskAsyncResult (IAsyncResult asyncResult)
		{
			return asyncResult is TaskToApm.TaskWrapperAsyncResult || asyncResult is Task;
		}

		public override Task WriteAsync (byte[] buffer, int offset, int count, CancellationToken cancellationToken)
		{
			var message = string.Format ("{0}.WriteAsync({1},{2})", Name, offset, count);

			AsyncWriteFunc asyncBaseWrite = base.WriteAsync;
			AsyncWriteHandler asyncWriteHandler = (b, o, c, func, ct) => func (b, o, c, ct);

			var action = Interlocked.Exchange (ref writeAction, null);
			if (action?.AsyncWrite != null) {
				message += " - action";
				return WriteAsync (buffer, offset, count, message, asyncBaseWrite, action.AsyncWrite, cancellationToken);
			}

			return WriteAsync (buffer, offset, count, message, asyncBaseWrite, asyncWriteHandler, cancellationToken);
		}

		async Task WriteAsync (byte[] buffer, int offset, int count, string message,
		                       AsyncWriteFunc func, AsyncWriteHandler handler, CancellationToken cancellationToken)
		{
			Context.LogDebug (4, message);
			try {
				await handler (buffer, offset, count, func, cancellationToken).ConfigureAwait (false);
				Context.LogDebug (4, "{0} done", message);
			} catch (Exception ex) {
				Context.LogDebug (4, "{0} failed: {1}", message, ex);
				throw;
			}
		}

		public override IAsyncResult BeginWrite (byte[] buffer, int offset, int size, AsyncCallback callback, object state)
		{
			var message = string.Format ("{0}.BeginWrite({1},{2})", Name, offset, size);
			Context.LogDebug (4, message);

			AsyncWriteFunc asyncBaseWrite = (b, o, s, _) => Task.Factory.FromAsync (
				(ca, st) => base.BeginWrite (b, o, s, ca, st),
				(result) => base.EndWrite (result), null);

			var action = Interlocked.Exchange (ref writeAction, null);
			if (action?.AsyncWrite == null)
				return base.BeginWrite (buffer, offset, size, callback, state);

			message += " - action";

			AsyncWriteFunc writeFunc = (b, o, s, ct) => action.AsyncWrite (b, o, s, asyncBaseWrite, ct);
			try {
				Context.LogDebug (4, message);
				var writeTask = writeFunc (buffer, offset, size, CancellationToken.None);
				Context.LogDebug (4, "{0} got task: {1}", message, writeTask.Status);
				return TaskToApm.Begin (writeTask, callback, state);
			} catch (Exception ex) {
				Context.LogDebug (4, "{0} failed: {1}", message, ex);
				throw;
			}
		}

		public override void EndWrite (IAsyncResult asyncResult)
		{
			if (!IsTaskAsyncResult (asyncResult)) {
				base.EndRead (asyncResult);
				return;
			}

			TaskToApm.End (asyncResult);
		}

		public override void Write (byte[] buffer, int offset, int size)
		{
			var message = string.Format ("{0}.Write({1},{2})", Name, offset, size);

			SyncWriteFunc syncWrite = (b, o, s) => base.Write (b, o, s);
			SyncWriteFunc originalSyncWrite = syncWrite;

			var action = Interlocked.Exchange (ref writeAction, null);
			if (action?.AsyncWrite != null) {
				message += " - action";

				AsyncWriteFunc asyncBaseWrite = (b, o, s, _) => Task.Factory.FromAsync (
					(callback, state) => originalSyncWrite.BeginInvoke (b, o, s, callback, state),
					(result) => originalSyncWrite.EndInvoke (result), null);

				syncWrite = (b, o, s) => action.AsyncWrite (b, o, s, asyncBaseWrite, CancellationToken.None).Wait ();
			}

			Write_internal (buffer, offset, size, message, syncWrite);
		}

		void Write_internal (byte[] buffer, int offset, int size, string message, SyncWriteFunc func)
		{
			Context.LogDebug (4, message);
			try {
				func (buffer, offset, size);
				Context.LogDebug (4, "{0} done", message);
			} catch (Exception ex) {
				Context.LogDebug (4, "{0} failed: {1}", message, ex);
				throw;
			}
		}

		public delegate Task<int> AsyncReadFunc (byte[] buffer, int offset, int count, CancellationToken cancellationToken);
		public delegate Task<int> AsyncReadHandler (byte[] buffer, int offset, int count, AsyncReadFunc func, CancellationToken cancellationToken);
		delegate int SyncReadFunc (byte[] buffer, int offset, int count);

		public override Task<int> ReadAsync (byte[] buffer, int offset, int count, CancellationToken cancellationToken)
		{
			var message = string.Format ("{0}.ReadAsync({1},{2})", Name, offset, count);

			AsyncReadFunc asyncBaseRead = base.ReadAsync;
			AsyncReadHandler asyncReadHandler = (b, o, c, func, ct) => func (b, o, c, ct);

			var action = Interlocked.Exchange (ref readAction, null);
			if (action?.AsyncRead != null) {
				message += " - action";
				return ReadAsync (buffer, offset, count, message, asyncBaseRead, action.AsyncRead, cancellationToken);
			}

			return ReadAsync (buffer, offset, count, message, asyncBaseRead, asyncReadHandler, cancellationToken);
		}

		async Task<int> ReadAsync (byte[] buffer, int offset, int count, string message,
		                           AsyncReadFunc func, AsyncReadHandler handler, CancellationToken cancellationToken)
		{
			Context.LogDebug (4, message);
			try {
				var ret = await handler (buffer, offset, count, func, cancellationToken).ConfigureAwait (false);
				Context.LogDebug (4, "{0} done: {1}", message, ret);
				return ret;
			} catch (Exception ex) {
				Context.LogDebug (4, "{0} failed: {1}", message, ex);
				throw;
			}
		}

		public override IAsyncResult BeginRead (byte[] buffer, int offset, int size, AsyncCallback callback, object state)
		{
			var message = string.Format ("{0}.BeginRead({1},{2})", Name, offset, size);
			Context.LogDebug (4, message);

			AsyncReadFunc asyncBaseRead = (b, o, s, _) => Task.Factory.FromAsync (
				(ca, st) => base.BeginRead (b, o, s, ca, st),
				(result) => base.EndRead (result), null);

			var action = Interlocked.Exchange (ref readAction, null);
			if (action?.AsyncRead == null)
				return base.BeginRead (buffer, offset, size, callback, state);

			message += " - action";

			AsyncReadFunc readFunc = (b, o, s, ct) => action.AsyncRead (b, o, s, asyncBaseRead, ct);
			try {
				Context.LogDebug (4, message);
				var readTask = readFunc (buffer, offset, size, CancellationToken.None);
				Context.LogDebug (4, "{0} got task: {1}", message, readTask.Status);
				return TaskToApm.Begin (readTask, callback, state);
			} catch (Exception ex) {
				Context.LogDebug (4, "{0} failed: {1}", message, ex);
				throw;
			}
		}

		public override int EndRead (IAsyncResult asyncResult)
		{
			if (!IsTaskAsyncResult (asyncResult))
				return base.EndRead (asyncResult);

			return TaskToApm.End<int> (asyncResult);
		}

		public override int Read (byte[] buffer, int offset, int size)
		{
			var message = string.Format ("{0}.Read({1},{2})", Name, offset, size);

			SyncReadFunc syncRead = (b, o, s) => base.Read (b, o, s);
			SyncReadFunc originalSyncRead = syncRead;

			var action = Interlocked.Exchange (ref readAction, null);
			if (action?.AsyncRead != null) {
				message += " - action";

				AsyncReadFunc asyncBaseRead = (b, o, s, _) => Task.Factory.FromAsync (
					(callback, state) => originalSyncRead.BeginInvoke (b, o, s, callback, state),
					(result) => originalSyncRead.EndInvoke (result), null);

				syncRead = (b, o, s) => action.AsyncRead (b, o, s, asyncBaseRead, CancellationToken.None).Result;
			}

			return Read_internal (buffer, offset, size, message, syncRead);
		}

		int Read_internal (byte[] buffer, int offset, int size, string message, SyncReadFunc func)
		{
			Context.LogDebug (4, message);
			try {
				int ret = func (buffer, offset, size);
				Context.LogDebug (4, "{0} done: {1}", message, ret);
				return ret;
			} catch (Exception ex) {
				Context.LogDebug (4, "{0} failed: {1}", message, ex);
				throw;
			}
		}

		class MyAction
		{
			public readonly AsyncReadHandler AsyncRead;
			public readonly AsyncWriteHandler AsyncWrite;

			public MyAction (AsyncReadHandler handler)
			{
				AsyncRead = handler;
			}

			public MyAction (AsyncWriteHandler handler)
			{
				AsyncWrite = handler;
			}
		}
	}
}

