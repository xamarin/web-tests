//
// HttpClientInstrumentationHandler.cs
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

	class HttpClientInstrumentationHandler : InstrumentationHandler
	{
		new public HttpClientTestRunner TestRunner => (HttpClientTestRunner)base.TestRunner;

		public bool CloseConnection {
			get;
		}

		public IPEndPoint RemoteEndPoint {
			get;
			private set;
		}

		public HttpClientInstrumentationHandler (HttpClientTestRunner parent, bool closeConnection)
			: base (parent, parent.EffectiveType.ToString ())
		{
			CloseConnection = closeConnection;

			Flags = RequestFlags.KeepAlive;
			if (CloseConnection)
				Flags |= RequestFlags.CloseConnection;
		}

		HttpClientInstrumentationHandler (HttpClientInstrumentationHandler other)
			: base (other)
		{
			CloseConnection = other.CloseConnection;
			Flags = other.Flags;
		}

		public override object Clone ()
		{
			return new HttpClientInstrumentationHandler (this);
		}

		protected internal override async Task<HttpResponse> HandleRequest (
			TestContext ctx, HttpOperation operation, HttpConnection connection, HttpRequest request,
			RequestFlags effectiveFlags, CancellationToken cancellationToken)
		{
			await AbstractConnection.FinishedTask.ConfigureAwait (false);

			RemoteEndPoint = connection.RemoteEndPoint;

			TestRunner.HandleRequest (ctx, this, connection, request);

			HttpContent content;
			switch (TestRunner.EffectiveType) {
			case HttpClientTestType.SimpleGZip:
			case HttpClientTestType.ParallelGZip:
			case HttpClientTestType.ParallelGZipNoClose:
			case HttpClientTestType.SequentialGZip:
			case HttpClientTestType.ReuseHandlerGZip:
				content = new GZipContent (ConnectionHandler.TheQuickBrownFoxBuffer);
				break;

			case HttpClientTestType.SequentialRequests:
			case HttpClientTestType.ReuseHandler:
			case HttpClientTestType.ReuseHandlerNoClose:
				content = HttpContent.TheQuickBrownFox;
				break;

			case HttpClientTestType.ReuseHandlerChunked:
			case HttpClientTestType.SequentialChunked:
				content = new ChunkedContent (ConnectionHandler.TheQuickBrownFox);
				break;

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
