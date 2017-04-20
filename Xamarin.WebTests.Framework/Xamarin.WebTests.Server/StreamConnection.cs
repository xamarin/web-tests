﻿﻿//
// StreamConnection.cs
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
using Xamarin.AsyncTests.Portable;
using Xamarin.AsyncTests.Constraints;
using Xamarin.WebTests.ConnectionFramework;
using Xamarin.WebTests.HttpFramework;

namespace Xamarin.WebTests.Server
{
	class StreamConnection : HttpConnection
	{
		public Socket Socket {
			get;
			private set;
		}

		public Stream Stream {
			get;
			private set;
		}

		Stream networkStream;
		ISslStream sslStream;
		HttpStreamReader reader;
		StreamWriter writer;

		StreamConnection (TestContext ctx, HttpServer server, Socket socket, Stream networkStream, Stream stream, ISslStream sslStream)
			: base (ctx, server, sslStream)
		{
			this.networkStream = networkStream;
			this.sslStream = sslStream;

			Stream = stream;

			reader = new HttpStreamReader (stream);
			writer = new StreamWriter (stream);
			writer.AutoFlush = true;
		}

		public static async Task<HttpConnection> CreateServer (TestContext ctx, HttpServer server, Socket socket,
		                                                       CancellationToken cancellationToken)
		{
			cancellationToken.ThrowIfCancellationRequested ();

			var stream = new NetworkStream (socket, true);

			var builtinHttpServer = server as BuiltinHttpServer;
			if (builtinHttpServer == null || builtinHttpServer.SslStreamProvider == null)
				return new StreamConnection (ctx, server, socket, stream, stream, null);

			var sslStream = await builtinHttpServer.SslStreamProvider.CreateServerStreamAsync (
				stream, builtinHttpServer.Parameters, cancellationToken).ConfigureAwait (false);
			return new StreamConnection (ctx, server, socket, stream, sslStream.AuthenticatedStream, sslStream);
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
				sslStream.Close ();
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
