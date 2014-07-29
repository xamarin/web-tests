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

		public bool? AllowWriteStreamBuffering {
			get {
				return allowWriteBuffering;
			}
			set {
				WantToModify ();
				allowWriteBuffering = value;
			}
		}

		public override object Clone ()
		{
			var post = new PostHandler ();
			post.body = body;
			post.allowWriteBuffering = allowWriteBuffering;
			post.mode = mode;
			return post;
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
			var body = request.ReadBody ();
			if (body == null)
				throw new InvalidOperationException ();
			return body.AsString ();
		}

		public override Request CreateRequest (Uri uri)
		{
			var traditional = new TraditionalRequest (uri);

			if (Body != null)
				traditional.Request.ContentType = "text/plain";
			traditional.Request.Method = "POST";

			if (AllowWriteStreamBuffering != null)
				traditional.Request.AllowWriteStreamBuffering = AllowWriteStreamBuffering.Value;

			if (((Flags & RequestFlags.ExplicitlySetLength) != 0) && (Mode != TransferMode.ContentLength))
				throw new InvalidOperationException ();

			switch (Mode) {
			case TransferMode.Chunked:
				traditional.Request.SendChunked = true;
				break;
			case TransferMode.ContentLength:
				if (body == null)
					traditional.Request.ContentLength = 0;
				else
					traditional.Request.ContentLength = body.Length;
				break;
			}

			traditional.Content = StringContent.CreateMaybeNull (Body);

			return traditional;
		}
	}
}

