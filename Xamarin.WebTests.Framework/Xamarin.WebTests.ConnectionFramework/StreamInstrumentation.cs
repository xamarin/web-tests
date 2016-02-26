//
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

namespace Xamarin.WebTests.ConnectionFramework
{
	public class StreamInstrumentation : NetworkStream, IStreamInstrumentation
	{
		public StreamInstrumentation (Socket socket)
			: base (socket)
		{
		}

		MyAction writeAction;
		MyAction readAction;

		public void OnNextBeginRead (Action action)
		{
			var myAction = new MyAction (action);
			if (Interlocked.CompareExchange (ref readAction, myAction, null) != null)
				throw new InvalidOperationException ();
		}

		public void OnNextBeginWrite (Action action)
		{
			var myAction = new MyAction (action);
			if (Interlocked.CompareExchange (ref writeAction, myAction, null) != null)
				throw new InvalidOperationException ();
		}

		public void OnNextWrite (Func<Task> before, Func<Task> after)
		{
			var myAction = new MyAction (before, after);
			if (Interlocked.CompareExchange (ref writeAction, myAction, null) != null)
				throw new InvalidOperationException ();
		}

		public void OnNextRead (Func<Task> before, Func<Task> after)
		{
			var myAction = new MyAction (before, after);
			if (Interlocked.CompareExchange (ref readAction, myAction, null) != null)
				throw new InvalidOperationException ();
		}

		public override IAsyncResult BeginWrite (byte[] buffer, int offset, int size, AsyncCallback callback, object state)
		{
			var action = Interlocked.Exchange (ref writeAction, null);
			if (action == null)
				return base.BeginWrite (buffer, offset, size, callback, state);

			if (action.Action != null) {
				action.Action ();
				return base.BeginWrite (buffer, offset, size, callback, state);
			}

			var myResult = new MyAsyncResult (action, callback, state);
			var transportResult = base.BeginWrite (buffer, offset, size, WriteCallback, myResult);

			if (transportResult.CompletedSynchronously)
				Task.Factory.StartNew (() => WriteCallback (transportResult));

			return myResult;
		}

		void WriteCallback (IAsyncResult transportResult)
		{
			var myResult = (MyAsyncResult)transportResult.AsyncState;

			try {
				myResult.Action.InvokeBefore ();
				base.EndWrite (transportResult);
				myResult.Action.InvokeAfter ();
				myResult.SetCompleted (false);
			} catch (Exception ex) {
				myResult.SetCompleted (false, ex);
			}
		}

		public override void EndWrite (IAsyncResult asyncResult)
		{
			var myResult = asyncResult as MyAsyncResult;
			if (myResult == null) {
				base.EndWrite (asyncResult);
				return;
			}

			myResult.WaitUntilComplete ();
			if (myResult.GotException)
				throw myResult.Exception;
		}

		public override IAsyncResult BeginRead (byte[] buffer, int offset, int size, AsyncCallback callback, object state)
		{
			var action = Interlocked.Exchange (ref readAction, null);
			if (action == null)
				return base.BeginRead (buffer, offset, size, callback, state);

			if (action.Action != null) {
				action.Action ();
				return base.BeginRead (buffer, offset, size, callback, state);
			}

			var myResult = new MyAsyncResult (action, callback, state);
			var transportResult = base.BeginRead (buffer, offset, size, ReadCallback, myResult);

			if (transportResult.CompletedSynchronously)
				Task.Factory.StartNew (() => ReadCallback (transportResult));

			return myResult;
		}

		void ReadCallback (IAsyncResult transportResult)
		{
			var myResult = (MyAsyncResult)transportResult.AsyncState;

			try {
				myResult.Action.InvokeBefore ();
				myResult.Result = base.EndRead (transportResult);
				myResult.Action.InvokeAfter ();
				myResult.SetCompleted (false);
			} catch (Exception ex) {
				myResult.SetCompleted (false, ex);
			}
		}

		public override int EndRead (IAsyncResult asyncResult)
		{
			var myResult = asyncResult as MyAsyncResult;
			if (myResult == null)
				return base.EndRead (asyncResult);

			myResult.WaitUntilComplete ();
			if (myResult.GotException)
				throw myResult.Exception;

			return myResult.Result;
		}

		class MyAction
		{
			public readonly Action Action;
			public readonly Func<Task> Before;
			public readonly Func<Task> After;

			public MyAction (Action action)
			{
				Action = action;
			}

			public MyAction (Func<Task> before, Func<Task> after)
			{
				Before = before;
				After = after;
			}

			public void InvokeBefore ()
			{
				if (Before != null) {
					var task = Before ();
					if (task != null)
						task.Wait ();
				}
			}

			public void InvokeAfter ()
			{
				if (After != null) {
					var task = After ();
					if (task != null)
						task.Wait ();
				}
			}
		}

		class MyAsyncResult : SimpleAsyncResult
		{
			public readonly MyAction Action;
			public int Result;

			internal MyAsyncResult (MyAction action, AsyncCallback callback, object state)
				: base (callback, state)
			{
				Action = action;
			}
		}
	}
}

