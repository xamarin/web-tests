//
// TestFork.cs
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
using System.Threading;
using System.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;

using Xamarin.AsyncTests;
using Xamarin.AsyncTests.Constraints;

namespace Xamarin.WebTests.Tests
{
	using Handlers;
	using Framework;
	using Portable;

	[Heavy]
	[AsyncTestFixture (Timeout = 5000)]
	public class TestFork : ITestHost<HttpServer>, ITestParameterSource<Handler>
	{
		public HttpServer CreateInstance (TestContext ctx)
		{
			return new HttpServer (PortableSupport.Web.GetLoopbackEndpoint (9999), true, false);
		}

		HttpContent CreateRandomContent (TestContext ctx)
		{
			return new RandomContent (2 << 15, 2 << 22);
		}

		public IEnumerable<Handler> GetParameters (TestContext ctx, string filter)
		{
			yield return new PostHandler {
				Mode = TransferMode.Chunked, Content = CreateRandomContent (ctx),
				AllowWriteStreamBuffering = false, Description = "Large chunked post"
			};
		}

		const string PostPuppy = "http://localhost/~martin/work/bugfree-octo-nemesis/www/cgi-bin/post-puppy.pl";

		[AsyncTest]
		public async Task<bool> Run (TestContext ctx, [TestHost] HttpServer server,
			[Fork (5, RandomDelay = 1500)] IFork fork, [Repeat (50)] int repeat,
			[TestParameter] Handler handler, CancellationToken cancellationToken)
		{
			ctx.LogMessage ("FORK START: {0} {1}", fork.ID, ctx.PortableSupport.CurrentThreadId);

			var ok = await TestRunner.RunTraditional (ctx, server, handler, cancellationToken);

			ctx.LogMessage ("FORK DONE: {0} {1} {2}", fork.ID, ctx.PortableSupport.CurrentThreadId, ok);

			return ok;
		}

		[Martin]
		[AsyncTest]
		public async Task<bool> RunPuppy (TestContext ctx, [TestHost] HttpServer server,
			[Fork (5, RandomDelay = 1500)] IFork fork, [Repeat (50)] int repeat,
			[TestParameter] Handler handler, CancellationToken cancellationToken)
		{
			ctx.LogMessage ("FORK START: {0} {1}", fork.ID, ctx.PortableSupport.CurrentThreadId);

			var uri = new Uri (PostPuppy);

			var request = new TraditionalRequest (uri);
			handler.ConfigureRequest (request, request.Request.RequestUri);

			var response = await request.Send (ctx, cancellationToken);

			bool ok;
			if (response.Error != null) {
				if (response.Content != null)
					ctx.OnError (new WebException (response.Content.AsString (), response.Error));
				else
					ctx.OnError (response.Error);
				ok = false;
			} else {
				ok = ctx.Expect (HttpStatusCode.OK, Is.EqualTo (response.Status), "status code");
				if (ok)
					ok &= ctx.Expect (response.IsSuccess, Is.True, "success status");
			}

			ctx.LogMessage ("FORK DONE: {0} {1} {2}", fork.ID, ctx.PortableSupport.CurrentThreadId, ok);

			return ok;
		}
	}
}
