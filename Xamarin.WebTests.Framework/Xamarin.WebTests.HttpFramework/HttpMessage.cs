﻿//
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
using System.Collections.Specialized;

namespace Xamarin.WebTests.HttpFramework
{
	public enum HttpProtocol {
		Http10,
		Http11,
		Http12
	}

	public abstract class HttpMessage
	{
		public HttpProtocol Protocol {
			get; protected set;
		}

		internal IReadOnlyDictionary<string,string> Headers {
			get { return headers; }
		}

		internal void AddHeader (string header, object value)
		{
			headers.Add (header, value.ToString ());
		}

		internal void SetHeader (string header, object value)
		{
			if (headers.ContainsKey (header))
				headers [header] = value.ToString ();
			else
				headers.Add (header, value.ToString ());
		}

		public int? ContentLength {
			get {
				string value;
				if (headers.TryGetValue ("Content-Length", out value))
					return int.Parse (value);
				return null;
			}
			set {
				if (value != null)
					headers ["Content-Length"] = value.ToString ();
				else
					headers.Remove ("Content-Length");
			}
		}

		public string ContentType {
			get {
				string value;
				if (headers.TryGetValue ("Content-Type", out value))
					return value;
				return null;
			}
			set {
				if (value != null)
					headers ["Content-Type"] = value;
				else
					headers.Remove (value);
			}
		}

		public string TransferEncoding {
			get {
				string value;
				if (headers.TryGetValue ("Transfer-Encoding", out value))
					return value;
				return null;
			}
			set {
				if (value != null)
					headers ["Transfer-Encoding"] = value;
				else
					headers.Remove (value);
			}
		}

		TaskCompletionSource<HttpContent> bodyTcs;

		Dictionary<string,string> headers = new Dictionary<string, string> ();

		protected HttpMessage ()
		{
		}

		protected HttpMessage (HttpProtocol protocol, NameValueCollection headerCollection)
		{
			Protocol = protocol;

			foreach (string header in headerCollection) {
				headers.Add (header, headerCollection [header]); 
			}
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

		protected async Task ReadHeaders (StreamReader reader, CancellationToken cancellationToken)
		{
			string line;
			while ((line = await reader.ReadLineAsync ()) != null) {
				cancellationToken.ThrowIfCancellationRequested ();
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

		protected async Task WriteHeaders (StreamWriter writer, CancellationToken cancellationToken)
		{
			foreach (var entry in Headers) {
				cancellationToken.ThrowIfCancellationRequested ();
				await writer.WriteAsync (string.Format ("{0}: {1}\r\n", entry.Key, entry.Value));
			}
			cancellationToken.ThrowIfCancellationRequested ();
			await writer.WriteAsync ("\r\n");
		}

		public Task<HttpContent> ReadBody (HttpConnection connection, CancellationToken cancellationToken)
		{
			cancellationToken.ThrowIfCancellationRequested ();
			if (Interlocked.CompareExchange (ref bodyTcs, new TaskCompletionSource<HttpContent> (), null) != null)
				return bodyTcs.Task;

			connection.ReadBody (this, cancellationToken).ContinueWith (t => {
				if (t.IsCompleted)
					bodyTcs.TrySetResult (t.Result);
				else if (t.IsCanceled)
					bodyTcs.TrySetCanceled ();
				else if (t.IsFaulted)
					bodyTcs.TrySetException (t.Exception);
			});

			return bodyTcs.Task;
		}
	}
}

