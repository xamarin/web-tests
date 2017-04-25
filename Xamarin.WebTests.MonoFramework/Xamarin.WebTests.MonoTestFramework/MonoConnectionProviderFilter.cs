//
// MonoConnectionProviderFilter.cs
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
using Xamarin.WebTests.TestFramework;
using Xamarin.WebTests.ConnectionFramework;
using Xamarin.WebTests.MonoConnectionFramework;
using Mono.Security.Interface;

namespace Xamarin.WebTests.MonoTestFramework
{
	public class MonoConnectionProviderFilter : ConnectionProviderFilter
	{
		public MonoConnectionTestCategory Category {
			get;
		}

		public MonoConnectionProviderFilter (MonoConnectionTestCategory category, ConnectionTestFlags flags)
			: base (flags)
		{
			Category = category;
		}
		protected override ClientAndServerProvider Create (ConnectionProvider client, ConnectionProvider server)
		{
			return new MonoConnectionTestProvider (client, server, Category, Flags);
		}

		public override bool IsClientSupported (TestContext ctx, ConnectionProvider provider, string filter)
		{
			if (!IsClientSupported (provider))
				return false;
			if (!provider.HasFlag (ConnectionProviderFlags.SupportsTls12))
				return false;
			return IsSupported (ctx, provider, filter) ?? true;
		}

		public override bool IsServerSupported (TestContext ctx, ConnectionProvider provider, string filter)
		{
			if (!IsServerSupported (provider))
				return false;
			if (!provider.HasFlag (ConnectionProviderFlags.SupportsTls12))
				return false;
			return IsSupported (ctx, provider, filter) ?? true;
		}
	}
}

