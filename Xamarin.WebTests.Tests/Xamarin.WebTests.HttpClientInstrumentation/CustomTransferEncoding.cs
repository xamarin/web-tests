﻿//
// CustomTransferEncoding.cs
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

namespace Xamarin.WebTests.HttpClientInstrumentation
{
	using TestFramework;
	using TestAttributes;
	using HttpFramework;
	using HttpClient;
	using HttpHandlers;
	using TestRunners;


	[RecentlyFixed] // Mono PR #9212.
	public class CustomTransferEncoding : HttpClientInstrumentationFixture
	{
		public override HttpOperationFlags OperationFlags => HttpOperationFlags.ExpectClientException;

		protected override void ConfigureRequest (TestContext ctx, InstrumentationOperation operation, CustomRequest request)
		{
			request.Client.DefaultRequestHeaders.AddTransferEncoding ("chunked2");
			base.ConfigureRequest (ctx, operation, request);
		}

		protected override async Task<Response> Run (TestContext ctx, CustomRequest request, CancellationToken cancellationToken)
		{
			var message = request.Handler.CreateRequestMessage (HttpMethod.Get, request.RequestUri);
			await ctx.AssertException<InvalidOperationException> (SendAsync, "Expect exception").ConfigureAwait (false);
			return null;

			Task<IHttpResponseMessage> SendAsync () => request.Client.SendAsync (message, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
		}
	}
}
