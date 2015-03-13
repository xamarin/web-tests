﻿//
// HttpClientRequest.cs
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
using System.Threading;
using System.Threading.Tasks;

using Xamarin.AsyncTests;
using Xamarin.AsyncTests.Constraints;

namespace Xamarin.WebTests.Handlers
{
	using Framework;
	using Portable;
	using HttpClient;

	public class HttpClientRequest : Request
	{
		public readonly Uri RequestUri;
		public readonly IHttpClientHandler Handler;
		public readonly IHttpClient Client;
		readonly IPortableWebSupport WebSupport;
		HttpMethod method = HttpMethod.Get;
		string contentType;
		long? contentLength;

		public HttpClientRequest (HttpClientHandler parent, Uri uri)
		{
			RequestUri = uri;
			WebSupport = DependencyInjector.Get<IPortableWebSupport> ();
			Handler = WebSupport.CreateHttpClientHandler ();
			Client = Handler.CreateHttpClient ();
		}

		public override string Method {
			get { return method.ToString ().ToUpper (); }
			set {
				switch (value.ToUpper ()) {
				case "GET":
					method = HttpMethod.Get;
					break;
				case "POST":
					method = HttpMethod.Post;
					break;
				case "PUT":
					method = HttpMethod.Put;
					break;
				case "HEAD":
					method = HttpMethod.Head;
					break;
				case "DELETE":
					method = HttpMethod.Delete;
					break;
				default:
					throw new NotImplementedException ();
				}
			}
		}

		public override void SetContentLength (long contentLength)
		{
			this.contentLength = contentLength;
		}

		public override void SetContentType (string contentType)
		{
			this.contentType = contentType;
		}

		public override void SendChunked ()
		{
			throw new NotSupportedException ();
		}

		public override void SetProxy (IPortableProxy proxy)
		{
			WebSupport.SetProxy (Handler, proxy);
		}
		public override void SetCredentials (ICredentials credentials)
		{
			Handler.Credentials = credentials;
		}

		public async Task<Response> GetString (TestContext ctx, CancellationToken cancellationToken)
		{
			var cts = CancellationTokenSource.CreateLinkedTokenSource (cancellationToken);
			cts.Token.Register (() => Client.CancelPendingRequests ());

			try {
				var body = await Client.GetStringAsync (RequestUri);
				return new SimpleResponse (this, HttpStatusCode.OK, StringContent.CreateMaybeNull (body));
			} catch (Exception ex) {
				return new SimpleResponse (this, HttpStatusCode.InternalServerError, null, ex);
			} finally {
				cts.Dispose ();
			}
		}

		public async Task<Response> PostString (
			TestContext ctx, HttpContent returnContent, CancellationToken cancellationToken)
		{
			var message = Handler.CreateRequestMessage ();
			message.Method = HttpMethod.Post;
			message.RequestUri = RequestUri;
			message.Content = Handler.CreateStringContent (Content.AsString ());

			if (contentLength != null)
				message.Content.ContentLength = contentLength;
			if (contentType != null)
				message.Content.ContentType = contentType;

			var response = await Client.SendAsync (
				message, HttpCompletionOption.ResponseContentRead, cancellationToken);

			ctx.Assert (response, Is.Not.Null, "response");

			ctx.LogDebug (3, "GOT RESPONSE: {0}", response.StatusCode);

			if (!response.IsSuccessStatusCode)
				return new SimpleResponse (this, response.StatusCode, null);

			string body = null;
			if (response.Content != null) {
				body = await response.Content.ReadAsStringAsync ();
				ctx.LogDebug (5, "GOT BODY: {0}", body);
			}

			if (returnContent != null) {
				ctx.Assert (body, Is.Not.Null, "returned body");

				body = body.TrimEnd ();
				ctx.Assert (body, Is.EqualTo (returnContent.AsString ()), "returned body");
			} else {
				ctx.Assert (body, Is.Empty, "returned body");
			}

			return new SimpleResponse (this, response.StatusCode, returnContent);
		}
	}
}
