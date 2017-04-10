//
// Connection.cs
//
// Author:
//       Martin Baulig <martin.baulig@xamarin.com>
//
// Copyright (c) 2014 Xamarin Inc. (http://www.xamarin.com)
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
using System.Text;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xamarin.AsyncTests;
using Xamarin.AsyncTests.Portable;
using Xamarin.AsyncTests.Constraints;

namespace Xamarin.WebTests.HttpFramework
{
	using ConnectionFramework;
	using Server;

	public class HttpConnection
	{
		public TestContext Context {
			get;
		}

		public HttpServer Server {
			get;
		}

		public Stream Stream {
			get;
		}

		public ISslStream SslStream {
			get;
		}

		StreamReader reader;
		StreamWriter writer;

		public HttpConnection (TestContext ctx, HttpServer server, Stream stream, ISslStream sslStream)
		{
			Context = ctx;
			Server = server;
			Stream = stream;
			SslStream = sslStream;

			reader = new StreamReader (stream);
			writer = new StreamWriter (stream);
			writer.AutoFlush = true;
		}

		public bool HasRequest ()
		{
			return reader.Peek () >= 0 && !reader.EndOfStream;
		}

		public HttpRequest ReadRequest ()
		{
			if (reader.Peek () < 0 && reader.EndOfStream)
				return null;
			return HttpRequest.Read (reader);
		}

		protected HttpResponse ReadResponse ()
		{
			return HttpResponse.Read (reader);
		}

		protected void WriteRequest (HttpRequest request)
		{
			request.Write (writer);
		}

		public void WriteResponse (HttpResponse response)
		{
			response.Write (writer);
		}

		public void Close ()
		{
			writer.Flush ();
		}

		public void CheckEncryption (TestContext ctx)
		{
			if ((Server.Backend.Flags & (ListenerFlags.SSL | ListenerFlags.ForceTls12)) == 0)
				return;

			ctx.Assert (SslStream, Is.Not.Null, "Needs SslStream");
			ctx.Assert (SslStream.IsAuthenticated, "Must be authenticated");

			var support = DependencyInjector.Get<IPortableSupport> ();
			if (((Server.Backend.Flags & ListenerFlags.ForceTls12) != 0) || support.HasAppleTls)
				ctx.Assert (SslStream.ProtocolVersion, Is.EqualTo (ProtocolVersions.Tls12), "Needs TLS 1.2");
		}
	}
}

