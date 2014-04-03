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
	public class TestPost
	{
		Listener listener;

		[TestFixtureSetUp]
		public void Start ()
		{
			listener = new Listener (IPAddress.Loopback, 9999);
		}

		[TestFixtureTearDown]
		public void Stop ()
		{
			listener.Stop ();
		}

		public IEnumerable<PostHandler> GetPostTests ()
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

		public IEnumerable<Handler> GetDeleteTests ()
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

		public IEnumerable<Handler> GetRedirectTests ()
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

		public IEnumerable<Handler> BrokenTests ()
		{
			yield break;
		}

		[Category ("NotWorking")]
		[TestCaseSource ("BrokenTests")]
		public void Work (Handler handler)
		{
			DoRun (handler);
		}

		[TestCaseSource ("GetPostTests")]
		[TestCaseSource ("GetDeleteTests")]
		[TestCaseSource ("GetRedirectTests")]
		public void Run (Handler handler)
		{
			DoRun (handler);
		}

		void DoRun (Handler handler)
		{
			Console.Error.WriteLine ("RUN: {0}", handler);

			var request = handler.CreateRequest (listener);

			try {
				var response = (HttpWebResponse)request.GetResponse ();
				Console.Error.WriteLine ("RUN - GOT RESPONSE: {0} {1}", handler, response.StatusCode);
				response.Close ();
			} catch (WebException wexc) {
				var response = (HttpWebResponse)wexc.Response;
				using (var reader = new StreamReader (response.GetResponseStream ())) {
					var content = reader.ReadToEnd ();
					Console.Error.WriteLine ("RUN - GOT WEB ERROR: {0} {1}\n{2}", handler, response.StatusCode, content);
					Assert.Fail ("{0}: {1}", handler, content);
				}
				response.Close ();
				throw;
			} catch (Exception ex) {
				Console.Error.WriteLine ("RUN - GOT EXCEPTION: {0}", ex);
				throw;
			} finally {
				Console.Error.WriteLine ("RUN DONE: {0}", handler);
			}
		}
	}
}

