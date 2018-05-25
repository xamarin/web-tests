//
// SocketBackend.cs
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
using System.IO;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Net.Security;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Xamarin.AsyncTests;
using Xamarin.AsyncTests.Portable;
using Xamarin.WebTests.HttpFramework;

namespace Xamarin.WebTests.Server
{
	class SocketBackend : ListenerBackend
	{
		protected Socket Socket {
			get;
		}

		List<SocketConnection> connections;

		public SocketBackend (TestContext ctx, HttpServer server)
			: base (server)
		{
			var ssl = (server.Flags & HttpServerFlags.SSL) != 0;
			if (ssl & (server.Flags & HttpServerFlags.Proxy) != 0)
				throw new InternalErrorException ();

			Socket = new Socket (AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
			Socket.Bind (server.ListenAddress);
			Socket.Listen (128);

			connections = new List<SocketConnection> ();
		}

		public override HttpConnection CreateConnection ()
		{
			return new SocketConnection (Server, Socket);
		}

		protected override void Close ()
		{
			if (Socket.Connected)
				Socket.Shutdown (SocketShutdown.Both);
			Socket.Close ();
		}
	}
}
