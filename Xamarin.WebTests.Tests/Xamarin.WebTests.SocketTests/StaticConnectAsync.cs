﻿//
// StaticConnectAsync.cs
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

	public class StaticConnectAsync : SocketTestFixture
	{
		public enum EndPointType
		{
			IPEndPoint,
			DnsEndPoint,
			[NotWorking]
			ReusingArgs,
			[NotWorking]
			ReusingArgsWithDnsEndPoint
		}

		public EndPointType Type {
			get;
		}

		public int Repeat {
			get;
		}

		[AsyncTest]
		public StaticConnectAsync (EndPointType type, [Repeat (2)] int repeat)
		{
			Type = type;
			Repeat = repeat;
		}

		protected override void CreateParameters (TestContext ctx, ConnectionParameters parameters)
		{
			var port = TestContext.GetUniquePort ();
			parameters.ListenAddress = new IPEndPoint (IPAddress.Loopback, port);
			switch (Type) {
			case EndPointType.DnsEndPoint:
			case EndPointType.ReusingArgsWithDnsEndPoint:
				parameters.EndPoint = new DnsEndPoint ("localhost", port);
				break;
			case EndPointType.IPEndPoint:
			case EndPointType.ReusingArgs:
				parameters.EndPoint = parameters.ListenAddress;
				break;
			default:
				throw ctx.AssertFail (Type);
			}
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

		static SocketAsyncEventArgs sharedArgs;

		protected override Task<Socket> StartClient (TestContext ctx, EndPoint endPoint, CancellationToken cancellationToken)
		{
			var socket = ctx.RegisterDispose (new Socket (AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp));

			var tcs = new TaskCompletionSource<Socket> ();

			ctx.LogMessage ($"{ME} START: {Type} {Repeat}");

			SocketAsyncEventArgs e;
			if (Type == EndPointType.ReusingArgs) {
				if (Repeat == 1)
					sharedArgs = new SocketAsyncEventArgs ();
				e = sharedArgs;
			} else {
				e = new SocketAsyncEventArgs ();
			}

			e.RemoteEndPoint = endPoint;
			e.Completed += Completed;

			Socket.ConnectAsync (SocketType.Stream, ProtocolType.Tcp, e);

			return tcs.Task;

			void Completed (object sender, SocketAsyncEventArgs _)
			{
				ctx.LogMessage ($"{ME} START #1: {tcs.Task.Status} {e.SocketError}");
				if (e.SocketError != SocketError.Success)
					tcs.SetException (new IOException ($"ConnectAsync() failed: {e.SocketError}"));
				else
					tcs.SetResult (e.ConnectSocket);
				e.Completed -= Completed;
			}
		}
	}
}
