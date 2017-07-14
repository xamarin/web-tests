//
// ProxyConnection.cs
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
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Xamarin.AsyncTests;

namespace Xamarin.WebTests.Server
{
	using HttpFramework;

	class ProxyConnection : SocketConnection
	{
		new public ProxyListener Listener => (ProxyListener)base.Listener;

		public Listener TargetListener {
			get;
		}

		HttpConnection targetConnection;

		public ProxyConnection (ProxyListener listener, BuiltinProxyServer server, Socket socket)
			: base (listener, server, socket)
		{
			TargetListener = listener.Target.Listener;
		}

		public override async Task AcceptAsync (TestContext ctx, CancellationToken cancellationToken)
		{
			await base.AcceptAsync (ctx, cancellationToken).ConfigureAwait (false);
		}

		protected override void StartOperation_internal (TestContext ctx, HttpOperation operation)
		{
			(targetConnection, _) = TargetListener.CreateConnection (ctx, operation, false);
			ctx.LogDebug (5, $"{ME} CREATE TARGET CONNECTION: {targetConnection.ME}");

			base.StartOperation_internal (ctx, operation);
		}

		internal async Task RunTarget (TestContext ctx, HttpOperation operation, CancellationToken cancellationToken)
		{
			await targetConnection.AcceptAsync (ctx, cancellationToken).ConfigureAwait (false);
			await targetConnection.Initialize (ctx, cancellationToken);
			await TargetListener.Server.HandleConnection (ctx, operation, targetConnection, cancellationToken);
		}

		protected override void Close ()
		{
			if (targetConnection != null) {
				targetConnection.Dispose ();
				targetConnection = null;
			}
			base.Close ();
		}
	}
}
