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
using Xamarin.AsyncTests.Portable;

namespace Xamarin.WebTests
{
	using Handlers;
	using Framework;
	using Portable;

	[AttributeUsage (AttributeTargets.Parameter | AttributeTargets.Property, AllowMultiple = false)]
	public class HttpClientHandlerAttribute : TestParameterAttribute, ITestParameterSource<HttpClientHandler>
	{
		public HttpClientHandlerAttribute (string filter = null, TestFlags flags = TestFlags.Browsable)
			: base (filter, flags)
		{
		}

		public IEnumerable<HttpClientHandler> GetParameters (TestContext ctx, string filter)
		{
			return TestHttpClient.GetParameters (ctx, filter);
		}
	}

	[AsyncTestFixture (Timeout = 10000)]
	public class TestHttpClient : ITestHost<HttpServer>
	{
		[WebTestFeatures.SelectSSL]
		public bool UseSSL {
			get; set;
		}

		public HttpServer CreateInstance (TestContext ctx)
		{
			var support = DependencyInjector.Get<IPortableEndPointSupport> ();
			return new HttpServer (support.GetLoopbackEndpoint (9999), false, UseSSL);
		}

		static IEnumerable<HttpClientHandler> GetStableTests ()
		{
			yield return new HttpClientHandler (
				"Get string", HttpClientOperation.GetString);
			yield return new HttpClientHandler (
				"Post string", HttpClientOperation.PostString, HttpContent.HelloWorld);
			yield return new HttpClientHandler (
				"Post string with result", HttpClientOperation.PostString,
				HttpContent.HelloWorld, new StringContent ("Returned body"));
			yield return new HttpClientHandler (
				"Put", HttpClientOperation.Put, HttpContent.HelloWorld);
		}

		static IEnumerable<HttpClientHandler> GetMono38Tests ()
		{
			yield return new HttpClientHandler (
				"Bug #20583", HttpClientOperation.PostString,
				HttpContent.HelloWorld, new Bug20583Content ()) {
				Filter = (ctx) => ctx.IsEnabled (WebTestFeatures.Mono38)
			};
		}

		public static IEnumerable<HttpClientHandler> GetParameters (TestContext ctx, string filter)
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
			[TestHost] HttpServer server, [HttpClientHandler ("stable")] HttpClientHandler handler)
		{
			return TestRunner.RunHttpClient (ctx, cancellationToken, server, handler);
		}

		[AsyncTest]
		public Task RunMono38 (TestContext ctx, CancellationToken cancellationToken,
			[TestHost] HttpServer server, [HttpClientHandler ("mono38")] HttpClientHandler handler)
		{
			return TestRunner.RunHttpClient (ctx, cancellationToken, server, handler);
		}

		[AsyncTest]
		public Task Run (TestContext ctx, CancellationToken cancellationToken, [TestHost] HttpServer server)
		{
			var handler = new HttpClientHandler ("PutRedirectEmptyBody", HttpClientOperation.Put);
			var redirect = new RedirectHandler (handler, HttpStatusCode.TemporaryRedirect);
			return TestRunner.RunHttpClient (ctx, cancellationToken, server, handler, redirect);
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

