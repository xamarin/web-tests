﻿//
// CloseIdleConnection.cs
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
	using TestAttributes;
	using TestFramework;
	using HttpFramework;
	using HttpHandlers;
	using TestRunners;

	[HttpServerFlags (HttpServerFlags.RequireNewWebStack)]
	public class CloseIdleConnection : HttpInstrumentationTestFixture
	{
		const int IdleTime = 750;

		ServicePoint servicePoint;

		protected override void ConfigurePrimaryRequest (
			TestContext ctx, InstrumentationOperation operation,
			TraditionalRequest request)
		{
			ctx.Assert (servicePoint, Is.Null, "ServicePoint");
			servicePoint = ServicePointManager.FindServicePoint (request.Uri);
			servicePoint.MaxIdleTime = IdleTime;
			base.ConfigurePrimaryRequest (ctx, operation, request);
		}

		protected override bool ConfigureNetworkStream (
			TestContext ctx, StreamInstrumentation instrumentation)
		{
			return false;
		}

		public override HttpResponse HandleRequest (
			TestContext ctx, InstrumentationOperation operation,
			HttpConnection connection, HttpRequest request)
		{
			return new HttpResponse (HttpStatusCode.OK, ExpectedContent);
		}

		protected override async Task<HttpOperation> RunSecondary (
			TestContext ctx, CancellationToken cancellationToken)
		{
			ctx.LogDebug (5, $"{ME}: active connections: {servicePoint.CurrentConnections}");
			await Task.Delay ((int)(IdleTime * 2.5)).ConfigureAwait (false);
			ctx.LogDebug (5, $"{ME}: active connections #1: {servicePoint.CurrentConnections}");
			ctx.Assert (servicePoint.CurrentConnections, Is.EqualTo (0), "current connections");
			return null;
		}
	}
}
