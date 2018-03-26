//
// CustomHandler.cs
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

namespace Xamarin.WebTests.HttpRequestTests
{
	using HttpFramework;
	using HttpHandlers;
	using TestRunners;

	public class CustomHandler : Handler
	{
		public CustomHandlerFixture Fixture {
			get;
		}

		public HttpRequestTestRunner TestRunner {
			get;
		}

		public CustomHandler (CustomHandlerFixture fixture,
		                      HttpRequestTestRunner runner)
			: base (fixture.FriendlyValue)
		{
			Fixture = fixture;
			TestRunner = runner;
			readyTcs = new TaskCompletionSource<bool> ();
			finishedTcs = new TaskCompletionSource<bool> ();

			Flags = RequestFlags.KeepAlive;

			if (fixture.CloseConnection)
				Flags |= RequestFlags.CloseConnection;
		}

		public override object Clone ()
		{
			return new CustomHandler (Fixture, TestRunner);
		}

		internal Request CurrentRequest => currentRequest;
		Request currentRequest;

		TaskCompletionSource<bool> readyTcs;
		TaskCompletionSource<bool> finishedTcs;

		internal Task WaitUntilReady ()
		{
			return readyTcs.Task;
		}

		internal Task WaitForCompletion ()
		{
			return finishedTcs.Task;
		}

		internal void SetReady ()
		{
			readyTcs.TrySetResult (true);
		}

		internal void SetCompleted ()
		{
			finishedTcs.TrySetResult (true);
		}

		internal void ConfigureRequest (TraditionalRequest request)
		{
			if (Interlocked.CompareExchange (ref currentRequest, request, null) != null)
				throw new InvalidOperationException ();
		}

		protected override Task<HttpResponse> HandleRequest (
			TestContext ctx, HttpOperation operation,
			HttpConnection connection, HttpRequest request,
			RequestFlags effectiveFlags,
			CancellationToken cancellationToken)
		{
			return Fixture.HandleRequest (
				ctx, operation, connection,
				request, this, cancellationToken);
		}

		public override bool CheckResponse (
			TestContext ctx, Response response)
		{
			return Fixture.CheckResponse (ctx, (TraditionalResponse)response);
		}
	}
}
