//
// DeleteHandler.cs
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
	using HttpFramework;

	public class DeleteHandler : Handler
	{
		public DeleteHandler (string identifier, string body = null)
			: base (identifier)
		{
			Body = body;
		}

		public string Body {
			get;
			private set;
		}

		public override object Clone ()
		{
			return new DeleteHandler (Value, Body) { Flags = Flags };
		}

		internal protected override async Task<HttpResponse> HandleRequest (
			TestContext ctx, HttpConnection connection, HttpRequest request,
			RequestFlags effectiveFlags, CancellationToken cancellationToken)
		{
			if (!request.Method.Equals ("DELETE"))
				return HttpResponse.CreateError ("Wrong method: {0}", request.Method);

			var hasExplicitLength = (Flags & RequestFlags.ExplicitlySetLength) != 0;

			if (request.ContentLength != null) {
				if (Body != null) {
					await request.ReadBody (connection, cancellationToken);
					return HttpResponse.CreateSuccess ();
				} else if (hasExplicitLength) {
					if (request.ContentLength.Value != 0)
						return HttpResponse.CreateError ("Content-Length must be zero");
				} else {
					return HttpResponse.CreateError ("Content-Length not allowed.");
				}
			} else if (hasExplicitLength || Body != null) {
				return HttpResponse.CreateError ("Missing Content-Length");
			}

			return HttpResponse.CreateSuccess ();
		}

		public override void ConfigureRequest (Request request, Uri uri)
		{
			base.ConfigureRequest (request, uri);
			request.Method = "DELETE";
			request.Content = StringContent.CreateMaybeNull (Body);

			if (Flags == RequestFlags.ExplicitlySetLength)
				request.SetContentLength (Body != null ? Body.Length : 0);
		}

		public override bool CheckResponse (TestContext ctx, Response response)
		{
			return ctx.Expect (response.Content, Is.Null, "response.Content");
		}
	}
}
