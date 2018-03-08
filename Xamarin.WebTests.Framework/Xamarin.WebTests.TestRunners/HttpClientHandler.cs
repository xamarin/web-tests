//
// HttpClientHandler.cs
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
using System.IO;
using System.Text;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;
using Xamarin.AsyncTests;
using Xamarin.AsyncTests.Constraints;
using Xamarin.AsyncTests.Portable;

namespace Xamarin.WebTests.TestRunners
{
	using ConnectionFramework;
	using HttpFramework;
	using HttpHandlers;
	using TestFramework;
	using TestAttributes;
	using HttpClient;

	class HttpClientHandler : InstrumentationHandler
	{
		new public HttpClientTestRunner TestRunner => (HttpClientTestRunner)base.TestRunner;

		public bool CloseConnection {
			get;
		}

		public HttpClientHandler (HttpClientTestRunner parent, bool closeConnection)
			: base (parent, parent.EffectiveType.ToString ())
		{
			CloseConnection = closeConnection;

			Flags = RequestFlags.KeepAlive;
			if (CloseConnection)
				Flags |= RequestFlags.CloseConnection;
		}

		HttpClientHandler (HttpClientHandler other)
			: base (other)
		{
			CloseConnection = other.CloseConnection;
			Flags = other.Flags;
		}

		public override object Clone ()
		{
			return new HttpClientHandler (this);
		}

		internal Request CreateRequest (InstrumentationOperation operation, Uri uri)
		{
			bool ReuseHandler ()
			{
				if (operation.Type == InstrumentationOperationType.Primary)
					return false;

				switch (TestRunner.EffectiveType) {
				case HttpClientTestType.ReuseHandler:
				case HttpClientTestType.ReuseHandlerNoClose:
				case HttpClientTestType.ReuseHandlerChunked:
				case HttpClientTestType.ReuseHandlerGZip:
					return true;
				default:
					return false;
				}
			}

			if (!ReuseHandler ())
				return new HttpClientRequest (
					operation, this, uri);

			var primaryRequest = (HttpClientRequest)TestRunner.PrimaryOperation.Request;
			return new HttpClientRequest (
				operation, this, primaryRequest, uri);
		}

		public override void ConfigureRequest (TestContext ctx, Request request, Uri uri)
		{
			if (request is HttpClientRequest instrumentationRequest) {
				instrumentationRequest.ConfigureRequest (ctx, uri);
				return;
			}

			base.ConfigureRequest (ctx, request, uri);
		}

		internal async Task<Response> SendAsync (TestContext ctx, Request request, CancellationToken cancellationToken)
		{
			var instrumentationRequest = (HttpClientRequest)request;
			using (var cts = CancellationTokenSource.CreateLinkedTokenSource (cancellationToken)) {
				cts.Token.Register (() => instrumentationRequest.Abort ());
				return await instrumentationRequest.SendAsync (ctx, cts.Token).ConfigureAwait (false);
			}
		}

		protected internal override async Task<HttpResponse> HandleRequest (
			TestContext ctx, HttpOperation operation, HttpConnection connection, HttpRequest request,
			RequestFlags effectiveFlags, CancellationToken cancellationToken)
		{
			if (RemoteEndPoint == null)
				RemoteEndPoint = connection.RemoteEndPoint;

			await AbstractConnection.FinishedTask.ConfigureAwait (false);

			HttpContent content;
			switch (TestRunner.EffectiveType) {
			case HttpClientTestType.SimpleGZip:
				content = new GZipContent (ConnectionHandler.TheQuickBrownFoxBuffer);
				break;
			case HttpClientTestType.ReuseHandlerGZip:
				content = new GZipContent (ConnectionHandler.TheQuickBrownFoxBuffer);
				break;
			case HttpClientTestType.SequentialGZip:
			case HttpClientTestType.ParallelGZip:
			case HttpClientTestType.ParallelGZipNoClose:
				AssertNotReusingConnection (ctx, connection);
				content = new GZipContent (ConnectionHandler.TheQuickBrownFoxBuffer);
				break;
			case HttpClientTestType.SequentialRequests:
				AssertNotReusingConnection (ctx, connection);
				content = HttpContent.TheQuickBrownFox;
				break;
			case HttpClientTestType.ReuseHandler:
			case HttpClientTestType.ReuseHandlerNoClose:
				AssertReusingConnection (ctx, connection);
				content = HttpContent.TheQuickBrownFox;
				break;
			case HttpClientTestType.ReuseHandlerChunked:
				AssertReusingConnection (ctx, connection);
				content = new ChunkedContent (ConnectionHandler.TheQuickBrownFox);
				break;
			case HttpClientTestType.SequentialChunked:
				AssertNotReusingConnection (ctx, connection);
				content = new ChunkedContent (ConnectionHandler.TheQuickBrownFox);
				break;
			case HttpClientTestType.CancelPostWhileWriting:
				var currentRequest = (HttpClientRequest)operation.Request;
				await currentRequest.HandleCancelPost (
					ctx, connection, request, cancellationToken).ConfigureAwait (false);
				return new HttpResponse (HttpStatusCode.OK, null);
			default:
				throw ctx.AssertFail (TestRunner.EffectiveType);
			}

			return new HttpResponse (HttpStatusCode.OK, content);
		}

		public override bool CheckResponse (TestContext ctx, Response response)
		{
			HttpContent expectedContent;
			switch (TestRunner.EffectiveType) {
			case HttpClientTestType.SimpleGZip:
			case HttpClientTestType.ParallelGZip:
			case HttpClientTestType.ParallelGZipNoClose:
			case HttpClientTestType.SequentialRequests:
			case HttpClientTestType.SequentialChunked:
			case HttpClientTestType.SequentialGZip:
			case HttpClientTestType.ReuseHandler:
			case HttpClientTestType.ReuseHandlerNoClose:
			case HttpClientTestType.ReuseHandlerChunked:
			case HttpClientTestType.ReuseHandlerGZip:
				expectedContent = HttpContent.TheQuickBrownFox;
				break;
			case HttpClientTestType.CancelPost:
			case HttpClientTestType.CancelPostWhileWriting:
				return ctx.Expect (response.Content, Is.Null);
			default:
				expectedContent = new StringContent (ME);
				break;
			}

			if (!ctx.Expect (response.Content, Is.Not.Null, "response.Content != null"))
				return false;

			return HttpContent.Compare (ctx, response.Content, expectedContent, false, "response.Content");
		}
	}
}
