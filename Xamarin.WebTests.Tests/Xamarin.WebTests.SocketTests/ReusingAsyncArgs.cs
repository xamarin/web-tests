//
// ReusingAsyncArgs.cs
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
using Xamarin.AsyncTests;

namespace Xamarin.WebTests.SocketTests
{
	using ConnectionFramework;
	using TestAttributes;

	[NotWorking]
	public class ReusingAsyncArgs : SocketTestFixture
	{
		protected override void CreateParameters (TestContext ctx, ConnectionParameters parameters)
		{
			var port = TestContext.GetUniquePort ();
			parameters.ListenAddress = new IPEndPoint (IPAddress.Loopback, port);
			parameters.EndPoint = new DnsEndPoint ("localhost", port);
			base.CreateParameters (ctx, parameters);
		}

		protected override Task<Socket> StartServer (TestContext ctx, EndPoint endPoint, CancellationToken cancellationToken)
		{
			var socket = ctx.RegisterDispose (new Socket (AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp));
			socket.Bind (endPoint);
			socket.Listen (2);

			var me = $"{ME}.{nameof (StartServer)}";
			ctx.LogMessage (me);

			var tcs = new TaskCompletionSource<Socket> ();

			Task.Run (() => {
				try {
					var accepted = socket.Accept ();
					ctx.LogDebug (LogCategory, 2, $"{me} accepted: {accepted.RemoteEndPoint}.");
					accepted.Dispose ();

					var accepted2 = socket.Accept ();
					ctx.LogDebug (LogCategory, 2, $"{me} accepted again: {accepted2.RemoteEndPoint}.");

					ctx.RegisterDispose (accepted2);
					tcs.TrySetResult (accepted2);
				} catch (Exception ex) {
					tcs.TrySetException (ex);
				}
			});

			return tcs.Task;
		}

		protected override Task<Socket> StartClient (TestContext ctx, EndPoint endPoint, CancellationToken cancellationToken)
		{
			var socket = ctx.RegisterDispose (new Socket (AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp));

			var me = $"{ME}.{nameof (StartClient)}";
			ctx.LogMessage (me);

			var tcs = new TaskCompletionSource<Socket> ();

			SocketAsyncEventArgs e = new SocketAsyncEventArgs {
				RemoteEndPoint = endPoint
			};

			bool secondConnect = false;

			e.Completed += (sender, _) => {
				ctx.LogDebug (LogCategory, 2, $"{me} connected: {secondConnect} {tcs.Task.Status} {e.SocketError}");

				if (secondConnect) {
					if (e.SocketError != SocketError.Success)
						tcs.SetException (new IOException ($"ConnectAsync() failed: {e.SocketError}"));
					else
						tcs.SetResult (e.ConnectSocket);
					return;
				}

				secondConnect = true;

				Socket.ConnectAsync (SocketType.Stream, ProtocolType.Tcp, e);
			};

			Socket.ConnectAsync (SocketType.Stream, ProtocolType.Tcp, e);

			return tcs.Task;
		}
	}
}
