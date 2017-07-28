﻿﻿//
// HttpRequest.cs
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
using System.Threading;
using System.Threading.Tasks;
using System.Globalization;
using System.Collections.Generic;
using System.Collections.Specialized;
using Xamarin.AsyncTests;

namespace Xamarin.WebTests.HttpFramework
{
	public class HttpRequest : HttpMessage
	{
		internal HttpRequest (HttpProtocol protocol, string method, string path, HttpStreamReader reader)
			: base (protocol)
		{
			Method = method;
			Path = path;
			this.reader = reader;
		}

		internal HttpRequest (HttpProtocol protocol, HttpListenerRequest request)
			: base (protocol, request.Headers)
		{
			this.listenerRequest = request;
			Method = request.HttpMethod;
			Path = request.RawUrl;
		}

		HttpStreamReader reader;
		HttpListenerRequest listenerRequest;
		bool headersRead;
		bool bodyRead;

		internal async Task ReadHeaders (TestContext ctx, CancellationToken cancellationToken)
		{
			if (headersRead)
				return;
			headersRead = true;

			if (listenerRequest != null)
				return;

			cancellationToken.ThrowIfCancellationRequested ();
			await ReadHeaders (ctx, reader, cancellationToken).ConfigureAwait (false);
		}

		internal async Task Read (TestContext ctx, CancellationToken cancellationToken)
		{
			if (bodyRead)
				return;
			bodyRead = true;

			await ReadHeaders (ctx, cancellationToken).ConfigureAwait (false);

			cancellationToken.ThrowIfCancellationRequested ();
			if (listenerRequest != null) {
				using (var bodyReader = new HttpStreamReader (listenerRequest.InputStream)) {
					Body = await ReadBody (ctx, bodyReader, true, cancellationToken).ConfigureAwait (false);
					return;
				}
			}

			cancellationToken.ThrowIfCancellationRequested ();
			Body = await ReadBody (ctx, reader, false, cancellationToken);
		}

		public string Method {
			get; private set;
		}

		public string Path {
			get; private set;
		}

		public async Task Write (TestContext ctx, StreamWriter writer, CancellationToken cancellationToken)
		{
			cancellationToken.ThrowIfCancellationRequested ();

			var header = string.Format ("{0} {1} {2}\r\n", Method, Path, ProtocolToString (Protocol));
			await writer.WriteAsync (header).ConfigureAwait (false);

			cancellationToken.ThrowIfCancellationRequested ();
			await WriteHeaders (writer, cancellationToken);

			if (Body != null) {
				cancellationToken.ThrowIfCancellationRequested ();
				await Body.WriteToAsync (ctx, writer);
			}
		}

		public override string ToString ()
		{
			return string.Format ("[HttpRequest: Method={0}, Path={1}]", Method, Path);
		}
	}
}

