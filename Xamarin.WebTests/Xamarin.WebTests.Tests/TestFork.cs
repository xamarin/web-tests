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
using Xamarin.AsyncTests.Portable;

namespace Xamarin.WebTests.Tests
{
	using HttpHandlers;
	using HttpFramework;
	using TestRunners;
	using Portable;
	using Providers;
	using Features;

	[AttributeUsage (AttributeTargets.Parameter | AttributeTargets.Property, AllowMultiple = false)]
	public class ForkHandlerAttribute : TestParameterAttribute, ITestParameterSource<Handler>
	{
		public ForkHandlerAttribute (string filter = null, TestFlags flags = TestFlags.Browsable)
			: base (filter, flags)
		{
		}

		public IEnumerable<Handler> GetParameters (TestContext ctx, string filter)
		{
			return TestFork.GetParameters (ctx, filter);
		}
	}

	[WebTestFeatures.Heavy]
	[AsyncTestFixture (Timeout = 5000)]
	public class TestFork : ITestHost<HttpServer>
	{
		public HttpServer CreateInstance (TestContext ctx)
		{
			var provider = DependencyInjector.Get<ConnectionProviderFactory> ().DefaultHttpProvider;
			var support = DependencyInjector.Get<IPortableEndPointSupport> ();
			return new HttpServer (provider, support.GetLoopbackEndpoint (9999), ListenerFlags.ReuseConnection);
		}

		static HttpContent CreateRandomContent (TestContext ctx)
		{
			return new RandomContent (2 << 15, 2 << 22);
		}

		public static IEnumerable<Handler> GetParameters (TestContext ctx, string filter)
		{
			yield return new PostHandler (
				"Large chunked post", CreateRandomContent (ctx), TransferMode.Chunked) {
				AllowWriteStreamBuffering = false
			};
		}

		const string PostPuppy = "http://localhost/~martin/work/bugfree-octo-nemesis/www/cgi-bin/post-puppy.pl";

		[AsyncTest]
		public async Task Run (TestContext ctx, [TestHost] HttpServer server,
			[Fork (5, RandomDelay = 1500)] IFork fork, [Repeat (50)] int repeat,
			[ForkHandler] Handler handler, CancellationToken cancellationToken)
		{
			var support = DependencyInjector.Get<IPortableSupport> ();
			ctx.LogMessage ("FORK START: {0} {1}", fork.ID, support.CurrentThreadId);

			await TestRunner.RunTraditional (ctx, server, handler, cancellationToken);

			ctx.LogMessage ("FORK DONE: {0} {1}", fork.ID, support.CurrentThreadId);
		}

		[Martin]
		[AsyncTest]
		public async Task RunPuppy (TestContext ctx, [TestHost] HttpServer server,
			[Fork (5, RandomDelay = 1500)] IFork fork, [Repeat (50)] int repeat,
			[ForkHandler] Handler handler, CancellationToken cancellationToken)
		{
			var support = DependencyInjector.Get<IPortableSupport> ();
			ctx.LogMessage ("FORK START: {0} {1}", fork.ID, support.CurrentThreadId);

			var uri = new Uri (PostPuppy);

			var request = new TraditionalRequest (uri);
			handler.ConfigureRequest (request, request.Request.Request.RequestUri);

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

			ctx.LogMessage ("FORK DONE: {0} {1} {2}", fork.ID, support.CurrentThreadId, ok);
		}
	}
}
