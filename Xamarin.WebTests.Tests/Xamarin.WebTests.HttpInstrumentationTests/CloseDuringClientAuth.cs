//
// CloseDuringClientAuth.cs
//
// Author:
//       Martin Baulig <mabaul@microsoft.com>
//
// Copyright (c) 2019 Microsoft Corporation
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
using Xamarin.AsyncTests.Constraints;

namespace Xamarin.WebTests.HttpInstrumentationTests
{
	using ConnectionFramework;
	using HttpFramework;
	using HttpHandlers;
	using TestRunners;

	// Test case for https://github.com/mono/mono/pull/17270
	public class CloseDuringClientAuth : HttpInstrumentationTestFixture
	{
		public override bool HasRequestBody => false;

		public override HttpOperationFlags OperationFlags => HttpOperationFlags.ServerAbortsHandshake | HttpOperationFlags.AbortAfterClientExits;

		public override HttpStatusCode ExpectedStatus => HttpStatusCode.InternalServerError;

		public override WebExceptionStatus ExpectedError => WebExceptionStatus.ConnectFailure;

		ServicePoint servicePoint;

		protected override Request CreateRequest (TestContext ctx, InstrumentationOperation operation, Uri uri)
		{
			return new InstrumentationRequest (this, uri);
		}

		protected override void ConfigurePrimaryRequest (TestContext ctx, InstrumentationOperation operation, TraditionalRequest request)
		{
			servicePoint = ServicePointManager.FindServicePoint (request.Uri);
			// 1.) save the ServicePoint for later.
			base.ConfigurePrimaryRequest (ctx, operation, request);
		}

		protected override async Task<Response> SendRequest (TestContext ctx, TraditionalRequest request, CancellationToken cancellationToken)
		{
			var response = await base.SendRequest (ctx, request, cancellationToken).ConfigureAwait (false);
			// No need to check the WebException.Status - the base class already does that for us.
			ctx.Expect (response.Error, Is.InstanceOf<WebException> (), "response.Error");
			ctx.Expect (response.Error.InnerException, Is.InstanceOf<ObjectDisposedException> (), "response.Error.InnerException");

			/*
			 * The connection was forcibly closed during SslStream.AuthenticateAsClientAsync(), resulting in
			 * MonoTlsStream.Dispose() being called via WebConnection.CloseSocket().
			 * Prior to https://github.com/mono/mono/pull/17270, MonoTlsStream.CreateConnection()'s exception
			 * block would NRE on the `sslStream.Dispose ()` call.  That exception was caught and wrapped up
			 * inside a WebException - which made this issue hard to find.
			 */
			return response;
		}

		protected override bool ConfigureNetworkStream (
			TestContext ctx, StreamInstrumentation instrumentation)
		{
			// 2.) I had a breakpoint here during my investigations.
			// Need to override this and return true to setup instrumentation.
			return true;
		}

		protected override async Task<bool> PrimaryReadHandler (
			TestContext ctx, InstrumentationOperation operation,
			byte[] buffer, int offset, int size, int bytesRead,
			CancellationToken cancellationToken)
		{
			/*
			 *
			 * Server-side instrumentation callback.  This is guaranteed to
			 * happen while MonoTlsStream.CreateStream() waits on
			 * SslStream.AuthenticateAsClientAsync().
			 * 
			 * 3.) Do not call 'operation.Request.Abort ()' here because that will
			 * only queue the ServicePoint for cleanup, but not run the scheduler.
			 *
			 * Calling ServicePoint.CloseConnectionGroup() forces a scheduler run,
			 * resulting in WebConnection.CloseSocket() and MonoTlsStream.Dispose()
			 * being called.
			 * 
			 */
			servicePoint.CloseConnectionGroup (null);

			/*
			 * Okay, the socket has now been closed and the ServicePointScheduler ran.
			 * 
			 * Now wait here for the client request to finish - that is the call to
			 * `base.SendRequest()` above will finish prior to this WaitForCompletion().
			 * 
			 * This is not strictly required, but it ensures that client and server side
			 * exceptions will not happen at the same time - which will ease debugging
			 * in the IDE a little bit.
			 */

			await operation.WaitForCompletion ().ConfigureAwait (false);
			return false;
		}
	}
}
