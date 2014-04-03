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
using System.Net.Sockets;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Xamarin.WebTests.Server
{
	public class Connection
	{
		Listener server;
		NetworkStream stream;
		StreamReader reader;
		StreamWriter writer;
		Dictionary<string,string> headers;

		public Connection (Listener server, Socket socket)
		{
			this.server = server;

			stream = new NetworkStream (socket);

			reader = new StreamReader (stream);
			writer = new StreamWriter (stream);
			writer.AutoFlush = true;
			headers = new Dictionary<string, string> ();
		}

		public Listener Server {
			get { return server; }
		}

		public string Method {
			get; private set;
		}

		public string Path {
			get; private set;
		}

		public Uri RequestUri {
			get; private set;
		}

		public StreamReader RequestReader {
			get { return reader; }
		}

		public StreamWriter ResponseWriter {
			get { return writer; }
		}

		public IDictionary<string,string> Headers {
			get { return headers; }
		}

		public void ReadHeaders ()
		{
			string line;
			var header = reader.ReadLine ();
			var fields = header.Split (new char[] { ' ' }, StringSplitOptions.None);
			if (fields.Length != 3) {
				Console.Error.WriteLine ("GOT INVALID HTTP REQUEST: {0}", header);
				throw new InvalidOperationException ();
			}

			Method = fields [0];
			Path = fields [1];
			RequestUri = new Uri (server.Uri, Path);
			var proto = fields [2];

			if (!proto.Equals ("HTTP/1.1") && !proto.Equals ("HTTP/1.0"))
				throw new InvalidOperationException ();

			while ((line = reader.ReadLine ()) != null) {
				if (string.IsNullOrEmpty (line))
					break;
				var pos = line.IndexOf (':');
				if (pos < 0)
					throw new InvalidOperationException ();

				var headerName = line.Substring (0, pos);
				var headerValue = line.Substring (pos + 1).Trim ();
				headers.Add (headerName, headerValue);
			}
		}

		public void Close ()
		{
			writer.Flush ();
			stream.Close ();
		}
	}
}

