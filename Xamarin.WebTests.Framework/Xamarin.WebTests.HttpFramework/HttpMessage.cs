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
using System.Threading.Tasks;
using System.Globalization;
using System.Collections.Generic;

namespace Xamarin.WebTests.HttpFramework
{
	public enum HttpProtocol {
		Http10,
		Http11
	}

	public abstract class HttpMessage
	{
		public HttpProtocol Protocol {
			get; protected set;
		}

		public HttpContent Body {
			get {
				return body;
			}
			set {
				body = value;
				hasBody = true;
			}
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
		HttpContent body;

		Dictionary<string,string> headers = new Dictionary<string, string> ();

		protected HttpMessage ()
		{
		}

		internal static HttpProtocol ProtocolFromString (string proto)
		{
			if (proto.Equals ("HTTP/1.0", StringComparison.OrdinalIgnoreCase))
				return HttpProtocol.Http10;
			else if (proto.Equals ("HTTP/1.1", StringComparison.OrdinalIgnoreCase))
				return HttpProtocol.Http11;
			else
				throw new InvalidOperationException ();
		}

		internal static string ProtocolToString (HttpProtocol proto)
		{
			if (proto == HttpProtocol.Http10)
				return "HTTP/1.0";
			else if (proto == HttpProtocol.Http11)
				return "HTTP/1.1";
			else
				throw new InvalidOperationException ();
		}

		protected void ReadHeaders (StreamReader reader)
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

		public abstract HttpContent ReadBody ();

		protected async Task DoReadBody (StreamReader reader)
		{
			if (hasBody)
				return;
			hasBody = true;

			string contentType = null;
			string transferEncoding = null;
			int? contentLength = null;

			string value;
			if (Headers.TryGetValue ("Content-Type", out value))
				contentType = value;
			if (Headers.TryGetValue ("Transfer-Encoding", out value))
				transferEncoding = value;
			if (Headers.TryGetValue ("Content-Length", out value))
				contentLength = int.Parse (value);

			if (contentType != null && contentType.Equals ("application/octet-stream")) {
				body = await BinaryContent.Read (reader, contentLength.Value);
			} else if (contentLength != null) {
				body = await StringContent.Read (reader, contentLength.Value);
			} else if (transferEncoding != null) {
				if (!transferEncoding.Equals ("chunked"))
					throw new InvalidOperationException ();
				body = await ChunkedContent.Read (reader);
			}
		}

		int GetContentLength ()
		{
			string value;
			if (!Headers.TryGetValue ("Content-Length", out value))
				throw new InvalidOperationException ();
			return int.Parse (value);
		}
	}
}

