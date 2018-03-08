//
// TraditionalOperation.cs
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

namespace Xamarin.WebTests.HttpOperations
{
	using HttpFramework;
	using HttpHandlers;

	public class TraditionalOperation : HttpOperation
	{
		public TraditionalOperation (HttpServer server, Handler handler, bool sendAsync,
		                             HttpOperationFlags flags = HttpOperationFlags.None,
		                             HttpStatusCode expectedStatus = HttpStatusCode.OK,
		                             WebExceptionStatus expectedError = WebExceptionStatus.Success)
			: base (server, $"{server.ME}:{handler.Value}", handler, flags,
			        expectedStatus, expectedError)
		{
			SendAsync = sendAsync;
		}

		public bool SendAsync {
			get;
		}

		protected override void ConfigureRequest (TestContext ctx, Uri uri, Request request)
		{
			Handler.ConfigureRequest (ctx, request, uri);

			request.SetProxy (Server.GetProxy ());
		}

		protected override Request CreateRequest (TestContext ctx, Uri uri)
		{
			return new TraditionalRequest (uri);
		}

		protected override void Destroy ()
		{
			;
		}

		protected override async Task<Response> RunInner (TestContext ctx, Request request, CancellationToken cancellationToken)
		{
			var traditionalRequest = (TraditionalRequest)request;

			Response response;
			if (SendAsync)
				response = await traditionalRequest.SendAsync (ctx, cancellationToken).ConfigureAwait (false);
			else
				response = await traditionalRequest.Send (ctx, cancellationToken).ConfigureAwait (false);

			return response;
		}
	}
}
