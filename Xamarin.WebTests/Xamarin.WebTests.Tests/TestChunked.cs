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
	using TestRunners;
	using Portable;
	using Providers;
	using Features;

	[AsyncTestFixture (Timeout = 5000)]
	public class TestChunked
	{
		[Martin]
		[AsyncTest]
		public Task IncompleChunk (TestContext ctx, CancellationToken cancellationToken, [HttpServer] HttpServer server, bool sendAsync)
		{
			var handler = new GetHandler ("incomplete-chunk", new IncompleteChunkContent ());
			return TestRunner.RunTraditional (ctx, server, handler, cancellationToken, sendAsync, HttpStatusCode.OK, WebExceptionStatus.FailedToReadResponseContent);
		}

		class IncompleteChunkContent : HttpContent
		{
			#region implemented abstract members of HttpContent
			public override string AsString ()
			{
				return "AAAA";
			}
			public override void AddHeadersTo (HttpMessage message)
			{
				message.SetHeader ("Transfer-Encoding", "chunked");
				message.SetHeader ("Content-Type", "text/plain");
			}
			public override async Task WriteToAsync (StreamWriter writer)
			{
				writer.AutoFlush = true;
				await writer.WriteAsync ("4\r\n");
				await writer.WriteAsync ("AAAA\r\n");
			}
			#endregion
		}
	}
}

