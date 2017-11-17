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
	using HttpOperations;

	public class HttpClientHandler : Handler
	{
		public HttpClientHandler (
			string identifier, HttpClientOperationType type,
			HttpContent content = null, HttpContent returnContent = null)
			: base (identifier)
		{
			Type = type;
			Content = content;
			ReturnContent = returnContent;
		}

		public HttpClientOperationType Type {
			get;
		}

		public HttpContent Content {
			get;
		}

		public HttpContent ReturnContent {
			get;
		}

		public string ObscureHttpMethod {
			get; set;
		}

		public override object Clone ()
		{
			var handler = new HttpClientHandler (Value, Type, Content, ReturnContent);
			handler.Flags = Flags;
			return handler;
		}

		internal protected override async Task<HttpResponse> HandleRequest (
			TestContext ctx, HttpOperation operation, HttpConnection connection, HttpRequest request,
			RequestFlags effectiveFlags, CancellationToken cancellationToken)
		{
			await CompletedTask.ConfigureAwait (false);

			switch (Type) {
			case HttpClientOperationType.GetString:
				ctx.Assert (request.Method, Is.EqualTo ("GET"), "method");
				return new HttpResponse (HttpStatusCode.OK, ReturnContent);

			case HttpClientOperationType.PostString:
				return HandlePostString (ctx, request, effectiveFlags);

			case HttpClientOperationType.PutString:
				return HandlePutString (ctx, request, effectiveFlags);

			case HttpClientOperationType.SendAsync:
				return HandleSendAsync (ctx, request, effectiveFlags);

			case HttpClientOperationType.SendAsyncChunked:
				return HandleSendAsync (ctx, request, effectiveFlags | RequestFlags.NoContentLength);

			case HttpClientOperationType.PutDataAsync:
				return HandlePutDataAsync (ctx, request, effectiveFlags);

			case HttpClientOperationType.PostRedirect:
				return HandlePostRedirect (ctx, operation, request, effectiveFlags);

			default:
				throw new InvalidOperationException ();
			}
		}

		HttpResponse HandlePostString (TestContext ctx, HttpRequest request, RequestFlags effectiveFlags)
		{
			ctx.Assert (request.Method, Is.EqualTo ("POST"), "method");

			Debug (ctx, 5, "BODY", request.Body);
			if ((effectiveFlags & RequestFlags.NoBody) != 0) {
				ctx.Assert (request.Body, Is.Not.Null, "body");
				return HttpResponse.CreateSuccess ();
			}

			HttpContent.Compare (ctx, request.Body, Content, false);
			return new HttpResponse (HttpStatusCode.OK, ReturnContent);
		}

		HttpResponse HandlePutString (TestContext ctx, HttpRequest request, RequestFlags effectiveFlags)
		{
			ctx.Assert (request.Method, Is.EqualTo ("PUT"), "method");

			Debug (ctx, 5, "BODY", request.Body);
			if ((effectiveFlags & RequestFlags.NoBody) != 0) {
				ctx.Assert (request.Body, Is.Not.Null, "body");
				return HttpResponse.CreateSuccess ();
			}

			HttpContent.Compare (ctx, request.Body, Content, false);
			return new HttpResponse (HttpStatusCode.OK, ReturnContent);
		}

		HttpResponse HandleSendAsync (TestContext ctx, HttpRequest request, RequestFlags effectiveFlags)
		{
			if ((effectiveFlags & RequestFlags.NoContentLength) == 0)
				ctx.Assert (request.ContentLength, Is.Not.Null, "Missing Content-Length");

			Debug (ctx, 5, "BODY", request.Body);
			if ((effectiveFlags & RequestFlags.NoBody) != 0) {
				ctx.Assert (request.Body, Is.Not.Null, "body");
				if (request.ContentLength != null)
					ctx.Assert (request.ContentLength.Value, Is.EqualTo (0), "Zero Content-Length");
				return HttpResponse.CreateSuccess ();
			}

			HttpContent.Compare (ctx, request.Body, Content, false);
			return new HttpResponse (HttpStatusCode.OK, ReturnContent);
		}

		HttpResponse HandlePutDataAsync (TestContext ctx, HttpRequest request, RequestFlags effectiveFlags)
		{
			ctx.Assert (request.Method, Is.EqualTo ("PUT"), "method");

			Debug (ctx, 5, "BODY", request.Body);
			if ((effectiveFlags & RequestFlags.NoBody) != 0) {
				ctx.Assert (request.Body, Is.Not.Null, "body");
				return HttpResponse.CreateSuccess ();
			}

			HttpContent.Compare (ctx, request.Body, Content, false);
			return new HttpResponse (HttpStatusCode.OK, ReturnContent);
		}

		HttpResponse HandlePostRedirect (TestContext ctx, HttpOperation operation, HttpRequest request, RequestFlags effectiveFlags)
		{
			ctx.Assert (request.Method, Is.EqualTo ("POST"), "method");

			var target = new PostHandler (Value, Content) { ReturnContent = ReturnContent };
			var redirect = operation.RegisterRedirect (ctx, target);
			return HttpResponse.CreateRedirect (HttpStatusCode.TemporaryRedirect, redirect);
		}

		public override void ConfigureRequest (Request request, Uri uri)
		{
			base.ConfigureRequest (request, uri);
			switch (Type) {
			case HttpClientOperationType.GetString:
				request.Method = "GET";
				if (Content != null)
					throw new InvalidOperationException ();
				break;
			case HttpClientOperationType.PostString:
				request.Method = "POST";
				if (Content == null)
					throw new InvalidOperationException ();
				request.Content = Content;
				break;
			case HttpClientOperationType.PutString:
				request.Method = "PUT";
				request.Content = Content;
				break;
			case HttpClientOperationType.SendAsync:
				request.Method = "POST";
				request.Content = Content;
				break;
			case HttpClientOperationType.SendAsyncChunked:
				request.Method = "POST";
				request.Content = Content.RemoveTransferEncoding ();
				request.SendChunked ();
				break;
			case HttpClientOperationType.PutDataAsync:
				request.Method = "PUT";
				request.Content = Content;
				break;
			case HttpClientOperationType.PostRedirect:
				request.Method = "POST";
				request.Content = Content;
				break;
			default:
				throw new InvalidOperationException ();
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

