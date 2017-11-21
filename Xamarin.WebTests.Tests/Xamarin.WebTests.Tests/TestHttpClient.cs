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
	using HttpOperations;
	using TestRunners;

	[AsyncTestFixture (Timeout = 10000)]
	public class TestHttpClient : ITestParameterSource<HttpClientHandler>
	{
		[WebTestFeatures.SelectHttpServerFlags]
		public HttpServerFlags Flags {
			get; set;
		}

		IEnumerable<HttpClientHandler> GetStableTests ()
		{
			yield return new HttpClientHandler (
				"Get string", HttpClientOperationType.GetString, null, HttpContent.HelloWorld);
			yield return new HttpClientHandler (
				"Post string", HttpClientOperationType.PostString, HttpContent.HelloWorld);
			yield return new HttpClientHandler (
				"Post string with result", HttpClientOperationType.PostString,
				HttpContent.HelloWorld, new StringContent ("Returned body"));
			yield return new HttpClientHandler (
				"Put", HttpClientOperationType.PutString, HttpContent.HelloWorld);
			if ((Flags & HttpServerFlags.HttpListener) == 0)
				yield return new HttpClientHandler (
					"Bug #20583", HttpClientOperationType.PostString,
					HttpContent.HelloWorld, new Bug20583Content ());
			yield return new HttpClientHandler (
				"Bug #41206", HttpClientOperationType.PutDataAsync,
				BinaryContent.CreateRandom (102400));
			yield return new HttpClientHandler (
				"Bug #41206 odd size", HttpClientOperationType.PutDataAsync,
				BinaryContent.CreateRandom (102431));
		}

		static IEnumerable<HttpClientHandler> GetRecentlyFixed ()
		{
			yield return new HttpClientHandler (
				"Put chunked", HttpClientOperationType.SendAsyncChunked,
				ConnectionHandler.GetLargeChunkedContent (50)) {
			};
		}

		static IEnumerable<HttpClientHandler> GetMartinTests ()
		{
			yield return new HttpClientHandler (
				"Post redirect", HttpClientOperationType.PostRedirect,
				HttpContent.HelloWorld, new StringContent ("Returned body"));
		}

		public IEnumerable<HttpClientHandler> GetParameters (TestContext ctx, string filter)
		{
			var list = new List<HttpClientHandler> ();
			switch (filter) {
			case null:
			case "stable":
				list.AddRange (GetStableTests ());
				break;
			case "recently-fixed":
				list.AddRange (GetRecentlyFixed ());
				break;
			case "martin":
				list.AddRange (GetMartinTests ());
				break;
			default:
				throw new InvalidOperationException ();
			}
			return list;
		}

		[AsyncTest (ParameterFilter = "stable")]
		public async Task Run (TestContext ctx, CancellationToken cancellationToken,
		                       HttpServer server, HttpClientHandler handler)
		{
			using (var operation = new HttpClientOperation (server, handler)) {
				await operation.Run (ctx, cancellationToken).ConfigureAwait (false);
			}
		}

		[RecentlyFixed]
		[AsyncTest (ParameterFilter = "recently-fixed")]
		public async Task RunRecentlyFixed (TestContext ctx, CancellationToken cancellationToken,
		                                    HttpServer server, HttpClientHandler handler)
		{
			using (var operation = new HttpClientOperation (server, handler)) {
				await operation.Run (ctx, cancellationToken).ConfigureAwait (false);
			}
		}

		[Martin ("HttpClient")]
		[AsyncTest (ParameterFilter = "martin")]
		public async Task RunMartin (TestContext ctx, CancellationToken cancellationToken,
		                             HttpServer server, HttpClientHandler handler)
		{
			using (var operation = new HttpClientOperation (server, handler)) {
				await operation.Run (ctx, cancellationToken).ConfigureAwait (false);
			}
		}

		[AsyncTest]
		public async Task Run (TestContext ctx, CancellationToken cancellationToken, HttpServer server)
		{
			var handler = new HttpClientHandler ("PutRedirectEmptyBody", HttpClientOperationType.PutString);
			var redirect = new RedirectHandler (handler, HttpStatusCode.TemporaryRedirect);
			using (var operation = new HttpClientOperation (server, redirect)) {
				await operation.Run (ctx, cancellationToken).ConfigureAwait (false);
			}
		}

		[AsyncTest]
		public async Task SendAsync (TestContext ctx, HttpServer server, CancellationToken cancellationToken)
		{
			var handler = new HttpClientHandler ("SendAsyncEmptyBody", HttpClientOperationType.SendAsync);
			using (var operation = new HttpClientOperation (server, handler)) {
				await operation.Run (ctx, cancellationToken).ConfigureAwait (false);
			}
		}

		[AsyncTest]
		public async Task Test31830 (TestContext ctx, HttpServer server, CancellationToken cancellationToken)
		{
			var handler = new HttpClientHandler ("SendAsyncObscureVerb", HttpClientOperationType.SendAsync);
			handler.ObscureHttpMethod = "EXECUTE";
			using (var operation = new HttpClientOperation (server, handler)) {
				await operation.Run (ctx, cancellationToken).ConfigureAwait (false);
			}
		}

		class Bug20583Content : HttpContent
		{
			#region implemented abstract members of HttpContent
			public override bool HasLength {
				get { return true; }
			}
			public override int Length {
				get { return 4; }
			}
			public override string AsString ()
			{
				return "AAAA";
			}
			public override byte[] AsByteArray ()
			{
				throw new NotSupportedException ();
			}
			public override void AddHeadersTo (HttpMessage message)
			{
				message.TransferEncoding = "chunked";
				message.ContentType = "text/plain";
			}
			public override async Task WriteToAsync (TestContext ctx, Stream stream, CancellationToken cancellationToken)
			{
				await Task.Delay (500).ConfigureAwait (false);
				await stream.WriteAsync ("0");
				await Task.Delay (500);
				await stream.WriteAsync ("4\r\n");
				await stream.WriteAsync ("AAAA\r\n0\r\n\r\n");
			}
			#endregion
		}


	}
}

