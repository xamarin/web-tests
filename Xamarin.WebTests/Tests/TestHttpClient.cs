//
// TestHttpClient.cs
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
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;

using Xamarin.AsyncTests;

namespace Xamarin.WebTests
{
	using Server;
	using Runners;
	using Handlers;
	using Framework;

	[RecentlyFixed]
	[AsyncTestFixture]
	public class TestHttpClient : ITestHost<TestRunner>, ITestParameterSource<Handler>
	{
		[TestParameter (typeof (WebTestFeatures.SelectSSL), null, TestFlags.Hidden)]
		public bool UseSSL {
			get; set;
		}

		public TestRunner CreateInstance (TestContext context)
		{
			return new HttpTestRunner { UseSSL = UseSSL };
		}

		public IEnumerable<Handler> GetParameters (TestContext ctx, string filter)
		{
			yield return new HttpClientHandler {
				Operation = HttpClientOperation.GetString, Description = "Get string"
			};
			yield return new HttpClientHandler {
				Operation = HttpClientOperation.PostString,
				Content = new StringContent ("Hello World!"),
				Description = "Post string"
			};
			yield return new HttpClientHandler {
				Operation = HttpClientOperation.PostString,
				Content = new StringContent ("Hello World!"),
				ReturnContent = new StringContent ("Returned body"),
				Description = "Post string with result"
			};
			yield return new HttpClientHandler {
				Operation = HttpClientOperation.PostString,
				Content = new StringContent ("Hello World!"),
				ReturnContent = new Bug20583Content (),
				Description = "Bug #20583"
			};
		}

		[AsyncTest]
		public Task Run (InvocationContext ctx, CancellationToken cancellationToken,
			[TestHost (typeof (TestHttpClient))] TestRunner runner, [TestParameter] Handler handler)
		{
			return runner.Run (ctx, handler, cancellationToken);
		}

		class Bug20583Content : HttpContent
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
			public override void WriteTo (StreamWriter writer)
			{
				writer.AutoFlush = true;
				Thread.Sleep (500);
				writer.Write ("0");
				Thread.Sleep (500);
				writer.Write ("4\r\n");
				writer.Write ("AAAA\r\n0\r\n\r\n");
			}
			#endregion
		}


	}
}

