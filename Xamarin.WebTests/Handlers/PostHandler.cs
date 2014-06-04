//
// SimplePostHandler.cs
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
using System.Globalization;
using System.Collections.Generic;

namespace Xamarin.WebTests.Handlers
{
	using Framework;
	using Server;

	public class PostHandler : Handler
	{
		string body;
		int? readChunkSize;
		int? readChunkMinDelay, readChunkMaxDelay;
		int? writeChunkSize;
		int? writeChunkMinDelay, writeChunkMaxDelay;
		bool? allowWriteBuffering;
		TransferMode mode = TransferMode.Default;

		public TransferMode Mode {
			get { return mode; }
			set {
				WantToModify ();
				mode = value;
			}
		}

		public string Body {
			get {
				return body;
			}
			set {
				WantToModify ();
				body = value;
			}
		}

		public int? ReadChunkSize {
			get {
				return readChunkSize;
			}
			set {
				WantToModify ();
				readChunkSize = value;
			}
		}

		public int? ReadChunkMinDelay {
			get {
				return readChunkMinDelay;
			}
			set {
				WantToModify ();
				readChunkMinDelay = value;
			}
		}

		public int? ReadChunkMaxDelay {
			get {
				return readChunkMaxDelay;
			}
			set {
				WantToModify ();
				readChunkMaxDelay = value;
			}
		}

		public bool? AllowWriteStreamBuffering {
			get {
				return allowWriteBuffering;
			}
			set {
				WantToModify ();
				allowWriteBuffering = value;
			}
		}

		public int? WriteChunkSize {
			get {
				return writeChunkSize;
			}
			set {
				WantToModify ();
				writeChunkSize = value;
			}
		}

		public int? WriteChunkMinDelay {
			get {
				return writeChunkMinDelay;
			}
			set {
				WantToModify ();
				writeChunkMinDelay = value;
			}
		}

		public int? WriteChunkMaxDelay {
			get {
				return writeChunkMaxDelay;
			}
			set {
				WantToModify ();
				writeChunkMaxDelay = value;
			}
		}

		protected internal override HttpResponse HandleRequest (HttpConnection connection, HttpRequest request, RequestFlags effectiveFlags)
		{
			Debug (0, "HANDLE POST", request.Path, request.Method, effectiveFlags);

			if (request.Headers.ContainsKey ("X-Mono-Redirected"))
				effectiveFlags |= RequestFlags.Redirected;

			if ((effectiveFlags & RequestFlags.RedirectedAsGet) != 0) {
				if (!request.Method.Equals ("GET"))
					return HttpResponse.CreateError ("Wrong method: {0}", request.Method);
			} else {
				if (!request.Method.Equals ("POST") && !request.Method.Equals ("PUT"))
					return HttpResponse.CreateError ("Wrong method: {0}", request.Method);
			}

			var haveContentLength = request.Headers.ContainsKey ("Content-Length");
			var haveTransferEncoding = request.Headers.ContainsKey ("Transfer-Encoding");

			if ((effectiveFlags & RequestFlags.RedirectedAsGet) != 0) {
				if (haveContentLength)
					return HttpResponse.CreateError ("Content-Length header not allowed");

				if (haveTransferEncoding)
					return HttpResponse.CreateError ("Transfer-Encoding header not allowed");

				return HttpResponse.CreateSuccess ();
			}

			string body;

			switch (Mode) {
			case TransferMode.Default:
				if (Body != null) {
					if (!haveContentLength)
						return HttpResponse.CreateError ("Missing Content-Length");

					body = ReadBody (connection, request, effectiveFlags);
					break;
				} else {
					if (haveContentLength)
						return HttpResponse.CreateError ("Content-Length header not allowed");

					return HttpResponse.CreateSuccess ();
				}

			case TransferMode.ContentLength:
				if (!haveContentLength)
					return HttpResponse.CreateError ("Missing Content-Length");

				if (haveTransferEncoding)
					return HttpResponse.CreateError ("Transfer-Encoding header not allowed");

				body = ReadBody (connection, request, effectiveFlags);
				break;

			case TransferMode.Chunked:
				if ((effectiveFlags & RequestFlags.Redirected) != 0)
					goto case TransferMode.ContentLength;

				if (haveContentLength)
					return HttpResponse.CreateError ("Content-Length header not allowed");

				if (!haveTransferEncoding)
					return HttpResponse.CreateError ("Missing Transfer-Encoding header");

				var transferEncoding = request.Headers ["Transfer-Encoding"];
				if (!string.Equals (transferEncoding, "chunked", StringComparison.InvariantCultureIgnoreCase))
					return HttpResponse.CreateError ("Invalid Transfer-Encoding header: '{0}'", transferEncoding);

				body = ReadBody (connection, request, effectiveFlags);
				break;

			default:
				return HttpResponse.CreateError ("Unknown TransferMode: '{0}'", Mode);
			}

			Debug (0, "BODY", body);
			if ((effectiveFlags & RequestFlags.NoBody) != 0) {
				if (!string.IsNullOrEmpty (body))
					return HttpResponse.CreateError ("Must not send a body with this request.");
				return HttpResponse.CreateSuccess ();
			}

			if (Body != null && !Body.Equals (body))
				return HttpResponse.CreateError ("Invalid body");

			return HttpResponse.CreateSuccess ();
		}

		string ReadBody (Connection connection, HttpRequest request, RequestFlags effectiveFlags)
		{
			connection.ReadChunkSize = ReadChunkSize;
			connection.ReadChunkMinDelay = ReadChunkMinDelay;
			connection.ReadChunkMaxDelay = ReadChunkMaxDelay;
			var body = request.ReadBody ();
			if (body == null)
				throw new InvalidOperationException ();
			return body;
		}

		public override HttpWebRequest CreateRequest (Uri uri)
		{
			var request = base.CreateRequest (uri);

			if (Body != null)
				request.ContentType = "text/plain";
			request.Method = "POST";

			if (AllowWriteStreamBuffering != null)
				request.AllowWriteStreamBuffering = AllowWriteStreamBuffering.Value;

			if (((Flags & RequestFlags.ExplicitlySetLength) != 0) && (Mode != TransferMode.ContentLength))
				throw new InvalidOperationException ();

			switch (Mode) {
			case TransferMode.Chunked:
				request.SendChunked = true;
				break;
			case TransferMode.ContentLength:
				if (body == null)
					request.ContentLength = 0;
				else
					request.ContentLength = body.Length;
				break;
			}

			return request;
		}

		public override void SendRequest (HttpWebRequest request)
		{
			base.SendRequest (request);

			if (Body != null) {
				using (var stream = request.GetRequestStream ()) {
					if (!string.IsNullOrEmpty (Body))
						WriteBody (stream, Body);
				}
			}
		}

		void WriteBody (Stream writer, string body)
		{
			var buffer = new UTF8Encoding ().GetBytes (body);

			var length = buffer.Length;
			var chunkSize = WriteChunkSize ?? length;
			var minDelay = WriteChunkMinDelay ?? 0;
			var maxDelay = WriteChunkMaxDelay ?? minDelay;

			var random = new Random ();
			var delayRange = maxDelay - minDelay;

			int offset = 0;
			while (offset < length) {
				int delay = minDelay + random.Next (delayRange);
				Thread.Sleep (delay);

				var size = Math.Min (length - offset, chunkSize);
				writer.Write (buffer, offset, size);
				offset += size;
			}
		}
	}
}

