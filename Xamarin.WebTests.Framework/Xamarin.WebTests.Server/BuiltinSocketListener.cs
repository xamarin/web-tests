﻿﻿//
// BuiltinSocketListener.cs
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
	abstract class BuiltinSocketListener : BuiltinListener
	{
		public IPEndPoint NetworkEndPoint {
			get;
		}

		Socket server;

		public BuiltinSocketListener (TestContext ctx, IPortableEndPoint endpoint, HttpServerFlags flags)
			: base (ctx)
		{
			var ssl = (flags & HttpServerFlags.SSL) != 0;
			if (ssl & (flags & HttpServerFlags.Proxy) != 0)
				throw new InternalErrorException ();

			var address = IPAddress.Parse (endpoint.Address);
			NetworkEndPoint = new IPEndPoint (address, endpoint.Port);

			server = new Socket (AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
			server.Bind (NetworkEndPoint);
			server.Listen (1);
		}

		public override async Task<BuiltinListenerContext> AcceptAsync (CancellationToken cancellationToken)
		{
			TestContext.LogDebug (5, "LISTEN ASYNC: {0}", NetworkEndPoint);

			var accepted = await server.AcceptAsync (cancellationToken).ConfigureAwait (false);
			return new BuiltinSocketContext (accepted);
		}

		protected override void Shutdown ()
		{
			TestContext.LogDebug (5, "SHUTDOWN: {0}", server.Connected);
			if (server.Connected)
				server.Shutdown (SocketShutdown.Both);
			server.Close ();
			base.Shutdown ();
		}

	}
}
