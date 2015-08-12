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
	public class PostHandlerAttribute : TestParameterAttribute, ITestParameterSource<Handler>
	{
		public PostHandlerAttribute (string filter = null, TestFlags flags = TestFlags.Browsable)
			: base (filter, flags)
		{
		}

		public IEnumerable<Handler> GetParameters (TestContext ctx, string filter)
		{
			return TestPost.GetParameters (ctx, filter);
		}
	}

	[AsyncTestFixture (Timeout = 10000)]
	public class TestPost
	{
		[WebTestFeatures.SelectSSL]
		public bool UseSSL {
			get; set;
		}

		public static IEnumerable<PostHandler> GetPostTests ()
		{
			yield return new PostHandler ("No body");
			yield return new PostHandler ("Empty body", StringContent.Empty);
			yield return new PostHandler ("Normal post", HttpContent.HelloWorld);
			yield return new PostHandler ("Content-Length", HttpContent.HelloWorld, TransferMode.ContentLength);
			yield return new PostHandler ("Chunked", HttpContent.HelloChunked, TransferMode.Chunked);
			yield return new PostHandler ("Explicit length and empty body", StringContent.Empty, TransferMode.ContentLength);
			yield return new PostHandler ("Explicit length and no body", null, TransferMode.ContentLength);
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

		static IEnumerable<Handler> GetChunkedTests ()
		{
			var chunks = new List<string> ();
			for (var i = 'A'; i < 'Z'; i++) {
				chunks.Add (new string (i, 1000));
			}

			var content = new ChunkedContent (chunks);

			yield return new PostHandler ("Big Chunked", content, TransferMode.Chunked);
		}

		public static IEnumerable<Handler> GetParameters (TestContext ctx, string filter)
		{
			if (filter == null) {
				var list = new List<Handler> ();
				list.Add (new HelloWorldHandler ("hello world"));
				list.AddRange (GetPostTests ());
				list.AddRange (GetDeleteTests ());
				return list;
			} else if (filter.Equals ("post"))
				return GetPostTests ();
			else if (filter.Equals ("delete"))
				return GetDeleteTests ();
			else if (filter.Equals ("chunked"))
				return GetChunkedTests ();
			else
				throw new InvalidOperationException ();
		}

		[AsyncTest]
		public Task RedirectAsGetNoBuffering (
			TestContext ctx, [HttpServer] HttpServer server,
			CancellationToken cancellationToken)
		{
			var post = new PostHandler ("RedirectAsGetNoBuffering", HttpContent.HelloChunked, TransferMode.Chunked) {
				Flags = RequestFlags.RedirectedAsGet,
				AllowWriteStreamBuffering = false
			};
			var handler = new RedirectHandler (post, HttpStatusCode.Redirect);
			return TestRunner.RunTraditional (ctx, server, handler, cancellationToken);
		}

		[AsyncTest]
		public Task RedirectNoBuffering (
			TestContext ctx, [HttpServer] HttpServer server,
			CancellationToken cancellationToken)
		{
			var post = new PostHandler ("RedirectNoBuffering", HttpContent.HelloChunked, TransferMode.Chunked) {
				Flags = RequestFlags.Redirected,
				AllowWriteStreamBuffering = false
			};
			var handler = new RedirectHandler (post, HttpStatusCode.TemporaryRedirect);
			return TestRunner.RunTraditional (
				ctx, server, handler, cancellationToken, false,
				HttpStatusCode.TemporaryRedirect, WebExceptionStatus.ProtocolError);
		}

		[AsyncTest]
		public Task Run (
			TestContext ctx, [HttpServer] HttpServer server, bool sendAsync,
			[PostHandler] Handler handler, CancellationToken cancellationToken)
		{
			return TestRunner.RunTraditional (ctx, server, handler, cancellationToken, sendAsync);
		}

		[AsyncTest]
		public Task Redirect (
			TestContext ctx, [HttpServer] HttpServer server, bool sendAsync,
			[RedirectStatus] HttpStatusCode code,
			[PostHandler ("post")] Handler handler, CancellationToken cancellationToken)
		{
			var post = (PostHandler)handler;
			var support = DependencyInjector.Get<IPortableSupport> ();
			var isWindows = support.IsMicrosoftRuntime;
			var hasBody = post.Content != null || ((post.Flags & RequestFlags.ExplicitlySetLength) != 0) || (post.Mode == TransferMode.ContentLength);

			if ((hasBody || !isWindows) && (code == HttpStatusCode.MovedPermanently || code == HttpStatusCode.Found))
				post.Flags = RequestFlags.RedirectedAsGet;
			else
				post.Flags = RequestFlags.Redirected;
			var identifier = string.Format ("{0}: {1}", code, post.ID);
			var redirect = new RedirectHandler (post, code, identifier);

			return TestRunner.RunTraditional (ctx, server, redirect, cancellationToken, sendAsync);
		}

		[AsyncTest]
		public async Task Test18750 (
			TestContext ctx, [HttpServer] HttpServer server,
			CancellationToken cancellationToken)
		{
			var post = new PostHandler ("First post", new StringContent ("var1=value&var2=value2")) {
				Flags = RequestFlags.RedirectedAsGet
			};
			var redirect = new RedirectHandler (post, HttpStatusCode.Redirect);

			await redirect.RunWithContext (ctx, server, async (uri) => {
				var support = DependencyInjector.Get<IPortableWebSupport> ();
				using (var wc = support.CreateWebClient ()) {
					var res = await wc.UploadStringTaskAsync (uri, post.Content.AsString ());
					ctx.LogDebug (2, "Test18750: {0}", res);
					return res;
				}
			});

			var secondPost = new PostHandler ("Second post", new StringContent ("Should send this"));

			await TestRunner.RunTraditional (ctx, server, secondPost, cancellationToken);
		}

		[AsyncTest]
		public Task TestChunked (
			TestContext ctx, [HttpServer] HttpServer server, bool sendAsync,
			[PostHandler ("chunked")] Handler handler, CancellationToken cancellationToken)
		{
			return TestRunner.RunTraditional (ctx, server, handler, cancellationToken, sendAsync);
		}

		Handler CreateAuthMaybeNone (Handler handler, AuthenticationType authType)
		{
			if (authType == AuthenticationType.None)
				return handler;
			var auth = new AuthenticationHandler (authType, handler);
			return auth;
		}

		void ConfigureWebClient (IWebClient client, Handler handler, CancellationToken cancellationToken)
		{
			cancellationToken.Register (() => client.Cancel ());
			var authHandler = handler as AuthenticationHandler;
			if (authHandler != null)
				client.SetCredentials (authHandler.GetCredentials ());
		}

		[AsyncTest]
		[WebTestFeatures.Mono381]
		public async Task Test10163 (
			TestContext ctx, [HttpServer] HttpServer server,
			[AuthenticationType] AuthenticationType authType,
			CancellationToken cancellationToken)
		{
			var post = new PostHandler ("Post bug #10163", HttpContent.HelloWorld);

			var handler = CreateAuthMaybeNone (post, authType);

			await handler.RunWithContext (ctx, server, async (uri) => {
				var support = DependencyInjector.Get<IPortableWebSupport> ();
				using (var client = support.CreateWebClient ()) {
					ConfigureWebClient (client, handler, cancellationToken);

					var stream = await client.OpenWriteAsync (uri, "PUT");

					using (var writer = new StreamWriter (stream)) {
						await post.Content.WriteToAsync (writer);
					}
				}
			});
		}

		[AsyncTest]
		[WebTestFeatures.Mono381]
		public async Task Test20359 (
			TestContext ctx, [HttpServer] HttpServer server,
			[AuthenticationType] AuthenticationType authType,
			CancellationToken cancellationToken)
		{
			var post = new PostHandler ("Post bug #20359", new StringContent ("var1=value&var2=value2"));

			post.CustomHandler = (request) => {
				string header;
				if (!request.Headers.TryGetValue ("Content-Type", out header))
					header = null;
				ctx.Expect (header, Is.EqualTo ("application/x-www-form-urlencoded"), "Content-Type");
				return null;
			};

			var handler = CreateAuthMaybeNone (post, authType);

			await handler.RunWithContext (ctx, server, async (uri) => {
				var support = DependencyInjector.Get<IPortableWebSupport> ();
				using (var client = support.CreateWebClient ()) {
					ConfigureWebClient (client, handler, cancellationToken);

					var collection = new List<KeyValuePair<string, string>> ();
					collection.Add (new KeyValuePair<string, string> ("var1", "value"));
					collection.Add (new KeyValuePair<string, string> ("var2", "value2"));

					byte[] data;
					try {
						data = await client.UploadValuesTaskAsync (uri, "POST", collection);
					} catch {
						if (ctx.HasPendingException)
							return false;
						throw;
					}

					var ok = ctx.Expect (data, Is.Not.Null, "Returned array");
					ok &= ctx.Expect (data.Length, Is.EqualTo (0), "Returned array");
					return ok;
				}
			});
		}

		[AsyncTest]
		public Task Test31830 (TestContext ctx, [HttpServer] HttpServer server, bool writeStreamBuffering, CancellationToken cancellationToken)
		{
			var handler = new PostHandler ("Obscure HTTP verb.");
			handler.Method = "EXECUTE";
			handler.AllowWriteStreamBuffering = writeStreamBuffering;
			handler.Flags |= RequestFlags.NoContentLength;
			return TestRunner.RunTraditional (ctx, server, handler, cancellationToken);
		}
	}
}

