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

		public override HttpRequest ReadRequest ()
		{
			if (reader.Peek () < 0 && reader.EndOfStream)
				return null;
			return HttpRequest.Read (reader);
		}

		protected override HttpResponse ReadResponse ()
		{
			return HttpResponse.Read (reader);
		}

		protected override void WriteRequest (HttpRequest request)
		{
			request.Write (writer);
		}

		public override void WriteResponse (HttpResponse response)
		{
			response.Write (writer);
		}

		public override void CheckEncryption (TestContext ctx)
		{
			if ((Server.Flags & (ListenerFlags.SSL | ListenerFlags.ForceTls12)) == 0)
				return;

			ctx.Assert (SslStream, Is.Not.Null, "Needs SslStream");
			ctx.Assert (SslStream.IsAuthenticated, "Must be authenticated");

			var setup = DependencyInjector.Get <IConnectionFrameworkSetup> ();
			if (((Server.Flags & ListenerFlags.ForceTls12) != 0) || setup.SupportsTls12)
				ctx.Assert (SslStream.ProtocolVersion, Is.EqualTo (ProtocolVersions.Tls12), "Needs TLS 1.2");
		}
	}
}
