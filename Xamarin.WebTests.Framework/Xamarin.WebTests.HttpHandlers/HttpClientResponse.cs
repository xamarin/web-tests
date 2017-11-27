//
// HttpClientResponse.cs
//
// Author:
//       Martin Baulig <mabaul@microsoft.com>
//
// Copyright (c) 2017 Xamarin Inc. (http://www.xamarin.com)
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

using Xamarin.AsyncTests;
using Xamarin.AsyncTests.Constraints;

namespace Xamarin.WebTests.HttpHandlers
{
	using ConnectionFramework;
	using HttpFramework;
	using HttpClient;

	public sealed class HttpClientResponse : Response
	{
		public IHttpResponseMessage Response {
			get;
		}

		public override HttpStatusCode Status => Response.StatusCode;

		public override HttpContent Content {
			get;
		}

		public override bool IsSuccess => Response.IsSuccessStatusCode;

		public override Exception Error {
			get;
		}

		HttpClientResponse (Request request, IHttpResponseMessage response, HttpContent content, Exception error = null)
			: base (request)
		{
			Response = response;
			Content = content;
			Error = error;
		}

		public static async Task<HttpClientResponse> Create (HttpClientRequest request, IHttpResponseMessage response)
		{
			var content = await response.Content.GetContent ().ConfigureAwait (false);
			return new HttpClientResponse (request, response, content);
		}

		public override string ToString ()
		{
			return $"[HttpClientResponse {Status}]";
		}
	}
}
