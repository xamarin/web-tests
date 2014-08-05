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

		static Random random = new Random ();

		HttpContent CreateRandomContent ()
		{
			var size = 13 + random.Next (1048576);
			var bytes = new byte [size];
			random.NextBytes (bytes);
			var text = Convert.ToBase64String (bytes);
			return new ChunkedContent (text);
		}

		public IEnumerable<Handler> GetParameters (TestContext ctx, string filter)
		{
			yield return new PostHandler {
				Mode = TransferMode.Chunked,  Content = CreateRandomContent (),
				Description = "Large chunked post"
			};
		}

		[AsyncTest]
		public async Task Run (TestContext ctx, [TestHost] HttpServer server,
			[Fork (10, RandomDelay = 1500)] IFork fork, [Repeat (50)] int repeat,
			[TestParameter] Handler handler, CancellationToken cancellationToken)
		{
			ctx.LogDebug (1, "FORK START: {0} {1}", fork.ID, ctx.PortableSupport.CurrentThreadId);

			await TestRunner.RunTraditional (ctx, server, handler, cancellationToken);

			ctx.LogDebug (1, "FORK DONE: {0} {1}", fork.ID, ctx.PortableSupport.CurrentThreadId);
		}
	}
}
