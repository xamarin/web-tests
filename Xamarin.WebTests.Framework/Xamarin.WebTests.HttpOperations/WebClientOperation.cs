//
// WebClientOperation.cs
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
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Xamarin.AsyncTests;

namespace Xamarin.WebTests.HttpOperations
{
	using System.Collections.Specialized;
	using HttpFramework;
	using HttpHandlers;

	public class WebClientOperation : HttpOperation
	{
		public WebClientOperationType Type {
			get;
		}

		public HttpContent Content {
			get;
		}

		public NameValueCollection Values {
			get { return values; }
			set {
				if (Type != WebClientOperationType.UploadValuesTaskAsync)
					throw new InvalidOperationException ();
				values = value;
			}
		}

		WebClient client;
		NameValueCollection values;

		public WebClientOperation (HttpServer server, Handler handler, WebClientOperationType type)
			: base (server, $"{server.ME}:{handler}", handler, HttpOperationFlags.None,
			        HttpStatusCode.OK, WebExceptionStatus.Success)
		{
			Type = type;
			Content = GetContent (handler);
		}

		static HttpContent GetContent (Handler handler)
		{
			if (handler is AbstractRedirectHandler redirect)
				return GetContent (redirect.Target);
			if (handler is PostHandler post)
				return post.Content;
			return null;
		}

		protected override void ConfigureRequest (TestContext ctx, Uri uri, Request request)
		{
			Handler.ConfigureRequest (request, uri);

			request.SetProxy (Server.GetProxy ());
		}

		protected override Request CreateRequest (TestContext ctx, Uri uri)
		{
			client = new WebClient ();
			return new WebClientRequest (this, client, uri);
		}

		protected override void Destroy ()
		{
			if (client != null) {
				client.Dispose ();
				client = null;
			}
		}

		protected override Task<Response> RunInner (TestContext ctx, Request request, CancellationToken cancellationToken)
		{
			var clientRequest = (WebClientRequest)request;
			return clientRequest.SendAsync (ctx, cancellationToken);
		}

		class WebClientRequest : Request
		{
			public WebClientOperation Operation {
				get;
			}

			public WebClient Client {
				get;
			}

			public Uri Uri {
				get;
			}

			string method = "GET";

			public override string Method {
				get { return method; }
				set { method = value; }
			}

			public WebClientRequest (WebClientOperation operation, WebClient client, Uri uri)
			{
				Operation = operation;
				Client = client;
				Uri = uri;
			}

			public override void SetContentLength (long contentLength)
			{
				throw new NotSupportedException ();
			}

			public override void SetContentType (string contentType)
			{
				;
			}

			public override void SendChunked ()
			{
				throw new NotSupportedException ();
			}

			public override void SetProxy (IWebProxy proxy)
			{
				Client.Proxy = proxy;
			}

			public override void SetCredentials (ICredentials credentials)
			{
				Client.Credentials = credentials;
			}

			public override async Task<Response> SendAsync (TestContext ctx, CancellationToken cancellationToken)
			{
				byte[] data;
				string text;
				HttpContent content;

				switch (Operation.Type) {
				case WebClientOperationType.DownloadStringTaskAsync:
					text = await Client.DownloadStringTaskAsync (Uri).ConfigureAwait (false);
					return new SimpleResponse (this, HttpStatusCode.OK, StringContent.CreateMaybeNull (text));

				case WebClientOperationType.UploadStringTaskAsync:
					text = await Client.UploadStringTaskAsync (Uri, Operation.Content.AsString ()).ConfigureAwait (false);
					content = !string.IsNullOrWhiteSpace (text) ? new StringContent (text) : null;
					return new SimpleResponse (this, HttpStatusCode.OK, content);

				case WebClientOperationType.OpenWriteTaskAsync:
					using (var stream = await Client.OpenWriteTaskAsync (Uri, Method).ConfigureAwait (false))
					using (var writer = new StreamWriter (stream)) {
						await Operation.Content.WriteToAsync (ctx, writer);
					}
					return new SimpleResponse (this, HttpStatusCode.OK, null);

				case WebClientOperationType.UploadValuesTaskAsync:
					data = await Client.UploadValuesTaskAsync (Uri, Method, Operation.Values).ConfigureAwait (false);
					content = data != null && data.Length > 0 ? new BinaryContent (data) : null;
					return new SimpleResponse (this, HttpStatusCode.OK, content);

				default:
					throw ctx.AssertFail (Operation.Type);
				}
			}

			public override void Abort ()
			{
				Client.CancelAsync ();
			}
		}
	}
}
