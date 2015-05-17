//
// PuppyTestRunner.cs
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
using System.Threading;
using System.Threading.Tasks;
using Xamarin.AsyncTests;

namespace Xamarin.WebTests.TestRunners
{
	using HttpHandlers;
	using HttpFramework;

	/*
	 * Used to make request against an actual web server.
	 *
	 */
	public class PuppyTestRunner
	{
		const string PuppyKey = "PuppyURL";

		public static bool IsSupported (SettingsBag settings)
		{
			string value;
			return settings.TryGetValue (PuppyKey, out value);
		}

		public static string GetPuppyURL (TestContext ctx)
		{
			string value;
			if (!ctx.Settings.TryGetValue (PuppyKey, out value))
				throw new InvalidOperationException ();
			ctx.LogMessage ("Got puppy URL: {0}", value);
			return value;
		}

		public Request CreateRequest (TestContext ctx)
		{
			var uri = new Uri (GetPuppyURL (ctx));
			return new TraditionalRequest (uri);
		}

		public async Task Run (TestContext ctx, CancellationToken cancellationToken)
		{
			var request = CreateRequest (ctx);
			var response = await request.SendAsync (ctx, cancellationToken);
			ctx.LogMessage ("PUPPY DONE: {0}", response.IsSuccess);
			if (!response.IsSuccess)
				ctx.LogMessage ("PUPPY ERROR: {0}", response.Error);
			ctx.Expect (response.IsSuccess, "is success");
		}
	}
}

