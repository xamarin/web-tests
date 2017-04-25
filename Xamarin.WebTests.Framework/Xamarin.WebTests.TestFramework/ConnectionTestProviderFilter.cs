//
// ConnectionTestProviderFilter.cs
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
using Xamarin.AsyncTests;

namespace Xamarin.WebTests.TestFramework
{
	using ConnectionFramework;
	using TestRunners;

	public class ConnectionTestProviderFilter : ConnectionProviderFilter
	{
		public ConnectionTestCategory Category {
			get;
		}

		public ConnectionTestProviderFilter (ConnectionTestCategory category, ConnectionTestFlags flags)
			: base (flags)
		{
			Category = category;
		}

		protected override ClientAndServerProvider Create (ConnectionProvider client, ConnectionProvider server)
		{
			return new ConnectionTestProvider (client, server, Category, Flags);
		}

		public override bool IsClientSupported (TestContext ctx, ConnectionProvider provider, string filter)
		{
			if (!IsClientSupported (provider))
				return false;

			var supported = IsSupported (ctx, provider, filter);
			if (supported != null)
				return supported.Value;

			return ConnectionTestRunner.IsSupported (ctx, Category, provider);
		}

		public override bool IsServerSupported (TestContext ctx, ConnectionProvider provider, string filter)
		{
			if (!IsServerSupported (provider))
				return false;

			var supported = IsSupported (ctx, provider, filter);
			if (supported != null)
				return supported.Value;

			return ConnectionTestRunner.IsSupported (ctx, Category, provider);
		}
	}
}

