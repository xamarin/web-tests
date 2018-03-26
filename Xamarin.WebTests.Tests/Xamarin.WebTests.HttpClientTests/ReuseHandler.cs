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
using Xamarin.AsyncTests;
using Xamarin.AsyncTests.Constraints;

namespace Xamarin.WebTests.HttpClientTests
{
	using ConnectionFramework;
	using TestFramework;
	using TestAttributes;
	using HttpFramework;
	using HttpHandlers;
	using HttpClient;
	using TestRunners;
	using System.Collections.Generic;

	[HttpServerTestCategory (HttpServerTestCategory.Default)]
	public class ReuseHandler : CustomHandlerFixture
	{
		public override HttpContent ExpectedContent => HttpContent.TheQuickBrownFox;

		public override HttpOperationFlags OperationFlags => HttpOperationFlags.DontReuseConnection;

		protected override bool ReusingHandler => true;

		public override bool CloseConnection => false;

		[TypeFilter]
		public enum ReuseHandlerType {
			Default,
			NoClose,
			Chunked,
			GZip
		}

		public ReuseHandlerType Type {
			get;
		}

		class TypeFilter : TestParameterAttribute, ITestParameterSource<ReuseHandlerType>
		{
			public IEnumerable<ReuseHandlerType> GetParameters (TestContext ctx, string filter)
			{
				yield return ReuseHandlerType.Default;
				yield return ReuseHandlerType.NoClose;
				yield return ReuseHandlerType.Chunked;

				if (DependencyInjector.Get<IConnectionFrameworkSetup> ().SupportsGZip)
					yield return ReuseHandlerType.GZip;
			}
		}

		[AsyncTest]
		public ReuseHandler (ReuseHandlerType type)
		{
			Type = type;
		}

		ServicePoint servicePoint;

		protected override void ConfigureRequest (
			TestContext ctx, CustomHandler handler, CustomRequest request)
		{
			if (handler.IsPrimary) {
				ctx.Assert (servicePoint, Is.Null, "should not be set");
				servicePoint = ServicePointManager.FindServicePoint (request.RequestUri);
				servicePoint.ConnectionLimit = 1;

				request.Handler.AutomaticDecompression |= Type == ReuseHandlerType.GZip;
			}
			base.ConfigureRequest (ctx, handler, request);
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
			TestContext ctx, HttpOperation operation,
			HttpConnection connection, HttpRequest request,
			CustomHandler handler, CancellationToken cancellationToken)
		{
			handler.AssertReusingConnection (ctx, connection);

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
			if (!handler.IsPrimary)
				response.CloseConnection = true;
			return Task.FromResult (response);
		}

		public override HttpOperation RunSecondary (
			TestContext ctx, HttpClientTestRunner runner,
			CancellationToken cancellationToken)
		{
			var handler = CreateHandler (ctx, runner, false);
			return runner.StartSequentialRequest (
				ctx, handler, OperationFlags, cancellationToken);
		}
	}
}
