//
// HttpResponseMessage.cs
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
	public class HttpResponseMessage : IHttpResponseMessage
	{
		readonly Http.HttpResponseMessage message;
		HttpContent content;

		public HttpResponseMessage (Http.HttpResponseMessage message)
		{
			this.message = message;
		}

		public HttpStatusCode StatusCode {
			get { return message.StatusCode; }
		}

		public bool IsSuccessStatusCode {
			get { return message.IsSuccessStatusCode; }
		}

		void InitializeContent ()
		{
			if (message.Content == null)
				return;

			var stringContent = message.Content as Http.StringContent;
			if (stringContent != null) {
				content = new StringContent (stringContent);
				return;
			}

			var streamContent = message.Content as Http.StreamContent;
			if (streamContent != null) {
				content = new StreamContent (streamContent);
				return;
			}

			throw new NotImplementedException ();
		}

		public IHttpContent Content {
			get {
				InitializeContent ();
				return content;
			}
		}
	}
}

