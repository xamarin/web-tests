﻿//
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
		HttpRequest ()
		{
		}

		public HttpRequest (HttpProtocol protocol, string method, string path, NameValueCollection headers)
			: base (protocol, headers)
		{
			Method = method;
			Path = path;
		}

		public static async Task<HttpRequest> Read (StreamReader reader, CancellationToken cancellationToken)
		{
			cancellationToken.ThrowIfCancellationRequested ();
			var request = new HttpRequest ();
			await request.InternalRead (reader, cancellationToken);
			return request;
		}

		public string Method {
			get; private set;
		}

		public string Path {
			get; private set;
		}

		async Task InternalRead (StreamReader reader, CancellationToken cancellationToken)
		{
			cancellationToken.ThrowIfCancellationRequested ();
			var header = await reader.ReadLineAsync ();
			if (header == null)
				throw new IOException ("Connection has been closed.");

			var fields = header.Split (new char[] { ' ' }, StringSplitOptions.None);
			if (fields.Length != 3)
				throw new InvalidOperationException ();

			Method = fields [0];
			Protocol = ProtocolFromString (fields [2]);
			if (Method.Equals ("CONNECT"))
				Path = fields [1];
			else
				Path = fields [1].StartsWith ("/", StringComparison.Ordinal) ? fields [1] : new Uri (fields [1]).AbsolutePath;

			await ReadHeaders (reader, cancellationToken);
		}

		public async Task Write (StreamWriter writer, CancellationToken cancellationToken)
		{
			cancellationToken.ThrowIfCancellationRequested ();
			await writer.WriteAsync (string.Format ("{0} {1} {2}\r\n", Method, Path, ProtocolToString (Protocol)));
			await WriteHeaders (writer, cancellationToken);
		}

		public override string ToString ()
		{
			return string.Format ("[HttpRequest: Method={0}, Path={1}]", Method, Path);
		}
	}
}

