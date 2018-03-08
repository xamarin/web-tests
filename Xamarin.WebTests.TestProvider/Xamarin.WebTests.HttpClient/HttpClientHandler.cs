﻿//
// HttpClientHandler.cs
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
using Xamarin.WebTests.ConnectionFramework;
using Xamarin.WebTests.HttpClient;

namespace Xamarin.WebTests.HttpClient
{
	class HttpClientHandler : IHttpClientHandler
	{
		readonly Http.HttpClientHandler handler;
		IWebProxy proxy;

		public HttpClientHandler (Http.HttpClientHandler handler)
		{
			this.handler = handler;
		}

		public IHttpClient CreateHttpClient ()
		{
			var client = new Http.HttpClient (handler, true);
			return new HttpClient (client);
		}

		public IHttpRequestMessage CreateRequestMessage ()
		{
			var message = new Http.HttpRequestMessage ();
			return new HttpRequestMessage (message);
		}

		public IHttpRequestMessage CreateRequestMessage (HttpMethod method, Uri requestUri)
		{
			var message = new Http.HttpRequestMessage (HttpRequestMessage.GetMethod (method), requestUri);
			return new HttpRequestMessage (message);
		}

		public ICredentials Credentials {
			get { return handler.Credentials; }
			set { handler.Credentials = value; }
		}

		public IWebProxy Proxy {
			get { return proxy; }
			set {
				proxy = value;
				handler.Proxy = value;
			}
		}

		public bool AutomaticDecompression {
			get {
				return handler.AutomaticDecompression != DecompressionMethods.None;
			}
			set {
				if (value)
					handler.AutomaticDecompression = DecompressionMethods.GZip;
				else
					handler.AutomaticDecompression = DecompressionMethods.None;
			}
		}
	}
}

