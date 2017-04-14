//
// HttpClientHandler.cs
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
using System.Net;
using System.Threading;
using System.Threading.Tasks;

using Xamarin.AsyncTests;
using Xamarin.AsyncTests.Constraints;

namespace Xamarin.WebTests.HttpHandlers
{
	using HttpFramework;

	public class HttpClientHandler : Handler
	{
		public HttpClientHandler (
			string identifier, HttpClientOperation operation,
			HttpContent content = null, HttpContent returnContent = null)
			: base (identifier)
		{
			Operation = operation;
			Content = content;
			ReturnContent = returnContent;
		}

		public HttpClientOperation Operation {
			get;
			private set;
		}

		public HttpContent Content {
			get;
			private set;
		}

		public HttpContent ReturnContent {
			get;
			private set;
		}

		public string ObscureHttpMethod {
			get; set;
		}

		public override object Clone ()
		{
			var handler = new HttpClientHandler (Value, Operation, Content, ReturnContent);
			handler.Flags = Flags;
			return handler;
		}

		protected internal override HttpResponse HandleRequest (TestContext ctx, HttpConnection connection, HttpRequest request, RequestFlags effectiveFlags)
		{
			switch (Operation) {
			case HttpClientOperation.GetString:
				ctx.Assert (request.Method, Is.EqualTo ("GET"), "method");
				return HttpResponse.CreateSuccess (string.Format ("Hello World!"));

			case HttpClientOperation.PostString:
				return HandlePostString (ctx, connection, request, effectiveFlags);

			case HttpClientOperation.PutString:
				return HandlePutString (ctx, connection, request, effectiveFlags);

			case HttpClientOperation.SendAsync:
				return HandleSendAsync (ctx, connection, request, effectiveFlags);

			case HttpClientOperation.PutDataAsync:
				return HandlePutDataAsync (ctx, connection, request, effectiveFlags);

			default:
				throw new InvalidOperationException ();
			}
		}

		HttpResponse HandlePostString (TestContext ctx, HttpConnection connection, HttpRequest request, RequestFlags effectiveFlags)
		{
			ctx.Assert (request.Method, Is.EqualTo ("POST"), "method");

			var body = request.ReadBody ();

			Debug (ctx, 5, "BODY", body);
			if ((effectiveFlags & RequestFlags.NoBody) != 0) {
				ctx.Assert (body, Is.Not.Null, "body");
				return HttpResponse.CreateSuccess ();
			}

			HttpContent.Compare (ctx, body, Content, false);
			return new HttpResponse (HttpStatusCode.OK, ReturnContent);
		}

		HttpResponse HandlePutString (TestContext ctx, HttpConnection connection, HttpRequest request, RequestFlags effectiveFlags)
		{
			ctx.Assert (request.Method, Is.EqualTo ("PUT"), "method");

			var body = request.ReadBody ();

			Debug (ctx, 5, "BODY", body);
			if ((effectiveFlags & RequestFlags.NoBody) != 0) {
				ctx.Assert (body, Is.Not.Null, "body");
				return HttpResponse.CreateSuccess ();
			}

			HttpContent.Compare (ctx, body, Content, false);
			return new HttpResponse (HttpStatusCode.OK, ReturnContent);
		}

		HttpResponse HandleSendAsync (TestContext ctx, HttpConnection connection, HttpRequest request, RequestFlags effectiveFlags)
		{
			var body = request.ReadBody ();

			if ((effectiveFlags & RequestFlags.NoContentLength) == 0)
				ctx.Assert (request.ContentLength, Is.Not.Null, "Missing Content-Length");

			Debug (ctx, 5, "BODY", body);
			if ((effectiveFlags & RequestFlags.NoBody) != 0) {
				ctx.Assert (body, Is.Not.Null, "body");
				if (request.ContentLength != null)
					ctx.Assert (request.ContentLength.Value, Is.EqualTo (0), "Zero Content-Length");
				return HttpResponse.CreateSuccess ();
			}

			HttpContent.Compare (ctx, body, Content, false);
			return new HttpResponse (HttpStatusCode.OK, ReturnContent);
		}

		HttpResponse HandlePutDataAsync (TestContext ctx, HttpConnection connection, HttpRequest request, RequestFlags effectiveFlags)
		{
			ctx.Assert (request.Method, Is.EqualTo ("PUT"), "method");

			var body = request.ReadBody ();

			Debug (ctx, 5, "BODY", body);
			if ((effectiveFlags & RequestFlags.NoBody) != 0) {
				ctx.Assert (body, Is.Not.Null, "body");
				return HttpResponse.CreateSuccess ();
			}

			HttpContent.Compare (ctx, body, Content, false);
			return new HttpResponse (HttpStatusCode.OK, ReturnContent);
		}

		public override void ConfigureRequest (Request request, Uri uri)
		{
			base.ConfigureRequest (request, uri);
			switch (Operation) {
			case HttpClientOperation.GetString:
				request.Method = "GET";
				if (Content != null)
					throw new InvalidOperationException ();
				break;
			case HttpClientOperation.PostString:
				request.Method = "POST";
				if (Content == null)
					throw new InvalidOperationException ();
				request.Content = Content;
				break;
			case HttpClientOperation.PutString:
				request.Method = "PUT";
				request.Content = Content;
				break;
			case HttpClientOperation.SendAsync:
				request.Method = "POST";
				request.Content = Content;
				break;
			case HttpClientOperation.PutDataAsync:
				request.Method = "PUT";
				request.Content = Content;
				break;
			default:
				throw new InvalidOperationException ();
			}
		}
	}
}

