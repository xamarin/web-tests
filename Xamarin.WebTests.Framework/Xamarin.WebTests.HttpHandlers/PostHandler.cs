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
using System.Threading.Tasks;
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

		public HttpContent ReturnContent {
			get; set;
		}

		public string Method {
			get {
				return method;
			}
			set {
				method = value;
			}
		}

		public bool? AllowWriteStreamBuffering {
			get {
				return allowWriteBuffering;
			}
			set {
				allowWriteBuffering = value;
			}
		}

		public Func<HttpRequest, HttpResponse> CustomHandler {
			set {
				customHandler = value;
			}
		}

		public override object Clone ()
		{
			var post = new PostHandler (Value, Content, Mode);
			post.Flags = Flags;
			post.allowWriteBuffering = allowWriteBuffering;
			post.customHandler = customHandler;
			post.ReturnContent = ReturnContent;
			return post;
		}

		internal protected override async Task<HttpResponse> HandleRequest (
			TestContext ctx, HttpOperation operation, HttpConnection connection, HttpRequest request,
			RequestFlags effectiveFlags, CancellationToken cancellationToken)
		{
			await CompletedTask.ConfigureAwait (false);

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

			if ((effectiveFlags & RequestFlags.RedirectedAsGet) != 0) {
				ctx.Expect (request.ContentLength, Is.Null, "Content-Length header not allowed");
				ctx.Expect (request.TransferEncoding, Is.Null, "Transfer-Encoding header not allowed");
				return HttpResponse.CreateSuccess ();
			}

			if ((effectiveFlags & RequestFlags.NoContentLength) != 0) {
				ctx.Expect (request.ContentLength, Is.Null, "Content-Length header not allowed");
				ctx.Expect (request.TransferEncoding, Is.Null, "Transfer-Encoding header not allowed");
			}

			Debug (ctx, 2, "HANDLE POST #1", request.ContentLength, request.TransferEncoding);

			switch (Mode) {
			case TransferMode.Default:
				if (Content != null) {
					ctx.Expect (request.ContentLength, Is.Not.Null, "Missing Content-Length");
					break;
				} else {
					ctx.Expect (request.ContentLength, Is.Null, "Content-Length header not allowed");
					return HttpResponse.CreateSuccess ();
				}

			case TransferMode.ContentLength:
				ctx.Expect (request.ContentLength, Is.Not.Null, "Missing Content-Length");
				ctx.Expect (request.TransferEncoding, Is.Null, "Transfer-Encoding header not allowed");
				break;

			case TransferMode.Chunked:
				if ((effectiveFlags & RequestFlags.Redirected) != 0)
					goto case TransferMode.ContentLength;

				ctx.Expect (request.ContentLength, Is.Null, "Content-Length header not allowed");
				var ok = ctx.Expect (request.TransferEncoding, Is.Not.Null, "Missing Transfer-Encoding header");
				if (!ok)
					break;

				ok &= ctx.Expect (request.TransferEncoding.ToLowerInvariant (), Is.EqualTo ("chunked"), "Invalid Transfer-Encoding");
				break;

			default:
				ctx.Expect (false, "Unknown TransferMode: '{0}'", Mode);
				return null;
			}

			Debug (ctx, 5, "BODY", request.Body);
			if ((effectiveFlags & RequestFlags.NoBody) != 0) {
				ctx.Expect (HttpContent.IsNullOrEmpty (request.Body), "Must not send a body with this request.");
				return null;
			}

			if (Content != null)
				HttpContent.Compare (ctx, request.Body, Content, true);
			else
				ctx.Expect (HttpContent.IsNullOrEmpty (request.Body), "null or empty content");

			if (ReturnContent != null)
				return new HttpResponse (HttpStatusCode.OK, ReturnContent);

			return null;
		}

		public override void ConfigureRequest (TestContext ctx, Request request, Uri uri)
		{
			base.ConfigureRequest (ctx, request, uri);
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

		public override bool CheckResponse (TestContext ctx, Response response)
		{
			if (ReturnContent == null)
				return ctx.Expect (response.Content, Is.Null, "response.Content");

			if (!ctx.Expect (response.Content, Is.Not.Null, "response.Content"))
				return false;
			if (response.Content.HasLength && ReturnContent.HasLength) {
				if (!ctx.Expect (response.Content.Length, Is.EqualTo (ReturnContent.Length), "response.Content.Length"))
					return false;
				if (!ctx.Expect (response.Content.AsString (), Is.EqualTo (ReturnContent.AsString ()), "response.Content.AsString()"))
					return false;
			}

			return true;
		}
	}
}

