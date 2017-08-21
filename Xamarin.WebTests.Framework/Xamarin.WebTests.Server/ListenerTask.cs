//
// ListenerTask.cs
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

namespace Xamarin.WebTests.Server
{
	abstract class ListenerTask
	{
		public abstract ListenerContext Context {
			get;
		}

		public abstract ConnectionState State {
			get;
		}

		public abstract Task Task {
			get;
		}

		public abstract void Start ();

		public abstract ConnectionState Continue ();

		public static ListenerTask Create (ListenerContext context, ConnectionState state,
		                                   Func<Task> start, Func<ConnectionState> continuation)
		{
			Func<Task<object>> func = async () => { await start ().ConfigureAwait (false); return null; };
			return new ListenerTask<object> (context, state, func, r => continuation ());
		}

		public static ListenerTask Create<T> (ListenerContext context, ConnectionState state,
		                                      Func<Task<T>> start, Func<T, ConnectionState> continuation)
		{
			return new ListenerTask<T> (context, state, start, continuation);
		}

		public static ListenerTask Create<T, U> (ListenerContext context, ConnectionState state,
		                                         Func<Task<(T, U)>> start, Func<T, U, ConnectionState> continuation)
		{
			return new ListenerTask<(T, U)> (context, state, start, r => continuation (r.Item1, r.Item2));
		}

		public static ListenerTask Create<T, U, V> (ListenerContext context, ConnectionState state,
		                                            Func<Task<(T, U, V)>> start, Func<T, U, V, ConnectionState> continuation)
		{
			return new ListenerTask<(T, U, V)> (context, state, start, r => continuation (r.Item1, r.Item2, r.Item3));
		}
	}

	sealed class ListenerTask<T> : ListenerTask
	{
		public override ListenerContext Context {
			get;
		}

		public override ConnectionState State {
			get;
		}

		public Func<Task<T>> StartFunc {
			get;
		}

		public override Task Task => tcs.Task;

		public Func<T, ConnectionState> Continuation {
			get;
		}

		public bool Completed {
			get { return completed; }
		}

		TaskCompletionSource<T> tcs;
		volatile bool completed;

		public ListenerTask (ListenerContext context, ConnectionState state, Func<Task<T>> start, Func<T, ConnectionState> continuation)
		{
			Context = context;
			State = state;
			StartFunc = start;
			Continuation = continuation;
			tcs = Listener.TaskSupport.CreateAsyncCompletionSource<T> ();
		}

		public override void Start ()
		{
			Task<T> task;
			try {
				task = StartFunc ();
			} catch (OperationCanceledException) {
				completed = true;
				tcs.TrySetCanceled ();
				return;
			} catch (Exception ex) {
				completed = true;
				tcs.TrySetException (ex);
				return;
			}

			task.ContinueWith (t => {
				completed = true;
				switch (t.Status) {
				case TaskStatus.Canceled:
					tcs.TrySetCanceled ();
					break;
				case TaskStatus.Faulted:
					tcs.TrySetException (t.Exception);
					break;
				case TaskStatus.RanToCompletion:
					tcs.TrySetResult (t.Result);
					break;
				default:
					tcs.TrySetException (new InvalidOperationException ());
					break;
				}
			});
		}

		public override ConnectionState Continue ()
		{
			var result = tcs.Task.Result;
			return Continuation (result);
		}
	}
}
