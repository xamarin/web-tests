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
		NetworkStream stream;
		StreamReader reader;
		StreamWriter writer;
		Dictionary<string,string> headers;

		public Connection (Socket socket)
		{
			stream = new NetworkStream (socket);

			reader = new StreamReader (stream);
			writer = new StreamWriter (stream);
			writer.AutoFlush = true;
			headers = new Dictionary<string, string> ();
		}

		protected Connection (Socket socket, string method, string path, string protocol)
			: this (socket)
		{
			Method = method;
			Path = path;
			Protocol = protocol;
		}

		public string Method {
			get; private set;
		}

		public string Path {
			get; private set;
		}

		public string Protocol {
			get; private set;
		}

		public int StatusCode {
			get; private set;
		}

		public string StatusMessage {
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

		public void ReadRequest (Uri serverUri)
		{
			var header = reader.ReadLine ();
			var fields = header.Split (new char[] { ' ' }, StringSplitOptions.None);
			if (fields.Length != 3) {
				Console.Error.WriteLine ("GOT INVALID HTTP REQUEST: {0}", header);
				throw new InvalidOperationException ();
			}

			Method = fields [0];
			Path = fields [1];
			RequestUri = new Uri (serverUri, Path);
			Protocol = fields [2];

			if (!Protocol.Equals ("HTTP/1.1") && !Protocol.Equals ("HTTP/1.0"))
				throw new InvalidOperationException ();

			ReadHeaders ();
		}

		public void ReadResponse ()
		{
			var header = reader.ReadLine ();
			var fields = header.Split (new char[] { ' ' }, StringSplitOptions.None);
			if (fields.Length < 2 || fields.Length > 3) {
				Console.Error.WriteLine ("GOT INVALID HTTP REQUEST: {0}", header);
				throw new InvalidOperationException ();
			}

			Protocol = fields [0];
			StatusCode = int.Parse (fields [1]);
			StatusMessage = fields.Length == 3 ? fields [2] : string.Empty;

			if (!Protocol.Equals ("HTTP/1.1") && !Protocol.Equals ("HTTP/1.0"))
				throw new InvalidOperationException ();

			ReadHeaders ();
		}

		protected void ReadHeaders ()
		{
			string line;
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

		internal void WriteSimpleResponse (int status, string message, string body)
		{
			ResponseWriter.WriteLine ("HTTP/1.1 {0} {1}", status, message);
			ResponseWriter.WriteLine ("Content-Type: text/plain");
			if (body != null) {
				ResponseWriter.WriteLine ("Content-Length: {0}", body.Length);
				ResponseWriter.WriteLine ("");
				ResponseWriter.WriteLine (body);
			} else {
				ResponseWriter.WriteLine ("");
			}
		}

		internal void WriteRedirect (int code, Uri uri)
		{
			ResponseWriter.WriteLine ("HTTP/1.1 {0}", code);
			ResponseWriter.WriteLine ("Location: {0}", uri);
			ResponseWriter.WriteLine ();
		}

		internal void WriteSuccess (string body = null)
		{
			WriteSimpleResponse (200, "OK", body);
		}

		internal void WriteError (string message, params object[] args)
		{
			WriteSimpleResponse (500, "ERROR", string.Format (message, args));
		}
	}
}

