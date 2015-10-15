//
// TestChunked.cs
//
// Author:
//       Martin Baulig <martin.baulig@xamarin.com>
//
// Copyright (c) 2015 Xamarin, Inc.
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
using System.Collections;
using System.Collections.Generic;

using Xamarin.AsyncTests;
using Xamarin.AsyncTests.Portable;
using Xamarin.AsyncTests.Constraints;

namespace Xamarin.WebTests.Tests
{
	using HttpHandlers;
	using HttpFramework;
	using TestFramework;
	using TestRunners;
	using Portable;
	using Providers;
	using Features;

	[AsyncTestFixture (Timeout = 5000)]
	public class TestChunked
	{
		[AsyncTest]
		public Task Run (TestContext ctx, CancellationToken cancellationToken, [HttpServer] HttpServer server,
			[ChunkContentType] ChunkContentType type, bool sendAsync)
		{
			var runner = new ChunkedTestRunner (server, type, sendAsync);
			return runner.Run (ctx, cancellationToken);
		}

		[AsyncTest]
		public Task ServerErrorTests (TestContext ctx, CancellationToken cancellationToken,
			[HttpServer (ListenerFlags.ExpectException)] HttpServer server,
			[ChunkContentType (ServerError = true)] ChunkContentType type, bool sendAsync)
		{
			var runner = new ChunkedTestRunner (server, type, sendAsync);
			return runner.Run (ctx, cancellationToken);
		}
	}
}

