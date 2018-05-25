//
// BeginEndAsync.cs
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
	public class BeginEndAsync : SocketTestFixture
	{
		public bool UseDnsEndPoint {
			get;
		}

		[AsyncTest]
		public BeginEndAsync (bool useDnsEndPoint)
		{
			UseDnsEndPoint = useDnsEndPoint;
		}

		protected override void CreateParameters (TestContext ctx, ConnectionParameters parameters)
		{
			var port = TestContext.GetUniquePort ();
			parameters.ListenAddress = new IPEndPoint (IPAddress.Loopback, port);
			if (UseDnsEndPoint)
				parameters.EndPoint = new DnsEndPoint ("localhost", port);
			else
				parameters.EndPoint = parameters.ListenAddress;
			base.CreateParameters (ctx, parameters);
		}

		protected override Task<Socket> StartServer (TestContext ctx, EndPoint endPoint, CancellationToken cancellationToken)
		{
			var socket = ctx.RegisterDispose (new Socket (AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp));
			socket.Bind (endPoint);
			socket.Listen (1);

			var tcs = new TaskCompletionSource<Socket> ();

			socket.BeginAccept (ar => {
				try {
					var accepted = socket.EndAccept (ar);
					cancellationToken.ThrowIfCancellationRequested ();
					ctx.RegisterDispose (accepted);
					tcs.SetResult (accepted);
				} catch (Exception ex) {
					tcs.SetException (ex);
				}
			}, null);

			return tcs.Task;
		}

		protected override Task<Socket> StartClient (TestContext ctx, EndPoint endPoint, CancellationToken cancellationToken)
		{
			var socket = ctx.RegisterDispose (new Socket (AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp));

			var tcs = new TaskCompletionSource<Socket> ();

			socket.BeginConnect (endPoint, ar => {
				try {
					socket.EndConnect (ar);
					cancellationToken.ThrowIfCancellationRequested ();
					tcs.SetResult (socket);
				} catch (Exception ex) {
					tcs.SetException (ex);
				}
			}, null);

			return tcs.Task;
		}
	}
}
