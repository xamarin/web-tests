//
// SocketClient.cs
//
// Author:
//       Martin Baulig <mabaul@microsoft.com>
//
// Copyright (c) 2018 Xamarin Inc. (http://www.xamarin.com)
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

namespace Xamarin.AsyncTests.Remoting
{
	public class SocketClient : IDisposable
	{
		Socket socket;
		NetworkStream stream;

		public NetworkStream Stream => stream;

		public SocketClient ()
		{
			socket = new Socket (AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
		}

		public Task ConnectAsync (EndPoint address, CancellationToken cancellationToken)
		{
			var tcs = new TaskCompletionSource<Socket> ();
			if (cancellationToken.IsCancellationRequested) {
				tcs.SetCanceled ();
				return tcs.Task;
			}

			var args = new SocketAsyncEventArgs ();
			args.RemoteEndPoint = address;
			args.Completed += (sender, e) => {
				if (cancellationToken.IsCancellationRequested) {
					tcs.TrySetCanceled ();
				} else if (args.SocketError != SocketError.Success) {
					var error = new IOException ($"AcceptAsync() failed: {args.SocketError})");
					tcs.TrySetException (error);
				} else {
					stream = new NetworkStream (socket);
					tcs.TrySetResult (null);
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

		int disposed;

		protected virtual void Dispose (bool disposing)
		{
			if (Interlocked.CompareExchange (ref disposed, 1, 0) != 0)
				return;

			if (socket != null) {
				try {
					socket.Dispose ();
				} catch {
					;
				}
				socket = null;
			}
		}

		public void Dispose ()
		{
			Dispose (true);
		}
	}
}
