﻿//
// CustomRequest.cs
//
// Author:
//       Martin Baulig <mabaul@microsoft.com>
//
// Copyright (c) 2018 Xamarin Inc. (http://www.xamarin.com)
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

namespace Xamarin.WebTests.HttpClientTests
{
	using HttpFramework;
	using HttpHandlers;
	using HttpClient;

	public class CustomRequest : Request
	{
		public CustomHandler Parent {
			get;
		}

		public Uri RequestUri {
			get;
		}

		public IHttpClientProvider Provider {
			get;
		}

		public IHttpClient Client {
			get;
		}

		public IHttpClientHandler Handler {
			get;
		}

		internal CustomRequest (CustomHandler parent, Uri requestUri)
		{
			Parent = parent;
			RequestUri = requestUri;

			Provider = DependencyInjector.Get<IHttpClientProvider> ();
			Handler = Provider.Create ();
			Client = Handler.CreateHttpClient ();
		}

		internal CustomRequest (
			CustomHandler parent, Uri requestUri,
			CustomRequest primaryRequest)
		{
			Parent = parent;
			RequestUri = requestUri;

			Provider = primaryRequest.Provider;
			Handler = primaryRequest.Handler;
			Client = primaryRequest.Client;
		}

		public override string Method {
			get => throw new NotImplementedException ();
			set => throw new NotImplementedException (); }

		public override void Abort ()
		{
			Client.CancelPendingRequests ();
		}

		public override void SendChunked ()
		{
			throw new NotSupportedException ();
		}

		public override void SetContentLength (long contentLength)
		{
			throw new NotSupportedException ();
		}

		public override void SetContentType (string contentType)
		{
			throw new NotSupportedException ();
		}

		public override void SetCredentials (ICredentials credentials)
		{
			throw new NotSupportedException ();
		}

		public override void SetProxy (IWebProxy proxy)
		{
			throw new NotSupportedException ();
		}

		public override Task<Response> SendAsync (
			TestContext ctx, CancellationToken cancellationToken)
		{
			throw new NotSupportedException ();
		}

		public async Task<Response> GetString (
			TestContext ctx, CancellationToken cancellationToken)
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

		public async Task<Response> GetStringNoClose (
			TestContext ctx, CancellationToken cancellationToken)
		{
			var method = Handler.CreateRequestMessage (HttpMethod.Get, RequestUri);
			method.SetKeepAlive ();
			var response = await Client.SendAsync (
				method, HttpCompletionOption.ResponseContentRead,
				cancellationToken).ConfigureAwait (false);
			ctx.Assert (response.IsSuccessStatusCode, "success");
			var body = await response.Content.ReadAsStringAsync ();
			var content = StringContent.CreateMaybeNull (body);
			return new SimpleResponse (this, HttpStatusCode.OK, content);
		}

	}
}
