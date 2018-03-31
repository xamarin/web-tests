//
// ReuseHandler.cs
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
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using Xamarin.AsyncTests;
using Xamarin.AsyncTests.Constraints;

namespace Xamarin.WebTests.HttpClientInstrumentation
{
	using ConnectionFramework;
	using TestFramework;
	using TestAttributes;
	using HttpFramework;
	using HttpHandlers;
	using HttpClient;
	using TestRunners;

	[HttpServerTestCategory (HttpServerTestCategory.Default)]
	public class ReuseHandler : HttpClientInstrumentationFixture
	{
		public override HttpContent ExpectedContent => HttpContent.TheQuickBrownFox;

		public override HttpOperationFlags OperationFlags => HttpOperationFlags.DontReuseConnection;

		protected override bool ReusingHandler => true;

		public override RequestFlags RequestFlags => RequestFlags.KeepAlive;

		public enum ReuseHandlerType {
			Default,
			NoClose,
			Chunked,
			[GZip]
			GZip
		}

		public ReuseHandlerType Type {
			get;
		}

		[AsyncTest]
		public ReuseHandler (ReuseHandlerType type)
		{
			Type = type;
		}

		ServicePoint servicePoint;

		protected override void ConfigureRequest (
			TestContext ctx, InstrumentationOperation operation,
			CustomRequest request)
		{
			if (operation.Type == InstrumentationOperationType.Primary) {
				ctx.Assert (servicePoint, Is.Null, "should not be set");
				servicePoint = ServicePointManager.FindServicePoint (request.RequestUri);
				servicePoint.ConnectionLimit = 1;

				request.Handler.AutomaticDecompression |= Type == ReuseHandlerType.GZip;
			}
			base.ConfigureRequest (ctx, operation, request);
		}

		protected override Task<Response> Run (
			TestContext ctx, CustomRequest request,
			CancellationToken cancellationToken)
		{
			switch (Type) {
			case ReuseHandlerType.Default:
				return request.GetString (ctx, cancellationToken);
			case ReuseHandlerType.NoClose:
			case ReuseHandlerType.Chunked:
			case ReuseHandlerType.GZip:
				return request.GetStringNoClose (ctx, cancellationToken);
			default:
				throw ctx.AssertFail (Type);
			}
		}

		public override Task<HttpResponse> HandleRequest (
			TestContext ctx, InstrumentationOperation operation,
			HttpConnection connection, HttpRequest request,
			RequestFlags effectiveFlags,
			CancellationToken cancellationToken)
		{
			AssertReusingConnection (ctx, operation, connection);

			HttpContent content;
			switch (Type) {
			case ReuseHandlerType.Default:
			case ReuseHandlerType.NoClose:
				content = HttpContent.TheQuickBrownFox;
				break;
			case ReuseHandlerType.Chunked:
				content = HttpContent.TheQuickBrownFoxChunked;
				break;
			case ReuseHandlerType.GZip:
				content = new GZipContent (ConnectionHandler.TheQuickBrownFoxBuffer);
				break;
			default:
				throw ctx.AssertFail (Type);
			}

			var response = new HttpResponse (HttpStatusCode.OK, content);
			if (operation.Type != InstrumentationOperationType.Primary)
				response.CloseConnection = true;
			return Task.FromResult (response);
		}

		protected override Task<HttpOperation> RunSecondary (
			TestContext ctx, CancellationToken cancellationToken)
		{
			return Task.Run (() => {
				return StartSequentialRequest (
					ctx, OperationFlags, cancellationToken);
			});
		}
	}
}
