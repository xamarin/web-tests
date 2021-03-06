﻿//
// ReuseAfterPartialRead.cs
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
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xamarin.AsyncTests;
using Xamarin.AsyncTests.Constraints;

namespace Xamarin.WebTests.HttpInstrumentationTests
{
	using ConnectionFramework;
	using TestFramework;
	using HttpFramework;
	using HttpHandlers;
	using TestRunners;

	public class ReuseAfterPartialRead : HttpInstrumentationTestFixture
	{
		public sealed override HttpOperationFlags OperationFlags {
			get;
		}

		public HttpContent Content {
			get;
		}

		public ReuseAfterPartialRead ()
		{
			Content = ConnectionHandler.GetLargeStringContent (2500);
			OperationFlags = HttpOperationFlags.ClientUsesNewConnection;
		}

		protected override Request CreateRequest (
			TestContext ctx, InstrumentationOperation operation,
			Uri uri)
		{
			return new InstrumentationRequest (this, uri);
		}

		protected override bool ConfigureNetworkStream (
			TestContext ctx, StreamInstrumentation instrumentation)
		{
			return false;
		}

		public override Task<HttpResponse> HandleRequest (
			TestContext ctx, InstrumentationOperation operation,
			HttpConnection connection, HttpRequest request,
			RequestFlags effectiveFlags, CancellationToken cancellationToken)
		{
			ctx.Assert (request.Method, Is.EqualTo ("GET"), "method");
			AssertNotReusingConnection (ctx, operation, connection);
			var response = new HttpResponse (HttpStatusCode.OK, Content) {
				WriteAsBlob = true
			};
			return Task.FromResult (response);
		}

		protected override async Task<HttpContent> ReadResponseBody (
			TestContext ctx, TraditionalRequest request,
			HttpWebResponse response, Stream stream,
			CancellationToken cancellationToken)
		{
			var buffer = new byte[1234];
			var ret = await stream.ReadAsync (buffer, 0, buffer.Length).ConfigureAwait (false);
			ctx.Assert (ret, Is.EqualTo (buffer.Length));
			return StringContent.CreateMaybeNull (new ASCIIEncoding ().GetString (buffer, 0, ret));
		}

		public override bool CheckResponse (TestContext ctx, Response response)
		{
			if (!ctx.Expect (response.Content, Is.Not.Null, "response.Content != null"))
				return false;

			return ctx.Expect (response.Content.Length, Is.GreaterThan (0), "response.Content.Length");
		}

		protected override Task<HttpOperation> RunSecondary (
			TestContext ctx, CancellationToken cancellationToken)
		{
			return Task.Run (() => StartSecond (ctx, cancellationToken));
		}

		HttpOperation StartSecond (
			TestContext ctx, CancellationToken cancellationToken)
		{
			var operation = CreateOperation (
				ctx, InstrumentationOperationType.Parallel,
				OperationFlags);
			operation.Start (ctx, cancellationToken);
			return operation;
		}
	}
}
