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
	using TestFramework;

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

		public bool IgnoreErrors {
			get; set;
		}

		public bool RequireAsync {
			get; set;
		}

		public bool AllowBeginEndAsync {
			get; set;
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
		MyAction flushAction;
		MyAction disposeAction;

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

		public void OnNextFlush (AsyncFlushHandler handler)
		{
			var myAction = new MyAction (handler);
			if (Interlocked.CompareExchange (ref flushAction, myAction, null) != null)
				throw new InvalidOperationException ();
		}

		public void OnDispose (DisposeHandler handler)
		{
			var myAction = new MyAction (handler);
			if (Interlocked.CompareExchange (ref disposeAction, myAction, null) != null)
				throw new InvalidOperationException ();
		}

		public delegate Task AsyncWriteFunc (byte[] buffer, int offset, int count, CancellationToken cancellationToken);
		public delegate Task AsyncWriteHandler (byte[] buffer, int offset, int count, AsyncWriteFunc func, CancellationToken cancellationToken);
		delegate void SyncWriteFunc (byte[] buffer, int offset, int count);

		static bool IsTaskAsyncResult (IAsyncResult asyncResult)
		{
			return asyncResult is TaskToApm.TaskWrapperAsyncResult || asyncResult is Task;
		}

		void LogDebug (string message)
		{
			Context.LogDebug (LogCategories.StreamInstrumentation, 4, message);
		}

		Task BaseWriteAsync (byte[] buffer, int offset, int count, CancellationToken cancellationToken)
		{
			return Task.Factory.FromAsync (
				(ca, st) => base.BeginWrite (buffer, offset, count, ca, st),
				(result) => base.EndWrite (result), null);
		}

		public override Task WriteAsync (byte[] buffer, int offset, int count, CancellationToken cancellationToken)
		{
			var message = string.Format ("{0}.WriteAsync({1},{2})", Name, offset, count);

			AsyncWriteFunc asyncBaseWrite = BaseWriteAsync;
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
			LogDebug (message);
			try {
				await handler (buffer, offset, count, func, cancellationToken).ConfigureAwait (false);
				LogDebug ($"{message} done");
			} catch (Exception ex) {
				if (IgnoreErrors)
					return;
				LogDebug ($"{message} failed: {ex}");
				throw;
			}
		}

		public override IAsyncResult BeginWrite (byte[] buffer, int offset, int size, AsyncCallback callback, object state)
		{
			var message = $"{Name}.BeginWrite({offset},{size})";
			LogDebug (message);

			if (RequireAsync && !AllowBeginEndAsync)
				throw Context.AssertFail ($"{message}: async API required.");

			AsyncWriteFunc asyncBaseWrite = (b, o, s, _) => Task.Factory.FromAsync (
				(ca, st) => base.BeginWrite (b, o, s, ca, st),
				(result) => base.EndWrite (result), null);

			var action = Interlocked.Exchange (ref writeAction, null);
			if (action?.AsyncWrite == null)
				return base.BeginWrite (buffer, offset, size, callback, state);

			message += " - action";

			AsyncWriteFunc writeFunc = (b, o, s, ct) => action.AsyncWrite (b, o, s, asyncBaseWrite, ct);
			try {
				var writeTask = writeFunc (buffer, offset, size, CancellationToken.None);
				LogDebug ($"{message} got task: {writeTask.Status}");
				return TaskToApm.Begin (writeTask, callback, state);
			} catch (Exception ex) {
				LogDebug ($"{message} failed: {ex}");
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
			var message = $"{Name}.Write({offset},{size})";

			if (RequireAsync)
				throw Context.AssertFail ($"{message}: async API required.");

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
			LogDebug (message);
			try {
				func (buffer, offset, size);
				LogDebug ($"{message} done");
			} catch (Exception ex) {
				if (IgnoreErrors)
					return;
				LogDebug ($"{message} failed: {ex}");
				throw;
			}
		}

		public delegate Task<int> AsyncReadFunc (byte[] buffer, int offset, int count, CancellationToken cancellationToken);
		public delegate Task<int> AsyncReadHandler (byte[] buffer, int offset, int count, AsyncReadFunc func, CancellationToken cancellationToken);
		delegate int SyncReadFunc (byte[] buffer, int offset, int count);

		Task<int> BaseReadAsync (byte[] buffer, int offset, int count, CancellationToken cancellationToken)
		{
			return Task.Factory.FromAsync (
				(ca, st) => base.BeginRead (buffer, offset, count, ca, st),
				(result) => base.EndRead (result), null);
		}

		public override Task<int> ReadAsync (byte[] buffer, int offset, int count, CancellationToken cancellationToken)
		{
			var message = $"{Name}.ReadAsync({offset},{count})";

			AsyncReadFunc asyncBaseRead = BaseReadAsync;
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
			LogDebug (message);
			try {
				var ret = await handler (buffer, offset, count, func, cancellationToken).ConfigureAwait (false);
				LogDebug ($"{message} done: {ret}");
				return ret;
			} catch (Exception ex) {
				if (IgnoreErrors)
					return -1;
				LogDebug ($"{message} failed: {ex}");
				throw;
			}
		}

		public override IAsyncResult BeginRead (byte[] buffer, int offset, int size, AsyncCallback callback, object state)
		{
			var message = $"{Name}.BeginRead({offset},{size})";
			LogDebug (message);

			if (RequireAsync && !AllowBeginEndAsync)
				throw Context.AssertFail ($"{message}: async API required.");

			AsyncReadFunc asyncBaseRead = (b, o, s, _) => Task.Factory.FromAsync (
				(ca, st) => base.BeginRead (b, o, s, ca, st),
				(result) => base.EndRead (result), null);

			var action = Interlocked.Exchange (ref readAction, null);
			if (action?.AsyncRead == null)
				return base.BeginRead (buffer, offset, size, callback, state);

			message += " - action";

			AsyncReadFunc readFunc = (b, o, s, ct) => action.AsyncRead (b, o, s, asyncBaseRead, ct);
			try {
				var readTask = readFunc (buffer, offset, size, CancellationToken.None);
				LogDebug ($"{message} got task: {readTask.Status}");
				return TaskToApm.Begin (readTask, callback, state);
			} catch (Exception ex) {
				LogDebug ($"{message} failed: {ex}");
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
			var message = $"{Name}.Read({offset},{size})";

			if (RequireAsync)
				throw Context.AssertFail ($"{message}: async API required.");

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
			LogDebug (message);
			try {
				int ret = func (buffer, offset, size);
				LogDebug ($"{message} done: {ret}");
				return ret;
			} catch (Exception ex) {
				if (IgnoreErrors)
					return -1;
				LogDebug ($"{message} failed: {ex}");
				throw;
			}
		}

		public delegate Task AsyncFlushHandler (AsyncFlushFunc func, CancellationToken cancellationToken);
		public delegate Task AsyncFlushFunc (CancellationToken cancellationToken);
		delegate void SyncFlushFunc ();

		public override void Flush ()
		{
			var message = $"{Name}.Flush()";

			if (RequireAsync)
				throw Context.AssertFail ($"{message}: async API required.");

			SyncFlushFunc syncFlush = () => base.Flush ();
			SyncFlushFunc originalSyncFlush = syncFlush;

			var action = Interlocked.Exchange (ref flushAction, null);
			if (action?.AsyncFlush != null) {
				message += " - action";

				AsyncFlushFunc asyncBaseFlush = (_) => Task.Factory.FromAsync (
					(callback, state) => originalSyncFlush.BeginInvoke (callback, state),
					(result) => originalSyncFlush.EndInvoke (result), null);

				syncFlush = () => action.AsyncFlush (asyncBaseFlush, CancellationToken.None).Wait ();
			}

			Flush_internal (message, syncFlush);
		}

		void Flush_internal (string message, SyncFlushFunc func)
		{
			LogDebug (message);
			try {
				func ();
				LogDebug ($"{message} done");
			} catch (Exception ex) {
				if (IgnoreErrors)
					return;
				LogDebug ($"{message} failed: {ex}");
				throw;
			}
		}

		public override Task FlushAsync (CancellationToken cancellationToken)
		{
			var message = $"{Name}.FlushAsync()";

			AsyncFlushFunc asyncBaseFlush = base.FlushAsync;
			AsyncFlushHandler asyncFlushHandler = (func, ct) => func (ct);

			var action = Interlocked.Exchange (ref flushAction, null);
			if (action?.AsyncFlush != null) {
				message += " - action";
				return FlushAsync (message, asyncBaseFlush, action.AsyncFlush, cancellationToken);
			}

			return FlushAsync (message, asyncBaseFlush, asyncFlushHandler, cancellationToken);
		}

		async Task FlushAsync (string message, AsyncFlushFunc func, AsyncFlushHandler handler, CancellationToken cancellationToken)
		{
			LogDebug (message);
			try {
				await handler (func, cancellationToken).ConfigureAwait (false);
				LogDebug ($"{message} done");
			} catch (Exception ex) {
				if (IgnoreErrors)
					return;
				LogDebug ($"{message} failed: {ex}");
				throw;
			}
		}

		public delegate void DisposeHandler (DisposeFunc func);
		public delegate void DisposeFunc ();

		protected override void Dispose (bool disposing)
		{
			if (!disposing) {
				base.Dispose (disposing);
				return;
			}

			var message = $"{Name}.Dispose()";

			DisposeFunc baseDispose = () => base.Dispose (true);
			DisposeFunc originalDispose = baseDispose;

			var action = Interlocked.Exchange (ref disposeAction, null);
			if (action?.Dispose != null) {
				message += " - action";
				baseDispose = () => action.Dispose (originalDispose);
			}

			Dispose_internal (message, baseDispose);
		}

		void Dispose_internal (string message, DisposeFunc func)
		{
			LogDebug (message);
			try {
				func ();
				LogDebug ($"{message} done");
			} catch (Exception ex) {
				if (IgnoreErrors)
					return;
				LogDebug ($"{message} failed: {ex}");
				throw;
			}
		}

		class MyAction
		{
			public readonly AsyncReadHandler AsyncRead;
			public readonly AsyncWriteHandler AsyncWrite;
			public readonly AsyncFlushHandler AsyncFlush;
			public readonly DisposeHandler Dispose;

			public MyAction (AsyncReadHandler handler)
			{
				AsyncRead = handler;
			}

			public MyAction (AsyncWriteHandler handler)
			{
				AsyncWrite = handler;
			}

			public MyAction (AsyncFlushHandler handler)
			{
				AsyncFlush = handler;
			}

			public MyAction (DisposeHandler dispose)
			{
				Dispose = dispose;
			}
		}
	}
}
