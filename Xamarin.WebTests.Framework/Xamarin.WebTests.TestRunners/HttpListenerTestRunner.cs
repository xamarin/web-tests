//
// HttpListenerTestRunner.cs
//
// Author:
//       Martin Baulig <mabaul@microsoft.com>
//
// Copyright (c) 2017 Xamarin, Inc.
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

	public class HttpListenerTestRunner : TestRunner
	{
		new public HttpListenerHandler Handler {
			get { return (HttpListenerHandler)base.Handler; }
		}

		public HttpListenerTestRunner (HttpServer server, HttpListenerHandler handler)
			: base (server, handler)
		{
		}

		protected override Request CreateRequest (TestContext ctx, Uri uri)
		{
			return Handler.CreateRequest (ctx, Server, uri);
		}

		protected override Task<Response> RunInner (TestContext ctx, Request request, CancellationToken cancellationToken)
		{
			// ctx.Assert (Server.IsHttpListener, "HttpServer.IsHttpListener");
			return request.SendAsync (ctx, cancellationToken);
		}
	}
}
