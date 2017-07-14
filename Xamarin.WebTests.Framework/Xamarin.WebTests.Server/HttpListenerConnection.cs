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
		public HttpListener Listener {
			get;
		}

		public HttpListenerContext Context {
			get;
			private set;
		}

		public override SslStream SslStream {
			get { return sslStream; }
		}

		internal override IPEndPoint RemoteEndPoint => remoteEndPoint;

		SslStream sslStream;
		IPEndPoint remoteEndPoint;
		HttpOperation currentOperation;

		public HttpListenerConnection (HttpServer server, HttpListener listener)
			: base (server)
		{
			Listener = listener;
		}

		public override async Task AcceptAsync (TestContext ctx, CancellationToken cancellationToken)
		{
			Context = await Listener.GetContextAsync ().ConfigureAwait (false);
			remoteEndPoint = Context.Request.RemoteEndPoint;
		}

		public override Task Initialize (TestContext ctx, CancellationToken cancellationToken)
		{
			if (Server.SslStreamProvider?.SupportsHttpListenerContext ?? false)
				sslStream = Server.SslStreamProvider.GetSslStream (Context);

			return Handler.CompletedTask;
		}

		public override Task<bool> ReuseConnection (TestContext ctx, CancellationToken cancellationToken)
		{
			return Task.FromResult (false);
		}

		internal override bool IsStillConnected ()
		{
			return false;
		}

		public override Task<bool> HasRequest (CancellationToken cancellationToken)
		{
			return Task.FromResult (Listener.IsListening);
		}

		public override async Task<HttpRequest> ReadRequest (TestContext ctx, CancellationToken cancellationToken)
		{
			var listenerRequest = Context.Request;
			var protocol = GetProtocol (listenerRequest.ProtocolVersion);
			var request = new HttpRequest (protocol, listenerRequest.HttpMethod, listenerRequest.RawUrl, listenerRequest.Headers);

			ctx.LogDebug (5, "GOT REQUEST: {0} {1}", request, listenerRequest.HasEntityBody);

			cancellationToken.ThrowIfCancellationRequested ();
			var body = await ReadBody (ctx, request, cancellationToken).ConfigureAwait (false);
			request.SetBody (body);

			return request;
		}

		public override Task<HttpResponse> ReadResponse (TestContext ctx, CancellationToken cancellationToken)
		{
			throw new NotImplementedException ();
		}

		async Task<HttpContent> ReadBody (TestContext ctx, HttpMessage message, CancellationToken cancellationToken)
		{
			ctx.LogDebug (5, "READ BODY: {0}", message);
			using (var reader = new HttpStreamReader (Context.Request.InputStream)) {
				cancellationToken.ThrowIfCancellationRequested ();
				if (message.ContentType != null && message.ContentType.Equals ("application/octet-stream"))
					return await BinaryContent.Read (reader, message.ContentLength.Value, cancellationToken);
				if (message.ContentLength != null)
					return await StringContent.Read (ctx, reader, message.ContentLength.Value, cancellationToken);
				if (message.TransferEncoding != null) {
					if (!message.TransferEncoding.Equals ("chunked"))
						throw new InvalidOperationException ();
					return await ChunkedContent.ReadNonChunked (reader, cancellationToken);
				}
				return null;
			}
		}

		internal override async Task WriteResponse (TestContext ctx, HttpResponse response, CancellationToken cancellationToken)
		{
			await Task.Yield ();
			cancellationToken.ThrowIfCancellationRequested ();

			ctx.LogDebug (5, "WRITE RESPONSE: {0}", response);

			if (response.HttpListenerResponse != null) {
				if (response.HttpListenerResponse != Context.Response)
					throw new ConnectionException ("Invalid HttpListenerResponse object.");
				return;
			}

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
					await response.Body.WriteToAsync (ctx, writer);
			}
		}

		internal override Task WriteRequest (TestContext ctx, HttpRequest request, CancellationToken cancellationToken)
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

		public override bool StartOperation (TestContext ctx, HttpOperation operation)
		{
			lock (Listener) {
				ctx.LogDebug (5, $"{ME} START OPERATION: {currentOperation != null}");
				if (Interlocked.CompareExchange (ref currentOperation, operation, null) != null)
					return false;
				return true;
			}
		}

		public override void Continue (TestContext ctx, bool keepAlive)
		{
			lock (Listener) {
				ctx.LogDebug (5, $"{ME} CONTINUE: {keepAlive}");
				if (!keepAlive) {
					Close ();
					return;
				}

				currentOperation = null;
				OnClosed (true);
			}
		}

		protected override void Close ()
		{
			OnClosed (false);
			Context?.Response?.Close ();
		}
	}
}
