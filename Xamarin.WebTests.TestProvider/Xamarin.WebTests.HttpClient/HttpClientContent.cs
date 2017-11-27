//
// HttpClientContent.cs
//
// Author:
//       Martin Baulig <martin.baulig@xamarin.com>
//
// Copyright (c) 2015 Xamarin, Inc.
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
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Http = System.Net.Http;

namespace Xamarin.WebTests.HttpClient
{
	using HttpFramework;

	abstract class HttpClientContent : IHttpContent
	{
		HttpContent httpContent;

		public HttpClientContent (Http.HttpContent content)
		{
			Content = content;
		}

		public Http.HttpContent Content {
			get;
		}

		public Task<string> ReadAsStringAsync ()
		{
			return Content.ReadAsStringAsync ();
		}

		public long? ContentLength {
			get { return Content.Headers.ContentLength; }
			set { Content.Headers.ContentLength = value; }
		}

		public string ContentType {
			get {
				var header = Content.Headers.ContentType;
				if (header == null)
					return null;
				return header.MediaType;
			}
			set {
				if (value == null) {
					Content.Headers.ContentType = null;
					return;
				}

				Http.Headers.MediaTypeHeaderValue contentType;
				if (!Http.Headers.MediaTypeHeaderValue.TryParse (value, out contentType))
					throw new InvalidOperationException ();
				Content.Headers.ContentType = contentType;
			}
		}

		public async Task<HttpContent> GetContent ()
		{
			if (httpContent != null)
				return httpContent;

			httpContent = await LoadContent ().ConfigureAwait (false);
			return httpContent;
		}

		protected abstract Task<HttpContent> LoadContent ();
	}
}

