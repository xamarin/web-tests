//
// TestPost.cs
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
using System.Collections.Specialized;

using Xamarin.AsyncTests;
using Xamarin.AsyncTests.Constraints;
using Xamarin.AsyncTests.Portable;

namespace Xamarin.WebTests.Tests
{
	using HttpHandlers;
	using HttpFramework;
	using TestFramework;
	using HttpOperations;
	using TestRunners;

	[AsyncTestFixture (Timeout = 10000)]
	public class TestPost : ITestParameterSource<Handler>, ITestParameterSource<PostHandler>
	{
		[WebTestFeatures.SelectHttpServerFlags]
		public HttpServerFlags Flags {
			get; set;
		}

		public bool SendAsync {
			get; set;
		}

		public static IEnumerable<PostHandler> GetPostTests (HttpServerFlags flags)
		{
			if ((flags & HttpServerFlags.HttpListener) == 0)
				yield return new PostHandler ("No body");
			yield return new PostHandler ("Empty body", StringContent.Empty);
			yield return new PostHandler ("Normal post", HttpContent.HelloWorld);
			yield return new PostHandler ("Content-Length", HttpContent.HelloWorld, TransferMode.ContentLength);
			yield return new PostHandler ("Chunked", HttpContent.HelloChunked, TransferMode.Chunked);
			yield return new PostHandler ("Explicit length and empty body", StringContent.Empty, TransferMode.ContentLength);
			yield return new PostHandler ("Explicit length and no body", null, TransferMode.ContentLength);
			yield return new PostHandler ("Bug #41206", new RandomContent (102400));
			yield return new PostHandler ("Bug #41206 odd size", new RandomContent (102431));
		}

		public static IEnumerable<Handler> GetDeleteTests ()
		{
			yield return new DeleteHandler ("Empty delete");
			yield return new DeleteHandler ("DELETE with empty body", string.Empty);
			yield return new DeleteHandler ("DELETE with request body", "I have a body!");
			yield return new DeleteHandler ("DELETE with no body and a length") {
				Flags = RequestFlags.ExplicitlySetLength
			};
		}

		internal static Handler GetBigChunkedHandler ()
		{
			var chunks = new List<string> ();
			for (var i = 'A'; i < 'Z'; i++) {
				chunks.Add (new string (i, 1000));
			}

			var content = new ChunkedContent (chunks);

			return new PostHandler ("Big Chunked", content, TransferMode.Chunked);
		}

		static IEnumerable<Handler> GetChunkedTests ()
		{
			yield return GetBigChunkedHandler ();
		}

		static IEnumerable<PostHandler> GetRecentlyFixed ()
		{
			yield break;
		}

		IEnumerable<Handler> ITestParameterSource<Handler>.GetParameters (TestContext ctx, string filter)
		{
			return GetParameters (ctx, filter, Flags);
		}

		IEnumerable<PostHandler> ITestParameterSource<PostHandler>.GetParameters (TestContext ctx, string filter)
		{
			return GetPostTests (Flags);
		}

		public static IEnumerable<Handler> GetParameters (TestContext ctx, string filter, HttpServerFlags flags)
		{
			switch (filter) {
			case null:
				var list = new List<Handler> ();
				list.Add (new HelloWorldHandler ("hello world"));
				list.AddRange (GetPostTests (flags));
				list.AddRange (GetDeleteTests ());
				list.AddRange (GetRecentlyFixed ());
				return list;
			case "post":
				return GetPostTests (flags);
			case "delete":
				return GetDeleteTests ();
			case "chunked":
				return GetChunkedTests ();
			case "recently-fixed":
				return GetRecentlyFixed ();
			default:
				throw ctx.AssertFail ("Invalid TestPost filter `{0}'.", filter);
			}
		}

		[AsyncTest]
		public Task RedirectAsGetNoBuffering (TestContext ctx, HttpServer server, CancellationToken cancellationToken)
		{
			var post = new PostHandler ("RedirectAsGetNoBuffering", HttpContent.HelloChunked, TransferMode.Chunked) {
				Flags = RequestFlags.RedirectedAsGet,
				AllowWriteStreamBuffering = false
			};
			var handler = new RedirectHandler (post, HttpStatusCode.Redirect);
			return TestRunner.RunTraditional (ctx, server, handler, cancellationToken, SendAsync);
		}

		[AsyncTest]
		public Task RedirectNoBuffering (TestContext ctx, HttpServer server, CancellationToken cancellationToken)
		{
			var post = new PostHandler ("RedirectNoBuffering", HttpContent.HelloChunked, TransferMode.Chunked) {
				Flags = RequestFlags.Redirected,
				AllowWriteStreamBuffering = false
			};
			var handler = new RedirectHandler (post, HttpStatusCode.TemporaryRedirect);
			return TestRunner.RunTraditional (
				ctx, server, handler, cancellationToken, SendAsync,
				HttpOperationFlags.ClientDoesNotSendRedirect,
				HttpStatusCode.TemporaryRedirect, WebExceptionStatus.ProtocolError);
		}

		[AsyncTest]
		public Task Run (TestContext ctx, HttpServer server,
		                 Handler handler, CancellationToken cancellationToken)
		{
			return TestRunner.RunTraditional (ctx, server, handler, cancellationToken, SendAsync);
		}

