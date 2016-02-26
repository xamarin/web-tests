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
	using HttpHandlers;
	using HttpFramework;
	using TestFramework;
	using TestRunners;
	using Features;

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
	public class TestHttpClient
	{
		[WebTestFeatures.SelectSSL]
		public bool UseSSL {
			get; set;
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
			yield return new HttpClientHandler (
				"Bug #20583", HttpClientOperation.PostString,
				HttpContent.HelloWorld, new Bug20583Content ());
		}

		public static IEnumerable<HttpClientHandler> GetParameters (TestContext ctx, string filter)
		{
			if (filter == null || filter.Equals ("stable")) {
				foreach (var test in GetStableTests ())
					yield return test;
			}
		}

		[AsyncTest]
		public Task Run (TestContext ctx, CancellationToken cancellationToken,
			[HttpServer] HttpServer server, [HttpClientHandler ("stable")] HttpClientHandler handler)
		{
			return TestRunner.RunHttpClient (ctx, cancellationToken, server, handler);
		}

		[AsyncTest]
		public Task RunMono38 (TestContext ctx, CancellationToken cancellationToken,
			[HttpServer] HttpServer server, [HttpClientHandler ("mono38")] HttpClientHandler handler)
		{
			return TestRunner.RunHttpClient (ctx, cancellationToken, server, handler);
		}

		[AsyncTest]
		public Task Run (TestContext ctx, CancellationToken cancellationToken, [HttpServer] HttpServer server)
		{
			var handler = new HttpClientHandler ("PutRedirectEmptyBody", HttpClientOperation.Put);
			var redirect = new RedirectHandler (handler, HttpStatusCode.TemporaryRedirect);
			return TestRunner.RunHttpClient (ctx, cancellationToken, server, handler, redirect);
		}

		[AsyncTest]
		public Task SendAsync (TestContext ctx, [HttpServer] HttpServer server, CancellationToken cancellationToken)
		{
			var handler = new HttpClientHandler ("SendAsyncEmptyBody", HttpClientOperation.SendAsync);
			return TestRunner.RunHttpClient (ctx, cancellationToken, server, handler);
		}

		[AsyncTest]
		public Task Test31830 (TestContext ctx, [HttpServer] HttpServer server, CancellationToken cancellationToken)
		{
			var handler = new HttpClientHandler ("SendAsyncObscureVerb", HttpClientOperation.SendAsync);
			handler.ObscureHttpMethod = "EXECUTE";
			return TestRunner.RunHttpClient (ctx, cancellationToken, server, handler);
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

