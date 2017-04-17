//
// NetworkTests.cs
//
// Author:
//       Martin Baulig <martin.baulig@xamarin.com>
//
// Copyright (c) 2016 Xamarin Inc. (http://www.xamarin.com)
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
using Xamarin.WebTests.TestFramework;

namespace Xamarin.WebTests.Tests
{
	[Network]
	[AsyncTestFixture]
	public class NetworkTests
	{
		[Tls12]
		[AsyncTest]
		public async Task TestWebClient (TestContext ctx, CancellationToken cancellationToken)
		{
			var wc = new WebClient ();
			var result = await wc.DownloadStringTaskAsync ("https://tlstest.xamdev.com/").ConfigureAwait (false);
			ctx.Assert (result.Length, Is.GreaterThan (0), "result");
		}

		[AsyncTest]
		public async Task TestWebClient10 (TestContext ctx, CancellationToken cancellationToken)
		{
			var wc = new WebClient();
			var result = await wc.DownloadStringTaskAsync("https://tlstest-1.xamdev.com/").ConfigureAwait(false);
			ctx.Assert(result.Length, Is.GreaterThan(0), "result");
		}

		[Tls12]
		[AsyncTest]
		public async Task TestWebClient11 (TestContext ctx, CancellationToken cancellationToken)
		{
			var wc = new WebClient();
			var result = await wc.DownloadStringTaskAsync("https://tlstest-11.xamdev.com/").ConfigureAwait(false);
			ctx.Assert(result.Length, Is.GreaterThan(0), "result");
		}

		[Tls12]
		[AsyncTest]
		public async Task TestWebClient12 (TestContext ctx, CancellationToken cancellationToken)
		{
			var wc = new WebClient();
			var result = await wc.DownloadStringTaskAsync("https://tlstest-12.xamdev.com/").ConfigureAwait(false);
			ctx.Assert(result.Length, Is.GreaterThan(0), "result");
		}
	}
}

