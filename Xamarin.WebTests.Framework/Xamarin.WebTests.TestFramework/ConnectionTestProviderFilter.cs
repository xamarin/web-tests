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
			private set;
		}

		public ConnectionTestFlags Flags {
			get;
			private set;
		}

		public ConnectionTestProviderFilter (ConnectionTestCategory category, ConnectionTestFlags flags)
		{
			Category = category;
			Flags = flags;
		}

		protected override ClientAndServerProvider Create (ConnectionProvider client, ConnectionProvider server)
		{
			return new ConnectionTestProvider (client, server, Category, Flags);
		}

		public override bool IsClientSupported (TestContext ctx, ConnectionProvider provider, string filter)
		{
			if ((Flags & ConnectionTestFlags.ManualClient) != 0)
				return provider.Type == ConnectionProviderType.Manual;
			return IsSupported (ctx, provider, filter);
		}

		public override bool IsServerSupported (TestContext ctx, ConnectionProvider provider, string filter)
		{
			if ((Flags & ConnectionTestFlags.ManualServer) != 0)
				return provider.Type == ConnectionProviderType.Manual;
			return IsSupported (ctx, provider, filter);
		}

		bool IsSupported (TestContext ctx, ConnectionProvider provider, string filter)
		{
			var supportsSslStream = (provider.Flags & ConnectionProviderFlags.SupportsSslStream) != 0;
			var supportsHttps = (provider.Flags & ConnectionProviderFlags.SupportsHttp) != 0;
			var supportsTrustedRoots = (provider.Flags & ConnectionProviderFlags.SupportsTrustedRoots) != 0;

			if ((Flags & ConnectionTestFlags.RequireSslStream) != 0 && !supportsSslStream)
				return false;
			if ((Flags & ConnectionTestFlags.RequireHttp) != 0 && !supportsHttps)
				return false;
			if ((Flags & ConnectionTestFlags.RequireTrustedRoots) != 0 && !supportsTrustedRoots)
				return false;

			var match = MatchesFilter (provider, filter);
			if (match != null)
				return match.Value;
			if ((provider.Flags & ConnectionProviderFlags.IsExplicit) != 0)
				return false;

			if ((Flags & ConnectionTestFlags.AssumeSupportedByTest) != 0)
				return true;

			return ConnectionTestRunner.IsSupported (ctx, Category, provider);
		}
	}
}

