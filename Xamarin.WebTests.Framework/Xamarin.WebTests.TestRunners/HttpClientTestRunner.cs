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

namespace Xamarin.WebTests.TestRunners
{
	using HttpFramework;
	using HttpHandlers;

	public class HttpClientTestRunner : TestRunner
	{
		public HttpClientTestRunner (HttpServer server, HttpClientHandler handler, RedirectHandler redirect = null)
			: base (server, handler, redirect)
		{
		}

		protected override Request CreateRequest (TestContext ctx, Uri uri)
		{
			return new HttpClientRequest ((HttpClientHandler)Handler, uri);
		}

		protected override async Task<Response> RunInner (TestContext ctx, Request request, CancellationToken cancellationToken)
		{
			var httpClientHandler = (HttpClientHandler)Handler;
			var httpClientRequest = (HttpClientRequest)request;

			Response response;

			switch (httpClientHandler.Operation) {
			case HttpClientOperation.GetString:
				response = await httpClientRequest.GetString (ctx, cancellationToken);
				break;
			case HttpClientOperation.PostString:
				response = await httpClientRequest.PostString (ctx, httpClientHandler.ReturnContent, cancellationToken);
				break;
			case HttpClientOperation.PutString:
				response = await httpClientRequest.PutString (ctx, cancellationToken);
				break;
			case HttpClientOperation.SendAsync:
				response = await httpClientRequest.SendAsync (ctx, cancellationToken);
				break;
			case HttpClientOperation.PutDataAsync:
				response = await httpClientRequest.PutDataAsync (ctx, cancellationToken);
				break;
			default:
				throw new InvalidOperationException ();
			}

			return response;
		}
	}
}

