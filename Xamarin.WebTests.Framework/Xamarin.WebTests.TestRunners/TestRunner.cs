//
// HttpTest.cs
//
// Author:
//       Martin Baulig <martin.baulig@xamarin.com>
//
// Copyright (c) 2014 Xamarin Inc. (http://www.xamarin.com)
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
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;
using Xamarin.AsyncTests;
using Xamarin.AsyncTests.Constraints;

namespace Xamarin.WebTests.TestRunners
{
	using HttpFramework;
	using HttpHandlers;
	using HttpOperations;

	public static class TestRunner
	{
#if FIXME
		public async Task RunExternal (
			TestContext ctx, CancellationToken cancellationToken, Uri uri,
			HttpStatusCode expectedStatus = HttpStatusCode.OK,
			WebExceptionStatus expectedError = WebExceptionStatus.Success)
		{
			Debug (ctx, 1, "RUN");

			var request = CreateRequest (ctx, uri);

			var response = await Server.RunWithContext (ctx, (token) => RunInner (ctx, request, token), cancellationToken);

			CheckResponse (ctx, response, cancellationToken, expectedStatus, expectedError);
		}
#endif

		public static async Task RunTraditional (
			TestContext ctx, HttpServer server, Handler handler,
			CancellationToken cancellationToken, bool sendAsync = false,
			HttpOperationFlags flags = HttpOperationFlags.None,
			HttpStatusCode expectedStatus = HttpStatusCode.OK,
			WebExceptionStatus expectedError = WebExceptionStatus.Success)
		{
			var operation = new TraditionalOperation (server, handler, sendAsync, flags, expectedStatus, expectedError);
			operation.Start (ctx, cancellationToken);
			await operation.WaitForCompletion ().ConfigureAwait (false);
		}
	}
}

