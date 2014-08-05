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

using Xamarin.AsyncTests;

namespace Xamarin.WebTests.Handlers
{
	using Framework;
	using Portable;

	public class PostHandler : Handler
	{
		HttpContent content;
		bool? allowWriteBuffering;
		TransferMode mode = TransferMode.Default;

		public TransferMode Mode {
			get { return mode; }
			set {
				WantToModify ();
				mode = value;
			}
		}

		public HttpContent Content {
			get {
				return content;
			}
			set {
				WantToModify ();
				content = value;
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
			post.content = content;
			post.allowWriteBuffering = allowWriteBuffering;
			post.mode = mode;
			return post;
		}

		protected internal override HttpResponse HandleRequest (TestContext ctx, HttpConnection connection, HttpRequest request, RequestFlags effectiveFlags)
		{
			Debug (ctx, 2, "HANDLE POST", request.Path, request.Method, effectiveFlags);

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

			var content = request.ReadBody ();

			switch (Mode) {
			case TransferMode.Default:
				if (Content != null) {
					if (!haveContentLength)
						return HttpResponse.CreateError ("Missing Content-Length");

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

				break;

			case TransferMode.Chunked:
				if ((effectiveFlags & RequestFlags.Redirected) != 0)
					goto case TransferMode.ContentLength;

				if (haveContentLength)
					return HttpResponse.CreateError ("Content-Length header not allowed");

				if (!haveTransferEncoding)
					return HttpResponse.CreateError ("Missing Transfer-Encoding header");

				var transferEncoding = request.Headers ["Transfer-Encoding"];
				if (!string.Equals (transferEncoding, "chunked", StringComparison.OrdinalIgnoreCase))
					return HttpResponse.CreateError ("Invalid Transfer-Encoding header: '{0}'", transferEncoding);

				break;

			default:
				return HttpResponse.CreateError ("Unknown TransferMode: '{0}'", Mode);
			}

			Debug (ctx, 5, "BODY", content);
			if ((effectiveFlags & RequestFlags.NoBody) != 0) {
				ctx.Assert (HttpContent.IsNullOrEmpty (content), "Must not send a body with this request.");
				return HttpResponse.CreateSuccess ();
			}

			if (Content != null)
				HttpContent.Compare (ctx, content, Content, true, true);
			else
				ctx.Assert (HttpContent.IsNullOrEmpty (content));

			return HttpResponse.CreateSuccess ();
		}

		public override void ConfigureRequest (Request request, Uri uri)
		{
			base.ConfigureRequest (request, uri);
			request.Method = "POST";

			if (AllowWriteStreamBuffering != null)
				PortableSupport.Web.SetAllowWriteStreamBuffering (((TraditionalRequest)request).Request, AllowWriteStreamBuffering.Value);

			if (Content != null)
				request.SetContentType ("text/plain");

			if (((Flags & RequestFlags.ExplicitlySetLength) != 0) && (Mode != TransferMode.ContentLength))
				throw new InvalidOperationException ();

			var effectiveMode = Mode;
			if ((Flags & RequestFlags.Redirected) != 0) {
				if (effectiveMode == TransferMode.Chunked)
					effectiveMode = TransferMode.ContentLength;
			}

			if (Content != null)
				request.Content = new StringContent (Content.AsString ());

			switch (effectiveMode) {
			case TransferMode.Chunked:
				request.SendChunked ();
				break;
			case TransferMode.ContentLength:
				if (Content == null)
					request.SetContentLength (0);
				else
					request.SetContentLength (request.Content.Length);
				break;
			}
		}
	}
}

