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

namespace Xamarin.WebTests.HttpClientTests
{
	using HttpFramework;
	using HttpHandlers;
	using TestRunners;

	public class CustomHandler : Handler
	{
		public CustomHandlerFixture Fixture {
			get;
		}

		public HttpClientTestRunner TestRunner {
			get;
		}

		public bool IsPrimary {
			get;
		}

		public CustomHandler (CustomHandlerFixture fixture,
		                      HttpClientTestRunner runner,
		                      bool primary)
			: base (fixture.FriendlyValue)
		{
			Fixture = fixture;
			TestRunner = runner;
			IsPrimary = primary;

			Flags = RequestFlags.KeepAlive;

			if (fixture.CloseConnection)
				Flags |= RequestFlags.CloseConnection;
		}

		public IPEndPoint RemoteEndPoint {
			get;
			protected set;
		}

		public override object Clone ()
		{
			return new CustomHandler (Fixture, TestRunner, IsPrimary);
		}

		protected override Task<HttpResponse> HandleRequest (
			TestContext ctx, HttpOperation operation,
			HttpConnection connection, HttpRequest request,
			RequestFlags effectiveFlags,
			CancellationToken cancellationToken)
		{
			if (RemoteEndPoint == null)
				RemoteEndPoint = connection.RemoteEndPoint;

			return Fixture.HandleRequest (
				ctx, operation, connection,
				request, this, cancellationToken);
		}

		public override bool CheckResponse (
			TestContext ctx, Response response)
		{
			return Fixture.CheckResponse (ctx, response);
		}

		public void AssertNotReusingConnection (TestContext ctx, HttpConnection connection)
		{
			var firstHandler = (CustomHandler)TestRunner.PrimaryHandler;
			ctx.LogDebug (2, $"{Fixture.ME}: {this == firstHandler} {RemoteEndPoint}");
			if (this == firstHandler)
				return;
			ctx.Assert (connection.RemoteEndPoint, Is.Not.EqualTo (firstHandler.RemoteEndPoint), "RemoteEndPoint");
		}

		public void AssertReusingConnection (TestContext ctx, HttpConnection connection)
		{
			var firstHandler = (CustomHandler)TestRunner.PrimaryHandler;
			ctx.LogDebug (2, $"{Fixture.ME}: {this == firstHandler} {RemoteEndPoint}");
			if (this == firstHandler)
				return;
			ctx.Assert (connection.RemoteEndPoint, Is.EqualTo (firstHandler.RemoteEndPoint), "RemoteEndPoint");
		}

	}
}
