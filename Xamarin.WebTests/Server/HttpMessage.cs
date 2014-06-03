//
// HttpMessage.cs
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
using System.Globalization;
using System.Collections.Generic;

namespace Xamarin.WebTests.Server
{
	public abstract class HttpMessage
	{
		public string Protocol {
			get; protected set;
		}

		public string Body {
			get; protected set;
		}

		public Connection Connection {
			get { return connection; }
		}

		public IDictionary<string,string> Headers {
			get { return headers; }
		}

		public void AddHeader (string header, object value)
		{
			headers.Add (header, value.ToString ());
		}

		public void SetHeader (string header, object value)
		{
			if (headers.ContainsKey (header))
				headers [header] = value.ToString ();
			else
				headers.Add (header, value.ToString ());
		}

		bool hasBody;
		string body;

		protected readonly StreamReader reader;
		protected readonly Connection connection;
		Dictionary<string,string> headers = new Dictionary<string, string> ();

		protected HttpMessage ()
		{
		}

		protected HttpMessage (Connection connection, StreamReader reader)
		{
			this.connection = connection;
			this.reader = reader;
			Read ();
		}

		protected abstract void Read ();

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

		protected void WriteHeaders (StreamWriter writer)
		{
			foreach (var entry in Headers)
				writer.Write ("{0}: {1}\r\n", entry.Key, entry.Value);
			writer.Write ("\r\n");
		}

		public string ReadBody ()
		{
			DoReadBody ();
			return body;
		}

		void DoReadBody ()
		{
			if (hasBody)
				return;
			hasBody = true;

			string value;
			if (Headers.TryGetValue ("Content-Length", out value))
				body = ReadStaticBody (int.Parse (value));
			else if (Headers.TryGetValue ("Transfer-Encoding", out value)) {
				if (!value.Equals ("chunked"))
					throw new InvalidOperationException ();
				body = ReadChunkedBody ();
			}
		}

		string ReadStaticBody (int length)
		{
			var chunkSize = connection.ReadChunkSize ?? 4096;
			var minDelay = connection.ReadChunkMinDelay ?? 0;
			var maxDelay = connection.ReadChunkMaxDelay ?? 0;

			var random = new Random ();
			var delayRange = maxDelay - minDelay;

			var buffer = new char [length];
			int offset = 0;
			while (offset < length) {
				int delay = minDelay + random.Next (delayRange);
				Thread.Sleep (delay);

				var size = Math.Min (length - offset, chunkSize);
				int ret = reader.Read (buffer, offset, size);
				if (ret <= 0)
					throw new InvalidOperationException ();

				offset += ret;
			}

			return new string (buffer);
		}

		string ReadChunkedBody ()
		{
			var body = new StringBuilder ();
			CopyChunkedBody (new StringWriter (body), false);
			return body.ToString ();
		}

		void CopyChunkedBody (TextWriter writer, bool verbose)
		{
			do {
				var header = reader.ReadLine ();
				if (verbose) {
					writer.Write (header);
					writer.Write ('\r');
					writer.Write ('\n');
				}
				var length = int.Parse (header, NumberStyles.HexNumber);
				if (length == 0)
					break;

				var buffer = new char [length];
				var ret = reader.Read (buffer, 0, length);
				if (ret != length)
					throw new InvalidOperationException ();

				writer.Write (buffer, 0, length);
				if (verbose) {
					writer.Write ('\r');
					writer.Write ('\n');
				}

				var empty = reader.ReadLine ();
				if (!string.IsNullOrEmpty (empty))
					throw new InvalidOperationException ();
			} while (true);
		}

		internal void CopyBody (TextWriter writer)
		{
			if (hasBody)
				throw new InvalidOperationException ();
			string value;
			if (Headers.TryGetValue ("Content-Length", out value)) {
				var body = ReadStaticBody (int.Parse (value));
				writer.Write (body);
			} else if (Headers.TryGetValue ("Transfer-Encoding", out value)) {
				if (!value.Equals ("chunked"))
					throw new InvalidOperationException ();
				CopyChunkedBody (writer, true);
			}
		}
	}
}

