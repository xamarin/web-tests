//
// ConnectionProviderFilter.cs
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
using System.Linq;
using System.Collections.Generic;
using Xamarin.AsyncTests;

namespace Xamarin.WebTests.ConnectionFramework
{
	using TestFramework;
	using Providers;

	public abstract class ConnectionProviderFilter
	{
		public abstract bool IsClientSupported (TestContext ctx, ConnectionProvider provider, string filter = null);

		public abstract bool IsServerSupported (TestContext ctx, ConnectionProvider provider, string filter = null);

		protected static bool? MatchesFilter (ConnectionProvider provider, string filter)
		{
			if (filter == null)
				return null;

			var parts = filter.Split (',');
			foreach (var part in parts) {
				if (string.Equals (provider.Name, part, StringComparison.OrdinalIgnoreCase))
					return true;
			}

			return false;
		}

		protected abstract ClientAndServerProvider Create (ConnectionProvider client, ConnectionProvider server);

		public IEnumerable<ClientAndServerProvider> GetSupportedProviders (TestContext ctx, string filter)
		{
			string clientFilter, serverFilter;
			if (filter == null)
				clientFilter = serverFilter = null;
			else {
				int pos = filter.IndexOf (':');
				if (pos < 0)
					clientFilter = serverFilter = filter;
				else {
					clientFilter = filter.Substring (0, pos);
					serverFilter = filter.Substring (pos + 1);
				}
			}

			var factory = DependencyInjector.Get<ConnectionProviderFactory> ();

			var clientProviders = factory.GetProviders (p => IsClientSupported (ctx, p, clientFilter));
			var serverProviders = factory.GetProviders (p => IsServerSupported (ctx, p, serverFilter));

			return ConnectionTestHelper.Join (clientProviders, serverProviders, (c, s) => {
				if (!c.IsCompatibleWith (s.Type))
					return null;
				if (!s.IsCompatibleWith (c.Type))
					return null;
				return Create (c, s);
			});
		}
	}
}

