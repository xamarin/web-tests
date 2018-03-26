﻿//
// TraditionalRequest.cs
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
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

using Xamarin.AsyncTests;

namespace Xamarin.WebTests.HttpHandlers
{
	using ConnectionFramework;
	using HttpFramework;

	public class TraditionalRequest : Request
	{
		public readonly HttpWebRequest Request;
		public readonly IHttpWebRequestExtension RequestExt;

		public TraditionalRequest (Uri uri)
		{
			Request = (HttpWebRequest)HttpWebRequest.Create (uri);
			RequestExt = DependencyInjector.GetExtension<HttpWebRequest,IHttpWebRequestExtension> (Request);

			RequestExt.SetKeepAlive (true);
		}

		public TraditionalRequest (HttpWebRequest request)
		{
			Request = request;
			RequestExt = DependencyInjector.GetExtension<HttpWebRequest,IHttpWebRequestExtension> (Request);

			RequestExt.SetKeepAlive (true);
		}

		#region implemented abstract members of Request

		public override void SetCredentials (ICredentials credentials)
		{
			Request.Credentials = credentials;
		}

		public override void SetProxy (IWebProxy proxy)
		{
			RequestExt.SetProxy (proxy);
		}

		public override string Method {
			get { return Request.Method; }
			set { Request.Method = value; }
		}

		public override void SetContentLength (long contentLength)
		{
			RequestExt.SetContentLength (contentLength);
		}

		public override void SetContentType (string contentType)
		{
			Request.ContentType = contentType;
		}

		public override void SendChunked ()
		{
			RequestExt.SetSendChunked (true);
		}

		public async Task<Response> Send (TestContext ctx, CancellationToken cancellationToken)
		{
			var cts = CancellationTokenSource.CreateLinkedTokenSource (cancellationToken);
			cts.Token.Register (() => Request.Abort ());

			var task = Task.Factory.StartNew (() => {
				return Send (ctx);
			});

			try {
				return await task;
			} finally {
				cts.Dispose ();
			}
		}

		protected virtual async Task WriteBody (TestContext ctx, CancellationToken cancellationToken)
		{
			using (var stream = await RequestExt.GetRequestStreamAsync ().ConfigureAwait (false)) {
				await Content.WriteToAsync (ctx, stream, cancellationToken);
			}
		}

		public override async Task<Response> SendAsync (TestContext ctx, CancellationToken cancellationToken)
		{
			var cts = CancellationTokenSource.CreateLinkedTokenSource (cancellationToken);
			cts.Token.Register (() => Request.Abort ());

			try {
				if (Content != null)
					await WriteBody (ctx, cancellationToken).ConfigureAwait (false);

				var response = await RequestExt.GetResponseAsync ().ConfigureAwait (false);
				return await GetResponseFromHttp (ctx, response, null, cancellationToken);
			} catch (WebException wexc) {
				var response = (HttpWebResponse)wexc.Response;
				if (response == null)
					return new TraditionalResponse (this, HttpStatusCode.InternalServerError, wexc);

				return await GetResponseFromHttp (ctx, response, wexc, cancellationToken);
			} catch (Exception ex) {
				return new TraditionalResponse (this, HttpStatusCode.InternalServerError, ex);
			} finally {
				cts.Dispose ();
			}
		}

		TraditionalResponse GetResponseFromHttpSync (TestContext ctx, HttpWebResponse response, WebException error)
		{
			return GetResponseFromHttp (ctx, response, error, CancellationToken.None).Result;
		}

		protected async virtual Task<TraditionalResponse> GetResponseFromHttp (TestContext ctx, HttpWebResponse response, WebException error, CancellationToken cancellationToken)
		{
			string content = null;
			var status = response.StatusCode;

			using (var reader = new StreamReader (response.GetResponseStream ())) {
				if (!reader.EndOfStream)
					content = await reader.ReadToEndAsync ().ConfigureAwait (false);
			}

			return new TraditionalResponse (this, response, StringContent.CreateMaybeNull (content), error);
		}

		TraditionalResponse Send (TestContext ctx)
		{
			try {
				if (Content != null) {
					using (var stream = RequestExt.GetRequestStream ())
						Content.WriteToAsync (ctx, stream, CancellationToken.None).Wait ();
				}

				var response = RequestExt.GetResponse ();
				return GetResponseFromHttpSync (ctx, response, null);
			} catch (WebException wexc) {
				var response = (HttpWebResponse)wexc.Response;
				if (response == null)
					return new TraditionalResponse (this, HttpStatusCode.InternalServerError, wexc);

				return GetResponseFromHttpSync (ctx, response, wexc);
			} catch (Exception ex) {
				return new TraditionalResponse (this, HttpStatusCode.InternalServerError, ex);
			}
		}

		public override void Abort ()
		{
			Request.Abort ();
		}

		#endregion
	}
}

