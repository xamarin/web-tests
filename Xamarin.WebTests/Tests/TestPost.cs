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
using System.Net;
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;

namespace Xamarin.WebTests.Tests
{
	using Server;
	using Framework;

	[TestFixture]
	public class TestPost : HttpTest
	{
		HttpListener listener;

		[TestFixtureSetUp]
		public void Start ()
		{
			listener = new HttpListener (IPAddress.Loopback, 9999);
		}

		[TestFixtureTearDown]
		public void Stop ()
		{
			listener.Stop ();
		}

		protected override HttpWebRequest CreateRequest (Handler handler)
		{
			return handler.CreateRequest (listener);
		}

		public static IEnumerable<Handler> GetHelloWorldTests ()
		{
			yield return new HelloWorldHandler ();
		}

		public static IEnumerable<PostHandler> GetPostTests ()
		{
			yield return new PostHandler () {
				Description = "No body"
			};
			yield return new PostHandler () {
				Description = "Empty body", Body = string.Empty
			};
			yield return new PostHandler () {
				Description = "Normal post",
				Body = "Hello Unknown World!"
			};
			yield return new PostHandler () {
				Description = "Content-Length",
				Body = "Hello Known World!",
				Mode = TransferMode.ContentLength
			};
			yield return new PostHandler () {
				Description = "Chunked",
				Body = "Hello Chunked World!",
				Mode = TransferMode.Chunked
			};
			yield return new PostHandler () {
				Description = "Explicit length and empty body",
				Mode = TransferMode.ContentLength,
				Body = string.Empty
			};
			yield return new PostHandler () {
				Description = "Explicit length and no body",
				Mode = TransferMode.ContentLength
			};
		}

		public static IEnumerable<Handler> GetDeleteTests ()
		{
			yield return new DeleteHandler ();
			yield return new DeleteHandler () {
				Description = "DELETE with empty body",
				Body = string.Empty
			};
			yield return new DeleteHandler () {
				Description = "DELETE with request body",
				Body = "I have a body!"
			};
			yield return new DeleteHandler () {
				Description = "DELETE with no body and a length",
				Flags = RequestFlags.ExplicitlySetLength
			};
		}

		public static IEnumerable<Handler> GetRedirectTests ()
		{
			foreach (var code in new [] { HttpStatusCode.Moved, HttpStatusCode.Found, HttpStatusCode.TemporaryRedirect }) {
				foreach (var post in GetPostTests ()) {
					var isWindows = Environment.OSVersion.Platform == PlatformID.Win32NT;
					var hasBody = post.Body != null || ((post.Flags & RequestFlags.ExplicitlySetLength) != 0) || (post.Mode == TransferMode.ContentLength);

					if ((hasBody || !isWindows) && (code == HttpStatusCode.MovedPermanently || code == HttpStatusCode.Found))
						post.Flags = RequestFlags.RedirectedAsGet;
					else
						post.Flags = RequestFlags.Redirected;
					post.Description = string.Format ("{0}: {1}", code, post.Description);
					yield return new RedirectHandler (post, code) { Description = post.Description };
				}
			}
		}

		[Test]
		public void RedirectAsGetNoBuffering ()
		{
			var post = new PostHandler {
				Description = "Chunked post",
				Body = "Hello chunked world",
				Mode = TransferMode.Chunked,
				Flags = RequestFlags.RedirectedAsGet,
				AllowWriteStreamBuffering = false
			};
			var redirect = new RedirectHandler (post, HttpStatusCode.Redirect);
			Run (redirect);
		}

		[Test]
		public void RedirectNoBuffering ()
		{
			var post = new PostHandler {
				Description = "Chunked post",
				Body = "Hello chunked world",
				Mode = TransferMode.Chunked,
				Flags = RequestFlags.Redirected,
				AllowWriteStreamBuffering = false
			};
			var redirect = new RedirectHandler (post, HttpStatusCode.TemporaryRedirect);
			Run (redirect, HttpStatusCode.TemporaryRedirect, true);
		}

		[TestCaseSource ("GetPostTests")]
		[TestCaseSource ("GetDeleteTests")]
		[TestCaseSource ("GetRedirectTests")]
		public void PostTests (Handler handler)
		{
			Run (handler);
		}

		[Test]
		public void Test18750 ()
		{
			var post = new PostHandler {
				Description = "First post",
				Body = "var1=value&var2=value2",
				Flags = RequestFlags.RedirectedAsGet
			};
			var redirect = new RedirectHandler (post, HttpStatusCode.Redirect);

			var uri = redirect.RegisterRequest (listener);

			var wc = new WebClient ();
			var res = wc.UploadString (uri, post.Body);
			Console.WriteLine (res);

			var secondPost = new PostHandler {
				Description = "Second post", Body = "Should send this"
			};

			Run (secondPost);
		}
	}
}

