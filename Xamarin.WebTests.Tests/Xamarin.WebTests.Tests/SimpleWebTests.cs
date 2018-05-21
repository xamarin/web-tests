﻿//
// SimpleTests.cs
//
// Author:
//       Martin Baulig <martin.baulig@xamarin.com>
//
// Copyright (c) 2015 Xamarin Inc. (http://www.xamarin.com)
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
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

using Xamarin.AsyncTests;
using Xamarin.AsyncTests.Framework;
using Xamarin.WebTests.TestFramework;
using Xamarin.WebTests.TestAttributes;
using Xamarin.WebTests.HttpFramework;
using Xamarin.WebTests.HttpHandlers;
using Xamarin.WebTests.HttpOperations;

namespace Xamarin.WebTests.Tests
{
	[AttributeUsage (AttributeTargets.Parameter | AttributeTargets.Property, AllowMultiple = false)]
	public class SimpleWebHandlerAttribute : TestParameterAttribute, ITestParameterSource<Handler>
	{
		public SimpleWebHandlerAttribute (string filter = null, TestFlags flags = TestFlags.Browsable)
			: base (filter, flags)
		{
		}

		public IEnumerable<Handler> GetParameters (TestContext ctx, string filter)
		{
			return SimpleWebTests.GetParameters (ctx, filter);
		}
	}

	[AsyncTestFixture]
	public class SimpleWebTests
	{
		public static IEnumerable<Handler> GetParameters (TestContext ctx, string filter)
		{
			yield return new HelloWorldHandler ("Hello World");
		}

		[AsyncTest]
		[HttpServerFlags (HttpServerFlags.SSL)]
		public async Task Run (TestContext ctx, CancellationToken cancellationToken,
		                       HttpServer server, [SimpleWebHandler] Handler handler)
		{
			using (var operation = new TraditionalOperation (server, handler, true))
				await operation.Run (ctx, cancellationToken).ConfigureAwait (false);

		}

		[Tls12]
		[AsyncTest]
		[HttpServerFlags (HttpServerFlags.SSL | HttpServerFlags.ForceTls12)]
		public async Task ForceTls12 (TestContext ctx, CancellationToken cancellationToken,
		                              HttpServer server, [SimpleWebHandler] Handler handler)
		{
			using (var operation = new TraditionalOperation (server, handler, true))
				await operation.Run (ctx, cancellationToken).ConfigureAwait (false);

		}

		[AsyncTest] [New]
		public void EmptyNewCategory ()
		{
		}

		[AsyncTest] [Work]
		public void EmptyWorkCategory ()
		{
		}
	}
}

