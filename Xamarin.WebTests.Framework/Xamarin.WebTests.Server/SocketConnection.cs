﻿﻿﻿//
// SocketConnection.cs
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
using System.Net.Security;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Xamarin.AsyncTests;
using Xamarin.AsyncTests.Portable;
using Xamarin.AsyncTests.Constraints;
using Xamarin.WebTests.ConnectionFramework;
using Xamarin.WebTests.HttpFramework;

namespace Xamarin.WebTests.Server
{
	class SocketConnection : HttpConnection
	{
		public Socket Socket {
			get;
			private set;
		}

		public Stream Stream {
			get;
			private set;
		}

		public override SslStream SslStream => sslStream;

		Stream networkStream;
		SslStream sslStream;
		HttpStreamReader reader;
		StreamWriter writer;

		public SocketConnection (HttpServer server, Socket socket)
			: base (server, (IPEndPoint)socket.RemoteEndPoint)
		{
			Socket = socket;
		}

		public override async Task Initialize (TestContext ctx, CancellationToken cancellationToken)
		{
			networkStream = new NetworkStream (Socket, true);

			if (Server.SslStreamProvider != null) {
				sslStream = await CreateSslStream (ctx, networkStream, cancellationToken).ConfigureAwait (false);
				Stream = sslStream;
			} else {
				Stream = networkStream;
			}

			reader = new HttpStreamReader (Stream);
			writer = new StreamWriter (Stream);
			writer.AutoFlush = true;
		}

		async Task<SslStream> CreateSslStream (TestContext ctx, Stream innerStream, CancellationToken cancellationToken)
		{
			var stream = Server.SslStreamProvider.CreateSslStream (ctx, innerStream, Server.Parameters, true);

			var certificate = Server.Parameters.ServerCertificate;
			var askForCert = Server.Parameters.AskForClientCertificate || Server.Parameters.RequireClientCertificate;
			var protocol = Server.SslStreamProvider.GetProtocol (Server.Parameters, true);

			await stream.AuthenticateAsServerAsync (certificate, askForCert, protocol, false).ConfigureAwait (false);

			return stream;
		}

		internal override bool IsStillConnected ()
		{
			try {
				if (!Socket.Poll (-1, SelectMode.SelectRead))
					return false;
				return Socket.Available > 0;
			} catch {
				return false;
			}
		}

		public override async Task<bool> HasRequest (CancellationToken cancellationToken)
		{
			return !await reader.IsEndOfStream (cancellationToken).ConfigureAwait (false);
		}

		public override Task<HttpRequest> ReadRequest (CancellationToken cancellationToken)
		{
			return HttpRequest.Read (reader, cancellationToken);
		}

		public override Task<HttpResponse> ReadResponse (CancellationToken cancellationToken)
		{
			return HttpResponse.Read (reader, cancellationToken);
		}

		internal override Task WriteRequest (HttpRequest request, CancellationToken cancellationToken)
		{
			return request.Write (writer, cancellationToken);
		}

		internal override Task WriteResponse (HttpResponse response, CancellationToken cancellationToken)
		{
			return response.Write (writer, cancellationToken);
		}

		protected override void Close ()
		{
			if (reader != null) {
				reader.Dispose ();
				reader = null;
			}
			if (writer != null) {
				writer.Dispose ();
				writer.Dispose ();
			}
			if (sslStream != null) {
				sslStream.Dispose ();
				sslStream = null;
			}
			if (networkStream != null) {
				try {
					networkStream.Dispose ();
				} catch {
					;
				} finally {
					networkStream = null;
				}
			}
			if (Stream != null) {
				try {
					Stream.Dispose ();
				} catch {
					;
				} finally {
					Stream = null;
				}
			}
			if (Socket != null) {
				try {
					Socket.Shutdown (SocketShutdown.Both);
				} catch {
					;
				} finally {
					Socket.Close ();
					Socket.Dispose ();
					Socket = null;
				}
			}
		}
	}
}
