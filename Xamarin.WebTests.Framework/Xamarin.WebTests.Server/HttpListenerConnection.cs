﻿//
// HttpListenerConnection.cs
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
using System.Threading;
using System.Threading.Tasks;
using Xamarin.AsyncTests;
using Xamarin.AsyncTests.Portable;
using Xamarin.WebTests.ConnectionFramework;
using Xamarin.WebTests.HttpFramework;
using Xamarin.WebTests.HttpHandlers;

namespace Xamarin.WebTests.Server {
	class HttpListenerConnection : HttpConnection {
		internal TestContext TestContext {
			get;
		}

		public HttpListenerContext Context {
			get;
		}

		public override SslStream SslStream {
			get { return sslStream; }
		}

		SslStream sslStream;

		public HttpListenerConnection (TestContext ctx, HttpServer server, HttpListenerContext context)
			: base (server, context.Request.RemoteEndPoint)
		{
			TestContext = ctx;
			Context = context;
		}

		public override Task Initialize (TestContext ctx, CancellationToken cancellationToken)
		{
			if (Server.SslStreamProvider?.SupportsHttpListenerContext ?? false)
				sslStream = Server.SslStreamProvider.GetSslStream (Context);

			return Handler.CompletedTask;
		}

		internal override bool IsStillConnected ()
		{
			return false;
		}

		public override Task<bool> HasRequest (CancellationToken cancellationToken)
		{
			throw new NotImplementedException ();
		}

		public override async Task<HttpRequest> ReadRequest (CancellationToken cancellationToken)
		{
			var listenerRequest = Context.Request;
			var protocol = GetProtocol (listenerRequest.ProtocolVersion);
			var request = new HttpRequest (protocol, listenerRequest.HttpMethod, listenerRequest.RawUrl, listenerRequest.Headers);

			TestContext.LogDebug (5, "GOT REQUEST: {0} {1}", request, listenerRequest.HasEntityBody);

			cancellationToken.ThrowIfCancellationRequested ();
			var body = await ReadBody (request, cancellationToken).ConfigureAwait (false);
			request.SetBody (body);

			return request;
		}

		public override Task<HttpResponse> ReadResponse (CancellationToken cancellationToken)
		{
			throw new NotImplementedException ();
		}

		async Task<HttpContent> ReadBody (HttpMessage message, CancellationToken cancellationToken)
		{
			TestContext.LogDebug (5, "READ BODY: {0}", message);
			using (var reader = new HttpStreamReader (Context.Request.InputStream)) {
				cancellationToken.ThrowIfCancellationRequested ();
				if (message.ContentType != null && message.ContentType.Equals ("application/octet-stream"))
					return await BinaryContent.Read (reader, message.ContentLength.Value, cancellationToken);
				if (message.ContentLength != null)
					return await StringContent.Read (reader, message.ContentLength.Value, cancellationToken);
				if (message.TransferEncoding != null) {
					if (!message.TransferEncoding.Equals ("chunked"))
						throw new InvalidOperationException ();
					return await ChunkedContent.ReadNonChunked (reader, cancellationToken);
				}
				return null;
			}
		}

		internal override async Task WriteResponse (HttpResponse response, CancellationToken cancellationToken)
		{
			await Task.Yield ();
			cancellationToken.ThrowIfCancellationRequested ();

			TestContext.LogDebug (5, "WRITE RESPONSE: {0}", response);

			try {
				Context.Response.StatusCode = (int)response.StatusCode;
				Context.Response.ProtocolVersion = GetProtocol (response.Protocol);
			} catch (ObjectDisposedException) {
				throw new ConnectionException ("HttpContext.Response already closed when trying to write response.");
			}

			Context.Response.KeepAlive = (Server.Flags & HttpServerFlags.ReuseConnection) != 0;

			foreach (var header in response.Headers) {
				Context.Response.AddHeader (header.Key, header.Value);
			}

			if (response.Body != null) {
				cancellationToken.ThrowIfCancellationRequested ();
				using (var writer = new StreamWriter (Context.Response.OutputStream))
					await response.Body.WriteToAsync (writer);
			}
		}

		internal override Task WriteRequest (HttpRequest request, CancellationToken cancellationToken)
		{
			throw new NotImplementedException ();
		}

		static readonly Version Version10 = new Version (1, 0);
		static readonly Version Version11 = new Version (1, 1);
		static readonly Version Version12 = new Version (1, 2);

		static Version GetProtocol (HttpProtocol protocol)
		{
			switch (protocol) {
			case HttpProtocol.Http10:
				return Version10;
			case HttpProtocol.Http11:
				return Version11;
			case HttpProtocol.Http12:
				return Version12;
			default:
				throw new InternalErrorException ();
			}
		}

		static HttpProtocol GetProtocol (Version version)
		{
			if (version.Equals (Version10))
				return HttpProtocol.Http10;
			else if (version.Equals (Version11))
				return HttpProtocol.Http11;
			else if (version.Equals (Version12))
				return HttpProtocol.Http12;
			throw new InternalErrorException ();
		}

		protected override void Close ()
		{
			Context.Response.Close ();
		}
	}
}
