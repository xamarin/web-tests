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

namespace Xamarin.WebTests.Tests
{
	using Server;
	using Runners;
	using Handlers;
	using Framework;

	[AsyncTestFixture]
	public class TestPostAsync : ITestHost<TestRunner>, ITestParameterSource<Handler>
	{
		[TestParameter]
		public bool UseSSL {
			get; set;
		}

		public TestRunner CreateInstance (TestContext context)
		{
			if (UseSSL)
				return new HttpsTestRunner ();
			else
				return new HttpTestRunner ();
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

		public IEnumerable<Handler> GetParameters (TestContext context, string filter)
		{
			if (filter == null) {
				var list = new List<Handler> ();
				list.AddRange (GetPostTests ());
				list.AddRange (GetDeleteTests ());
				return list;
			} else if (filter.Equals ("post"))
				return GetPostTests ();
			else if (filter.Equals ("delete"))
				return GetDeleteTests ();
			else
				throw new InvalidOperationException ();
		}

		[AsyncTest]
		public Task Run (TestContext ctx, CancellationToken cancellationToken,
			[TestHost] TestRunner runner, [TestParameter] Handler handler)
		{
			return runner.Run (ctx, handler, cancellationToken);
		}
	}
}

