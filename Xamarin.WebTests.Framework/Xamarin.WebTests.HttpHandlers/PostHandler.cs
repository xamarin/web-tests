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
using Xamarin.AsyncTests.Constraints;

namespace Xamarin.WebTests.HttpHandlers
{
	using ConnectionFramework;
	using HttpFramework;

	public class PostHandler : Handler
	{
		string method;
		bool? allowWriteBuffering;
		Func<HttpRequest, HttpResponse> customHandler;

		public PostHandler (string identifier, HttpContent content = null, TransferMode mode = TransferMode.Default)
			: base (identifier)
		{
			Mode = mode;
			Content = content;
		}

		public TransferMode Mode {
			get;
			private set;
		}

		public HttpContent Content {
			get;
			private set;
		}

		public string Method {
			get {
				return method;
			}
			set {
				WantToModify ();
				method = value;
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

		public Func<HttpRequest, HttpResponse> CustomHandler {
			set {
				WantToModify ();
				customHandler = value;
			}
		}

		public override object Clone ()
		{
			var post = new PostHandler (Value, Content, Mode);
			post.Flags = Flags;
			post.allowWriteBuffering = allowWriteBuffering;
			post.customHandler = customHandler;
			return post;
		}

		protected internal override HttpResponse HandleRequest (TestContext ctx, HttpConnection connection, HttpRequest request, RequestFlags effectiveFlags)
		{
			Debug (ctx, 2, "HANDLE POST", request.Path, request.Method, effectiveFlags);

			if (request.Headers.ContainsKey ("X-Mono-Redirected"))
				effectiveFlags |= RequestFlags.Redirected;

			if ((effectiveFlags & RequestFlags.RedirectedAsGet) != 0) {
				if (!ctx.Expect (request.Method, Is.EqualTo ("GET"), "method"))
					return null;
			} else if (Method != null) {
				if (!ctx.Expect (request.Method, Is.EqualTo (Method), "method"))
					return null;
			} else {
				if (!ctx.Expect (request.Method, Is.EqualTo ("POST").Or.EqualTo ("PUT"), "method"))
					return null;
			}

			if (customHandler != null) {
				var customResponse = customHandler (request);
				if (customResponse != null)
					return customResponse;
			}

			var hasContentLength = request.Headers.ContainsKey ("Content-Length");
			var hasTransferEncoding = request.Headers.ContainsKey ("Transfer-Encoding");

			if ((effectiveFlags & RequestFlags.RedirectedAsGet) != 0) {
				ctx.Expect (hasContentLength, Is.False, "Content-Length header not allowed");
				ctx.Expect (hasTransferEncoding, Is.False, "Transfer-Encoding header not allowed");
				return HttpResponse.CreateSuccess ();
			}

			if ((effectiveFlags & RequestFlags.NoContentLength) != 0) {
				ctx.Expect (hasContentLength, Is.False, "Content-Length header not allowed");
				ctx.Expect (hasTransferEncoding, Is.False, "Transfer-Encoding header not allowed");
			}

			Debug (ctx, 2, "HANDLE POST #1", hasContentLength, hasTransferEncoding);

			var content = request.ReadBody ();

			switch (Mode) {
			case TransferMode.Default:
				if (Content != null) {
					ctx.Expect (hasContentLength, Is.True, "Missing Content-Length");
					break;
				} else {
					ctx.Expect (hasContentLength, Is.False, "Content-Length header not allowed");
					return HttpResponse.CreateSuccess ();
				}

			case TransferMode.ContentLength:
				ctx.Expect (hasContentLength, Is.True, "Missing Content-Length");
				ctx.Expect (hasTransferEncoding, Is.False, "Transfer-Encoding header not allowed");
				break;

			case TransferMode.Chunked:
				if ((effectiveFlags & RequestFlags.Redirected) != 0)
					goto case TransferMode.ContentLength;

				ctx.Expect (hasContentLength, Is.False, "Content-Length header not allowed");
				var ok = ctx.Expect (hasTransferEncoding, Is.True, "Missing Transfer-Encoding header");
				if (!ok)
					break;

				var transferEncoding = request.Headers ["Transfer-Encoding"];
				ok &= ctx.Expect (transferEncoding.ToLowerInvariant (), Is.EqualTo ("chunked"), "Invalid Transfer-Encoding");
				break;

			default:
				ctx.Expect (false, "Unknown TransferMode: '{0}'", Mode);
				return null;
			}

			Debug (ctx, 5, "BODY", content);
			if ((effectiveFlags & RequestFlags.NoBody) != 0) {
				ctx.Expect (HttpContent.IsNullOrEmpty (content), "Must not send a body with this request.");
				return null;
			}

			if (Content != null)
				HttpContent.Compare (ctx, content, Content, true);
			else
				ctx.Expect (HttpContent.IsNullOrEmpty (content), "null or empty content");

			return null;
		}

		public override void ConfigureRequest (Request request, Uri uri)
		{
			base.ConfigureRequest (request, uri);
			request.Method = Method ?? "POST";

			if (AllowWriteStreamBuffering != null)
				((TraditionalRequest)request).RequestExt.SetAllowWriteStreamBuffering (AllowWriteStreamBuffering.Value);

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
				request.Content = Content.RemoveTransferEncoding ();

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

