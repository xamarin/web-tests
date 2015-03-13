//
// HttpContent.cs
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
using Xamarin.WebTests.Portable;
using Xamarin.WebTests.HttpClient;

namespace Xamarin.WebTests.HttpClient
{
	public abstract class HttpContent : IHttpContent
	{
		readonly Http.HttpContent content;

		public HttpContent (Http.HttpContent content)
		{
			this.content = content;
		}

		public Http.HttpContent Content {
			get { return content; }
		}

		public Task<string> ReadAsStringAsync ()
		{
			return content.ReadAsStringAsync ();
		}

		public long? ContentLength {
			get { return content.Headers.ContentLength; }
			set { content.Headers.ContentLength = value; }
		}

		public string ContentType {
			get {
				var header = content.Headers.ContentType;
				if (header == null)
					return null;
				return header.MediaType;
			}
			set {
				if (value == null) {
					content.Headers.ContentType = null;
					return;
				}

				Http.Headers.MediaTypeHeaderValue contentType;
				if (!Http.Headers.MediaTypeHeaderValue.TryParse (value, out contentType))
					throw new InvalidOperationException ();
				content.Headers.ContentType = contentType;
			}
		}
	}
}

