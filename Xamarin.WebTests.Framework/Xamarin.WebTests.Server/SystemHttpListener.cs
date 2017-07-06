﻿﻿//
// SystemHttpListener.cs
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
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Xamarin.AsyncTests;
using Xamarin.AsyncTests.Portable;
using Xamarin.WebTests.ConnectionFramework;
using Xamarin.WebTests.HttpFramework;

namespace Xamarin.WebTests.Server {
	class SystemHttpListener : BuiltinListener {
		public SystemHttpListener (TestContext ctx, HttpServer server)
			: base (ctx, server)
		{
			if (server.SslStreamProvider != null) {
				ctx.Assert (server.SslStreamProvider.SupportsHttpListener, "ISslStreamProvider.SupportsHttpListener");
				listener = server.SslStreamProvider.CreateHttpListener (server.Parameters);
			} else {
				listener = new HttpListener ();
			}

			listener.Prefixes.Add (Server.Uri.AbsoluteUri);
			listener.Start ();
		}

		HttpListener listener;

		public override async Task<HttpConnection> AcceptAsync (CancellationToken cancellationToken)
		{
			TestContext.LogDebug (5, "LISTEN ASYNC: {0}", Server.Uri);

			var cts = CancellationTokenSource.CreateLinkedTokenSource (cancellationToken);

			try {
				cts.Token.Register (() => {
					TestContext.LogDebug (5, "LISTENER ABORT!");
					listener.Abort ();
				});

				var context = await listener.GetContextAsync ().ConfigureAwait (false);
				return new HttpListenerConnection (TestContext, Server, context);
			} finally {
				cts.Dispose ();
			}
		}

		protected override async Task<bool> HandleConnection (HttpConnection connection, CancellationToken cancellationToken)
		{
			var request = await connection.ReadRequest (TestContext, cancellationToken);

			cancellationToken.ThrowIfCancellationRequested ();
			TestContext.LogDebug (4, "Handle request #1: {0} {1} {2}", connection, request, connection.RemoteEndPoint);
			var result = await Server.HandleConnection (TestContext, connection, request, cancellationToken);

			TestContext.LogDebug (4, "Handle request #2: {0} {1} {2}", connection, request, result);

			return result;
		}

		protected override void Shutdown ()
		{
			try {
				listener.Abort ();
				listener.Stop ();
				listener.Close ();
			} catch {
				;
			}
			listener = null;
			base.Shutdown ();
		}
	}
}
