//
// CloseConnectionGroup.cs
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
using System.Net.Sockets;
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

	[NotWorking] // This is causing problems on Jenkins
	[HttpServerFlags (HttpServerFlags.RequireInstrumentation)]
	public class CloseConnectionGroup : HttpInstrumentationTestFixture
	{
		const int IdleTime = 250;
		const int ExtraDelay = 150;

		string groupName;
		ServicePoint servicePoint;
		Task closeTask;
		bool disposed;
		Socket socket;

		protected override void ConfigurePrimaryRequest (TestContext ctx, InstrumentationOperation operation, TraditionalRequest request)
		{
			request.RequestExt.ConnectionGroupName = groupName = $"Custom{ctx.GetUniqueId ()}";
			ctx.Assert (servicePoint, Is.Null, "ServicePoint");
			servicePoint = ServicePointManager.FindServicePoint (request.Uri);
			servicePoint.MaxIdleTime = IdleTime;
			base.ConfigurePrimaryRequest (ctx, operation, request);
		}

		protected override bool ConfigureNetworkStream (TestContext ctx, StreamInstrumentation instrumentation)
		{
			socket = instrumentation.Socket;
			return base.ConfigureNetworkStream (ctx, instrumentation);
		}

		void WaitForClose ()
		{
			// Wait until the connection has been closed.
			while (!disposed && socket.Connected) {
				try {
					socket.Poll (2500000, SelectMode.SelectRead);
				} catch {
					break;
				}
			}
		}

		public override HttpResponse HandleRequest (
			TestContext ctx, InstrumentationOperation operation, HttpConnection connection, HttpRequest request)
		{
			// Once we get here, everything has been read from the connection.
			closeTask = Task.Run (() => WaitForClose ());
			return new HttpResponse (HttpStatusCode.OK, ExpectedContent) { CloseConnection = true };
		}

		protected override async Task<Response> Run (TestContext ctx, Request request, CancellationToken cancellationToken)
		{
			var response = await base.Run (ctx, request, cancellationToken).ConfigureAwait (false);

			var delayTask = Task.Delay (TimeSpan.FromSeconds (5));
			var ret = await Task.WhenAny (delayTask, closeTask).ConfigureAwait (false);

			disposed = true;
			ctx.Assert (ret == closeTask, $"{nameof (WaitForClose)} finished.");

			// The connection has already been closed, just give it a little extra time.
			await Task.Delay (ExtraDelay).ConfigureAwait (false);

			var ret2 = servicePoint.CloseConnectionGroup (groupName);
			ctx.Assert (ret2, "ServicePoint.CloseConnectionGroup().");

			return response;
		}
	}
}
