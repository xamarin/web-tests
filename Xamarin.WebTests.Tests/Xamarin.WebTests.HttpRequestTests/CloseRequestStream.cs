//
// CloseRequestStream.cs
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
using Xamarin.AsyncTests.Portable;

namespace Xamarin.WebTests.HttpRequestTests
{
	using TestFramework;
	using HttpFramework;
	using HttpHandlers;
	using TestRunners;

	public class CloseRequestStream : RequestTestFixture
	{
		public override bool HasRequestBody => false;

		public override HttpOperationFlags OperationFlags => HttpOperationFlags.AbortAfterClientExits;

		public override HttpStatusCode ExpectedStatus => HttpStatusCode.InternalServerError;

		public override WebExceptionStatus ExpectedError => WebExceptionStatus.RequestCanceled;

		public override HttpContent ExpectedContent => throw new NotImplementedException ();

		public override RequestFlags RequestFlags => RequestFlags.KeepAlive;

		protected override void ConfigureRequest (
			TestContext ctx, InstrumentationOperation operation,
			TraditionalRequest request)
		{
			request.Method = "POST";
			request.SetContentLength (16384);
			base.ConfigureRequest (ctx, operation, request);
		}

		protected override async Task<Response> SendRequest (
			TestContext ctx, TraditionalRequest request,
			CancellationToken cancellationToken)
		{
			var stream = await request.RequestExt.GetRequestStreamAsync ().ConfigureAwait (false);
			try {
				DependencyInjector.Get<IPortableSupport> ().Close (stream);
				throw ctx.AssertFail ("Expected exception.");
			} catch (Exception ex) {
				return new TraditionalResponse (request, HttpStatusCode.InternalServerError, ex);
			}
		}

		public override HttpResponse HandleRequest (
			TestContext ctx, InstrumentationOperation operation,
			HttpConnection connection, HttpRequest request)
		{
			throw ctx.AssertFail ("Should never happen.");
		}
	}
}
