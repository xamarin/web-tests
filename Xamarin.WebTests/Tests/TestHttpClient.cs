//
// TestHttpClient.cs
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
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;

using Xamarin.AsyncTests;

namespace Xamarin.WebTests
{
	using Handlers;
	using Framework;
	using Portable;

	[AsyncTestFixture (Timeout = 10000)]
	public class TestHttpClient : ITestHost<HttpServer>, ITestParameterSource<HttpClientHandler>
	{
		[TestParameter (typeof (WebTestFeatures.SelectSSL), null, TestFlags.Hidden)]
		public bool UseSSL {
			get; set;
		}

		public HttpServer CreateInstance (TestContext ctx)
		{
			return new HttpServer (PortableSupport.Web.GetLoopbackEndpoint (9999), false, UseSSL);
		}

		IEnumerable<HttpClientHandler> GetStableTests ()
		{
			yield return new HttpClientHandler {
				Operation = HttpClientOperation.GetString, Description = "Get string"
			};
			yield return new HttpClientHandler {
				Operation = HttpClientOperation.PostString,
				Content = new StringContent ("Hello World!"),
				Description = "Post string"
			};
			yield return new HttpClientHandler {
				Operation = HttpClientOperation.PostString,
				Content = new StringContent ("Hello World!"),
				ReturnContent = new StringContent ("Returned body"),
				Description = "Post string with result"
			};
		}

		IEnumerable<HttpClientHandler> GetMono38Tests ()
		{
			yield return new HttpClientHandler {
				Operation = HttpClientOperation.PostString,
				Content = new StringContent ("Hello World!"),
				ReturnContent = new Bug20583Content (),
				Description = "Bug #20583",
				Filter = (ctx) => ctx.IsEnabled (WebTestFeatures.Mono38)
			};
		}

		public IEnumerable<HttpClientHandler> GetParameters (TestContext ctx, string filter)
		{
			if (filter == null || filter.Equals ("stable")) {
				foreach (var test in GetStableTests ())
					yield return test;
			}

			if (filter == null || filter.Equals ("mono38")) {
				foreach (var test in GetMono38Tests ())
					yield return test;
			}
		}

		[AsyncTest]
		public Task Run (TestContext ctx, CancellationToken cancellationToken,
			[TestHost] HttpServer server, [TestParameter ("stable")] HttpClientHandler handler)
		{
			return TestRunner.RunHttpClient (ctx, server, handler, cancellationToken);
		}

		[AsyncTest]
		public Task RunMono38 (TestContext ctx, CancellationToken cancellationToken,
			[TestHost] HttpServer server, [TestParameter ("mono38")] HttpClientHandler handler)
		{
			return TestRunner.RunHttpClient (ctx, server, handler, cancellationToken);
		}

		class Bug20583Content : HttpContent
		{
			#region implemented abstract members of HttpContent
			public override string AsString ()
			{
				return "AAAA";
			}
			public override void AddHeadersTo (HttpMessage message)
			{
				message.SetHeader ("Transfer-Encoding", "chunked");
				message.SetHeader ("Content-Type", "text/plain");
			}
			public override async Task WriteToAsync (StreamWriter writer)
			{
				writer.AutoFlush = true;
				await Task.Delay (500).ConfigureAwait (false);
				await writer.WriteAsync ("0");
				await Task.Delay (500);
				await writer.WriteAsync ("4\r\n");
				await writer.WriteAsync ("AAAA\r\n0\r\n\r\n");
			}
			#endregion
		}


	}
}

