//
// ServerAbortsPost.cs
//
// Author:
//       Martin Baulig <mabaul@microsoft.com>
//
// Copyright (c) 2018 Xamarin Inc. (http://www.xamarin.com)
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

namespace Xamarin.WebTests.HttpRequestTests
{
	using TestFramework;
	using HttpFramework;
	using HttpHandlers;

	public class ServerAbortsPost : CustomHandlerFixture
	{
		public override HttpServerTestCategory Category => HttpServerTestCategory.NewWebStack;

		public override HttpStatusCode ExpectedStatus => HttpStatusCode.BadRequest;

		public override WebExceptionStatus ExpectedError => WebExceptionStatus.ProtocolError;

		public override HttpOperationFlags OperationFlags => HttpOperationFlags.ServerAbortsRedirection;

		public override bool HasRequestBody => false;

		public override HttpContent ExpectedContent => throw new InvalidOperationException ();

		public override bool CloseConnection => true;

		protected override void ConfigureRequest (
			TestContext ctx, Uri uri, CustomHandler handler,
			TraditionalRequest request)
		{
			request.Method = "POST";
			request.SetContentType ("application/x-www-form-urlencoded");
			request.Content = new FormContent (("foo", "bar"), ("hello", "world"), ("escape", "this needs % escaping"));
			base.ConfigureRequest (ctx, uri, handler, request);
		}

		protected override Task<Response> SendRequest (
			TestContext ctx, TraditionalRequest request,
			CancellationToken cancellationToken)
		{
			return request.Send (ctx, cancellationToken);
		}

		public override HttpResponse HandleRequest (
			TestContext ctx, HttpOperation operation,
			HttpRequest request, CustomHandler handler)
		{
			ctx.Assert (request.Method, Is.EqualTo ("POST"), "method");
			return new HttpResponse (HttpStatusCode.BadRequest, null);
		}
	}
}
