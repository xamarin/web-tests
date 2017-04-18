//
// GetHandler.cs
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

	public class GetHandler : Handler
	{
		public GetHandler (string identifier, HttpContent content)
			: base (identifier)
		{
			Content = content;
		}

		public HttpContent Content {
			get;
		}

		public override object Clone ()
		{
			return new GetHandler (Value, Content);
		}

		internal protected override Task<HttpResponse> HandleRequest (TestContext ctx, HttpConnection connection, HttpRequest request,
		                                                              RequestFlags effectiveFlags, CancellationToken cancellationToken)
		{
			ctx.Assert (request.Method, Is.EqualTo ("GET"), "method");
			return Task.FromResult (new HttpResponse (HttpStatusCode.OK, Content));
		}

		public override bool CheckResponse (TestContext ctx, Response response)
		{
			if (Content == null)
				return ctx.Expect (response.Content, Is.Null, "response.Content");

			if (!ctx.Expect (response.Content, Is.Not.Null, "response.Content"))
				return false;
			if (response.Content.HasLength && Content.HasLength) {
				if (!ctx.Expect (response.Content.Length, Is.EqualTo (Content.Length), "response.Content.Length"))
					return false;
				if (!ctx.Expect (response.Content.AsString (), Is.EqualTo (Content.AsString ()), "response.Content.AsString ()"))
					return false;
			}
			return true;
		}
	}
}

