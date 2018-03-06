//
// HttpClientTestRunner.cs
//
// Author:
//       Martin Baulig <martin.baulig@xamarin.com>
//
// Copyright (c) 2015 Xamarin, Inc.
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
using System.Text;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;
using Xamarin.AsyncTests;
using Xamarin.AsyncTests.Constraints;

namespace Xamarin.WebTests.HttpOperations
{
	using HttpFramework;
	using HttpHandlers;

	public class HttpClientOperation : HttpOperation
	{
		public HttpClientOperation (HttpServer server, Handler handler,
		                            HttpOperationFlags flags = HttpOperationFlags.None,
		                            HttpStatusCode expectedStatus = HttpStatusCode.OK,
		                            WebExceptionStatus expectedError = WebExceptionStatus.Success)
			: base (server, $"{server.ME}:{handler.Value}", handler, flags,
			        expectedStatus, expectedError)
		{
		}

		protected override Request CreateRequest (TestContext ctx, Uri uri)
		{
			return new HttpClientRequest (uri);
		}

		protected override void ConfigureRequest (TestContext ctx, Uri uri, Request request)
		{
			Handler.ConfigureRequest (ctx, request, uri);

			request.SetProxy (Server.GetProxy ());
		}

		public HttpClientHandler HttpClientHandler {
			get {
				var handler = Handler;
				if (handler is AbstractRedirectHandler redirect)
					handler = redirect.Target;
				return (HttpClientHandler)handler;
			}
		}

		protected override async Task<Response> RunInner (TestContext ctx, Request request, CancellationToken cancellationToken)
		{
			var httpClientRequest = (HttpClientRequest)request;

			Response response;

			switch (HttpClientHandler.Type) {
			case HttpClientOperationType.GetString:
				response = await httpClientRequest.GetString (ctx, cancellationToken);
				break;
			case HttpClientOperationType.PostString:
				response = await httpClientRequest.PostString (ctx, cancellationToken);
				break;
			case HttpClientOperationType.PutString:
				response = await httpClientRequest.PutString (ctx, cancellationToken);
				break;
			case HttpClientOperationType.SendAsync:
			case HttpClientOperationType.SendAsyncChunked:
				response = await httpClientRequest.SendAsync (ctx, cancellationToken);
				break;
			case HttpClientOperationType.PutDataAsync:
				response = await httpClientRequest.PutDataAsync (ctx, cancellationToken);
				break;
			case HttpClientOperationType.PostRedirect:
				response = await httpClientRequest.PostString (ctx, cancellationToken);
				break;
			default:
				throw new InvalidOperationException ();
			}

			return response;
		}

		protected override void Destroy ()
		{
		}
	}
}

