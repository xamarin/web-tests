//
// HttpClient.cs
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
using Xamarin.WebTests.HttpClient;

namespace Xamarin.WebTests.HttpClient
{
	public class HttpClient : IHttpClient
	{
		readonly Http.HttpClient client;

		public HttpClient (Http.HttpClient client)
		{
			this.client = client;
		}

		public void CancelPendingRequests ()
		{
			client.CancelPendingRequests ();
		}

		public Task<string> GetStringAsync (Uri requestUri)
		{
			return client.GetStringAsync (requestUri);
		}

		public async Task<IHttpResponseMessage> SendAsync (IHttpRequestMessage request, HttpCompletionOption completionOption, CancellationToken cancellationToken)
		{
			var message = ((HttpRequestMessage)request).Message;
			var response = await client.SendAsync (message, (Http.HttpCompletionOption)completionOption, cancellationToken);
			return new HttpResponseMessage (response);
		}

		public async Task<IHttpResponseMessage> PutAsync (Uri requestUri, IHttpContent content, CancellationToken cancellationToken)
		{
			Http.HttpContent httpContent = null;
			if (content != null)
				httpContent = ((HttpContent)content).Content;
			var response = await client.PutAsync (requestUri, httpContent, cancellationToken);
			return new HttpResponseMessage (response);
		}
	}
}