		[AsyncTest]
		public Task Redirect (TestContext ctx, HttpServer server,
		                      [RedirectStatus] HttpStatusCode code, PostHandler post,
		                      CancellationToken cancellationToken)
		{
			var support = DependencyInjector.Get<IPortableSupport> ();
			var isWindows = support.IsMicrosoftRuntime;
			var hasBody = post.Content != null || ((post.Flags & RequestFlags.ExplicitlySetLength) != 0) || (post.Mode == TransferMode.ContentLength);

			if ((hasBody || !isWindows) && (code == HttpStatusCode.MovedPermanently || code == HttpStatusCode.Found))
				post.Flags = RequestFlags.RedirectedAsGet;
			else
				post.Flags = RequestFlags.Redirected;
			var identifier = string.Format ("{0}: {1}", code, post.ID);
			var redirect = new RedirectHandler (post, code, identifier);

			return TestRunner.RunTraditional (ctx, server, redirect, cancellationToken, SendAsync);
		}

		[AsyncTest]
		public async Task Test18750 (TestContext ctx, HttpServer server, CancellationToken cancellationToken)
		{
			var post = new PostHandler ("First post", new StringContent ("var1=value&var2=value2")) {
				Flags = RequestFlags.RedirectedAsGet
			};
			var redirect = new RedirectHandler (post, HttpStatusCode.Redirect);

			using (var operation = new WebClientOperation (server, redirect, WebClientOperationType.UploadStringTaskAsync)) {
				await operation.Run (ctx, cancellationToken).ConfigureAwait (false);
			}

			var secondPost = new PostHandler ("Second post", new StringContent ("Should send this"));

			using (var operation = new TraditionalOperation (server, secondPost, true)) {
				await operation.Run (ctx, cancellationToken).ConfigureAwait (false);
			}
		}

		[AsyncTest (ParameterFilter = "chunked")]
		public Task TestChunked (TestContext ctx, HttpServer server, bool sendAsync,
		                         Handler handler, CancellationToken cancellationToken)
		{
			return TestRunner.RunTraditional (ctx, server, handler, cancellationToken, SendAsync);
		}

		Handler CreateAuthMaybeNone (Handler handler, AuthenticationType authType)
		{
			if (authType == AuthenticationType.None)
				return handler;
			var auth = new AuthenticationHandler (authType, handler);
			return auth;
		}

		void ConfigureWebClient (WebClient client, Handler handler, CancellationToken cancellationToken)
		{
			cancellationToken.Register (() => client.CancelAsync ());
			var authHandler = handler as AuthenticationHandler;
			if (authHandler != null)
				client.Credentials = authHandler.Manager.Credentials;
		}

		[AsyncTest]
		public async Task Test10163 (TestContext ctx, HttpServer server,
		                             [AuthenticationType] AuthenticationType authType,
		                             CancellationToken cancellationToken)
		{
			int handlerCalled = 0;
			var post = new PostHandler ("Post bug #10163", HttpContent.HelloWorld);
			post.Method = "PUT";
			post.CustomHandler = (request) => {
				Interlocked.Increment (ref handlerCalled);
				return null;
			};

			var handler = CreateAuthMaybeNone (post, authType);

			using (var operation = new WebClientOperation (server, handler, WebClientOperationType.OpenWriteTaskAsync)) {
				await operation.Run (ctx, cancellationToken).ConfigureAwait (false);
			}

			ctx.Assert (handlerCalled, Is.EqualTo (1), "handler called");
		}

		[AsyncTest]
		public async Task Test20359 (TestContext ctx, HttpServer server,
		                             [AuthenticationType] AuthenticationType authType,
		                             CancellationToken cancellationToken)
		{
			var post = new PostHandler ("Post bug #20359", new StringContent ("var1=value&var2=value2"));
			post.Method = "POST";

			post.CustomHandler = (request) => {
				ctx.Expect (request.ContentType, Is.EqualTo ("application/x-www-form-urlencoded"), "Content-Type");
				return null;
			};

			var handler = CreateAuthMaybeNone (post, authType);

			using (var operation = new WebClientOperation (server, handler, WebClientOperationType.UploadValuesTaskAsync)) {
				var collection = new NameValueCollection ();
				collection.Add ("var1", "value");
				collection.Add ("var2", "value2");

				operation.Values = collection;

				await operation.Run (ctx, cancellationToken).ConfigureAwait (false);
			}
		}

		[AsyncTest]
		public Task Test31830 (TestContext ctx, HttpServer server, bool writeStreamBuffering, CancellationToken cancellationToken)
		{
			var handler = new PostHandler ("Obscure HTTP verb.");
			handler.Method = "EXECUTE";
			handler.AllowWriteStreamBuffering = writeStreamBuffering;
			handler.Flags |= RequestFlags.NoContentLength;
			return TestRunner.RunTraditional (ctx, server, handler, cancellationToken, SendAsync);
		}

		[AsyncTest (ParameterFilter = "recently-fixed")]
		[WebTestFeatures.RecentlyFixed]
		public Task TestRecentlyFixed (TestContext ctx, HttpServer server, bool sendAsync, Handler handler,
		                               CancellationToken cancellationToken)
		{
			return TestRunner.RunTraditional (ctx, server, handler, cancellationToken, SendAsync);
		}
	}
}

