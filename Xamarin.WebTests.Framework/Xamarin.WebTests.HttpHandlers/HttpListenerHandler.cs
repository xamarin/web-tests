//
// HttpListenerHandler.cs
//
// Author:
//       Martin Baulig <mabaul@microsoft.com>
//
// Copyright (c) 2017 Xamarin Inc. (http://www.xamarin.com)
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
using Xamarin.AsyncTests.Portable;
using Xamarin.WebTests.HttpFramework;
using Xamarin.WebTests.TestFramework;
using Xamarin.WebTests.Server;

namespace Xamarin.WebTests.HttpHandlers
{
	public class HttpListenerHandler : Handler
	{
		public HttpListenerTestType Type {
			get;
		}

		public HttpListenerHandler (HttpListenerTestType type) : base (type.ToString ())
		{
			Type = type;
		}

		public override bool CheckResponse (TestContext ctx, Response response)
		{
			return true;
		}

		public override object Clone ()
		{
			return new HttpListenerHandler (Type);
		}

		async Task HandleRequest (TestContext ctx, HttpListenerContext context, CancellationToken cancellationToken)
		{
			await Task.Yield ();
			cancellationToken.ThrowIfCancellationRequested ();

			var portable = DependencyInjector.Get<IPortableSupport> ();

			portable.Close (context.Response.OutputStream);
			portable.Close (context.Response.OutputStream);
			portable.Close (context.Response.OutputStream);
			context.Response.Close ();
		}

		protected internal override async Task<HttpResponse> HandleRequest (
			TestContext ctx, HttpConnection connection, HttpRequest request,
			RequestFlags effectiveFlags, CancellationToken cancellationToken)
		{
			var listenerContext = ((HttpListenerConnection)connection).Context;
			await HandleRequest (ctx, listenerContext, cancellationToken).ConfigureAwait (false);
			return HttpResponse.CreateFrom (listenerContext.Response);
		}

		internal Request CreateRequest (TestContext ctx, HttpServer server, Uri uri)
		{
			return new TraditionalRequest (uri);
		}
	}
}
