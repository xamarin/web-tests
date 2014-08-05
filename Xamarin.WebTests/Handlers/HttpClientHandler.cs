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
using Http = System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

using Xamarin.AsyncTests;
using Xamarin.AsyncTests.Constraints;

namespace Xamarin.WebTests.Handlers
{
	using Framework;

	public class HttpClientHandler : Handler
	{
		HttpClientOperation operation;
		HttpContent content;
		HttpContent returnContent;

		public HttpClientOperation Operation {
			get { return operation; }
			set {
				WantToModify ();
				operation = value;
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

		public HttpContent ReturnContent {
			get {
				return returnContent;
			}
			set {
				WantToModify ();
				returnContent = value;
			}
		}

		public override object Clone ()
		{
			var handler = new HttpClientHandler ();
			handler.operation = operation;
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

			default:
				throw new InvalidOperationException ();
			}
		}

		HttpResponse HandlePostString (TestContext ctx, HttpConnection connection, HttpRequest request, RequestFlags effectiveFlags)
		{
			var body = request.ReadBody ();

			Debug (ctx, 5, "BODY", body);
			if ((effectiveFlags & RequestFlags.NoBody) != 0) {
				ctx.Assert (body, Is.Not.Null, "body");
				return HttpResponse.CreateSuccess ();
			}

			HttpContent.Compare (ctx, body, Content, false, true);
			return new HttpResponse (HttpStatusCode.OK, returnContent);
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
			default:
				throw new InvalidOperationException ();
			}
		}
	}
}

