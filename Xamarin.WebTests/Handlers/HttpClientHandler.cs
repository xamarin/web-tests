//
// HttpClientHandler.cs
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
using System.Net;
using Http = System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Xamarin.WebTests.Handlers
{
	using Server;
	using Framework;

	public class HttpClientHandler : Handler
	{
		HttpClientOperation operation;

		public HttpClientOperation Operation {
			get { return operation; }
			set {
				WantToModify ();
				operation = value;
			}
		}

		public override object Clone ()
		{
			var handler = new HttpClientHandler ();
			handler.operation = operation;
			return handler;
		}

		protected internal override HttpResponse HandleRequest (HttpConnection connection, HttpRequest request, RequestFlags effectiveFlags)
		{
			if (!request.Method.Equals ("GET"))
				return HttpResponse.CreateError ("Wrong method: {0}", request.Method);

			return HttpResponse.CreateSuccess (string.Format ("Hello World!"));
		}

		public override Request CreateRequest (Uri uri)
		{
			return new HttpClientRequest (this, uri);
		}

		class HttpClientRequest : Request
		{
			public readonly Uri RequestUri;
			public readonly HttpClientHandler Parent;
			public readonly Http.HttpClientHandler Handler;
			public readonly Http.HttpClient Client;

			public HttpClientRequest (HttpClientHandler parent, Uri uri)
			{
				Parent = parent;
				RequestUri = uri;
				Handler = new Http.HttpClientHandler ();
				Client = new Http.HttpClient (Handler, true);
			}

			public override async Task<Response> Send (CancellationToken cancellationToken)
			{
				var cts = CancellationTokenSource.CreateLinkedTokenSource (cancellationToken);
				cts.Token.Register (() => Client.CancelPendingRequests ());

				try {
					switch (Parent.Operation) {
					case HttpClientOperation.GetStringAsync:
						return await GetStringAsync (cts.Token);
					default:
						throw new InvalidOperationException ();
					}
				} finally {
					cts.Dispose ();
				}
			}

			public override void SetProxy (IWebProxy proxy)
			{
				Handler.Proxy = proxy;
			}
			public override void SetCredentials (ICredentials credentials)
			{
				Handler.Credentials = credentials;
			}

			async Task<Response> GetStringAsync (CancellationToken cancellationToken)
			{
				try {
					var body = await Client.GetStringAsync (RequestUri);
					return new SimpleResponse (this, HttpStatusCode.OK, body);
				} catch (Exception ex) {
					return new SimpleResponse (this, HttpStatusCode.InternalServerError, null, ex);
				}
			}
		}
	}
}

