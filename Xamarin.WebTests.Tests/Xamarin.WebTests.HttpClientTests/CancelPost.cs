//
// CancelPost.cs
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
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Xamarin.AsyncTests;
using Xamarin.AsyncTests.Constraints;

namespace Xamarin.WebTests.HttpClientTests
{
	using ConnectionFramework;
	using TestFramework;
	using TestAttributes;
	using HttpFramework;
	using HttpHandlers;
	using TestRunners;

	[HttpServerTestCategory (HttpServerTestCategory.RecentlyFixed)]
	public class CancelPost : InstrumentationFixture
	{
		public override HttpOperationFlags OperationFlags => HttpOperationFlags.ServerAbortsHandshake;

		CancellationTokenSource cts;

		protected override bool ConfigureNetworkStream (
			TestContext ctx, StreamInstrumentation instrumentation)
		{
			instrumentation.IgnoreErrors = true;
			return true;
		}

		protected override Task<bool> PrimaryReadHandler (
			TestContext ctx, CustomRequest request, int bytesRead,
			CancellationToken cancellationToken)
		{
			cts.Cancel ();
			return Task.FromResult (false);
		}

		protected override Task<bool> SecondaryReadHandler (
			TestContext ctx, HttpClientTestRunner runner,
			int bytesRead, CancellationToken cancellationToken)
		{
			throw new InvalidOperationException ();
		}

		protected override async Task<Response> Run (
			TestContext ctx, CustomRequest request,
			CancellationToken cancellationToken)
		{
			var data = new byte[6000 * 1024];
			var stream = ctx.RegisterDispose (new MemoryStream (data));
			cts = ctx.RegisterDispose (CancellationTokenSource.CreateLinkedTokenSource (cancellationToken));
			var content = request.Provider.CreateStreamContent (stream);
			content.ContentType = "application/octet-stream";
			await ctx.AssertException<TaskCanceledException> (
				() => request.Client.PostAsync (
					request.RequestUri, content, cts.Token),
				"expecting cancellation").ConfigureAwait (false);
			return new SimpleResponse (request, HttpStatusCode.OK, null);
		}

		public override bool CheckResponse (TestContext ctx, Response response)
		{
			return ctx.Expect (response.Content, Is.Null);
		}
	}
}
