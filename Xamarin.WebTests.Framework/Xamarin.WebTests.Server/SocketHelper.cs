//
// SocketHelper.cs
//
// Author:
//       Martin Baulig <martin.baulig@xamarin.com>
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
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace Xamarin.WebTests.Server
{
	static class SocketHelper
	{
		public static Task<Socket> AcceptAsync (this Socket socket, CancellationToken cancellationToken)
		{
			var tcs = new TaskCompletionSource<Socket> ();
			if (cancellationToken.IsCancellationRequested) {
				tcs.SetCanceled ();
				return tcs.Task;
			}

			var args = new SocketAsyncEventArgs ();
			args.Completed += (sender, e) => {
				if (cancellationToken.IsCancellationRequested) {
					tcs.TrySetCanceled ();
				} else if (args.SocketError != SocketError.Success) {
					var error = new IOException (string.Format ("AcceptAsync() failed: {0}", args.SocketError));
					tcs.TrySetException (error);
				} else {
					tcs.TrySetResult (args.AcceptSocket);
				}
				args.Dispose ();
			};

			try {
				if (!socket.AcceptAsync (args))
					throw new InvalidOperationException ();
			} catch (Exception ex) {
				tcs.TrySetException (ex);
			}

			return tcs.Task;
		}

		public static Task<Socket> ConnectAsync (this Socket socket, EndPoint endPoint, CancellationToken cancellationToken)
		{
			var tcs = new TaskCompletionSource<Socket> ();
			if (cancellationToken.IsCancellationRequested) {
				tcs.SetCanceled ();
				return tcs.Task;
			}

			var args = new SocketAsyncEventArgs ();
			args.RemoteEndPoint = endPoint;
			args.Completed += (sender, e) => {
				if (cancellationToken.IsCancellationRequested) {
					tcs.TrySetCanceled ();
				} else if (args.SocketError != SocketError.Success) {
					var error = new IOException (string.Format ("AcceptAsync() failed: {0}", args.SocketError));
					tcs.TrySetException (error);
				} else {
					tcs.TrySetResult (args.AcceptSocket);
				}
				args.Dispose ();
			};

			try {
				if (!socket.ConnectAsync (args))
					throw new InvalidOperationException ();
			} catch (Exception ex) {
				tcs.TrySetException (ex);
			}

			return tcs.Task;
		}

		public static Task<int> ReceiveAsync (this Socket socket, byte[] buffer, CancellationToken cancellationToken)
		{
			var tcs = new TaskCompletionSource<int> ();

			if (cancellationToken.IsCancellationRequested) {
				tcs.SetCanceled ();
				return tcs.Task;
			}

			var args = new SocketAsyncEventArgs ();
			args.SetBuffer (buffer, 0, buffer.Length);
			args.Completed += (sender, e) => {
				if (cancellationToken.IsCancellationRequested) {
					tcs.TrySetCanceled ();
				} else if (args.SocketError != SocketError.Success) {
					var error = new IOException (string.Format ("ReceiveAsync() failed: {0}", args.SocketError));
					tcs.TrySetException (error);
				} else {
					tcs.TrySetResult (args.BytesTransferred);
				}
				args.Dispose ();
			};

			try {
				if (!socket.ReceiveAsync (args))
					throw new InvalidOperationException ();
			} catch (Exception ex) {
				tcs.TrySetException (ex);
			}

			return tcs.Task;
		}

		public static Task<int> SendAsync (this Socket socket, byte[] buffer, int size, SocketFlags flags, CancellationToken cancellationToken)
		{
			var tcs = new TaskCompletionSource<int> ();

			if (cancellationToken.IsCancellationRequested) {
				tcs.SetCanceled ();
				return tcs.Task;
			}

			var args = new SocketAsyncEventArgs ();
			args.SetBuffer (buffer, 0, size);
			args.SocketFlags = flags;
			args.Completed += (sender, e) => {
				if (cancellationToken.IsCancellationRequested) {
					tcs.TrySetCanceled ();
				} else if (args.SocketError != SocketError.Success) {
					var error = new IOException (string.Format ("SendAsync() failed: {0}", args.SocketError));
					tcs.TrySetException (error);
				} else {
					tcs.TrySetResult (args.BytesTransferred);
				}
				args.Dispose ();
			};

			try {
				if (!socket.SendAsync (args))
					throw new InvalidOperationException ();
			} catch (Exception ex) {
				tcs.TrySetException (ex);
			}

			return tcs.Task;
		}

		public static Task<bool> PollAsync (this Socket socket, CancellationToken cancellationToken)
		{
			return Task.Run (() => {
				while (socket.Connected && !cancellationToken.IsCancellationRequested) {
					if (socket.Poll (100, SelectMode.SelectRead))
						return socket.Connected;
				}
				return false;
			});
		}

	}
}
