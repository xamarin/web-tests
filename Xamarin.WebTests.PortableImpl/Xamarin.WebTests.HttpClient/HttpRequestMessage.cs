//
// HttpRequestMessage.cs
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
	public class HttpRequestMessage : IHttpRequestMessage
	{
		readonly Http.HttpRequestMessage message;
		HttpContent content;

		public HttpRequestMessage (Http.HttpRequestMessage message)
		{
			this.message = message;
		}

		public Http.HttpRequestMessage Message {
			get { return message; }
		}

		static HttpMethod GetMethod (Http.HttpMethod method)
		{
			if (method == Http.HttpMethod.Get)
				return HttpMethod.Get;
			else if (method == Http.HttpMethod.Post)
				return HttpMethod.Post;
			else if (method == Http.HttpMethod.Put)
				return HttpMethod.Put;
			else if (method == Http.HttpMethod.Delete)
				return HttpMethod.Delete;
			else if (method == Http.HttpMethod.Head)
				return HttpMethod.Head;
			throw new InvalidOperationException ();
		}

		static Http.HttpMethod GetMethod (HttpMethod method)
		{
			switch (method) {
			case HttpMethod.Get:
				return Http.HttpMethod.Get;
			case HttpMethod.Post:
				return Http.HttpMethod.Post;
			case HttpMethod.Put:
				return Http.HttpMethod.Put;
			case HttpMethod.Delete:
				return Http.HttpMethod.Delete;
			case HttpMethod.Head:
				return Http.HttpMethod.Head;
			default:
				throw new InvalidOperationException ();
			}
		}

		public HttpMethod Method {
			get { return GetMethod (message.Method); }
			set { message.Method = GetMethod (value); }
		}

		public void SetObscureMethod (string method)
		{
			message.Method = new Http.HttpMethod (method);
		}

		public Uri RequestUri {
			get { return message.RequestUri; }
			set { message.RequestUri = value; }
		}

		public HttpContent Content {
			get { return content; }
			set {
				content = value;
				message.Content = value.Content;
			}
		}

		IHttpContent IHttpRequestMessage.Content {
			get { return Content; }
			set { Content = (HttpContent)value; }
		}
	}
}

