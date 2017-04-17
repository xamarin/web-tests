//
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
using System.Threading;
using System.Threading.Tasks;
using Xamarin.AsyncTests;
using Xamarin.AsyncTests.Portable;
using Xamarin.AsyncTests.Constraints;
using Xamarin.WebTests.ConnectionFramework;
using Xamarin.WebTests.HttpFramework;

namespace Xamarin.WebTests.Server {
	class StreamConnection : HttpConnection {
		public Stream Stream {
			get;
		}

		public ISslStream SslStream {
			get;
		}

		StreamReader reader;
		StreamWriter writer;

		public StreamConnection (TestContext ctx, HttpServer server, Stream stream, ISslStream sslStream)
			: base (ctx, server)
		{
			Stream = stream;
			SslStream = sslStream;

			reader = new StreamReader (stream);
			writer = new StreamWriter (stream);
			writer.AutoFlush = true;
		}

		public override bool HasRequest ()
		{
			return reader.Peek () >= 0 && !reader.EndOfStream;
		}

		public override Task<HttpRequest> ReadRequest (CancellationToken cancellationToken)
		{
			return HttpRequest.Read (reader, cancellationToken);
		}

		public override Task<HttpResponse> ReadResponse (CancellationToken cancellationToken)
		{
			return HttpResponse.Read (reader, cancellationToken);
		}

		internal override async Task<HttpContent> ReadBody (HttpMessage message, CancellationToken cancellationToken)
		{
			cancellationToken.ThrowIfCancellationRequested ();
			if (message.ContentType != null && message.ContentType.Equals ("application/octet-stream"))
				return await BinaryContent.Read (reader, message.ContentLength.Value);
			if (message.ContentLength != null)
				return await StringContent.Read (reader, message.ContentLength.Value);
			if (message.TransferEncoding != null) {
				if (!message.TransferEncoding.Equals ("chunked"))
					throw new InvalidOperationException ();
				return await ChunkedContent.Read (reader);
			}
			return null;
		}

		internal override Task WriteRequest (HttpRequest request, CancellationToken cancellationToken)
		{
			return request.Write (writer, cancellationToken);
		}

		internal override Task WriteResponse (HttpResponse response, CancellationToken cancellationToken)
		{
			return response.Write (writer, cancellationToken);
		}

		internal override Task WriteBody (HttpContent content, CancellationToken cancellationToken)
		{
			return content.WriteToAsync (writer);
		}

		public override void CheckEncryption (TestContext ctx)
		{
			if ((Server.Flags & (HttpServerFlags.SSL | HttpServerFlags.ForceTls12)) == 0)
				return;

			ctx.Assert (SslStream, Is.Not.Null, "Needs SslStream");
			ctx.Assert (SslStream.IsAuthenticated, "Must be authenticated");

			var setup = DependencyInjector.Get <IConnectionFrameworkSetup> ();
			if (((Server.Flags & HttpServerFlags.ForceTls12) != 0) || setup.SupportsTls12)
				ctx.Assert (SslStream.ProtocolVersion, Is.EqualTo (ProtocolVersions.Tls12), "Needs TLS 1.2");
		}
	}
}
