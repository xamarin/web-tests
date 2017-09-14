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
using Xamarin.AsyncTests.Constraints;

namespace Xamarin.WebTests.ConnectionFramework
{
	using TestFramework;

	public abstract class ConnectionProviderFilter {
		public ConnectionTestFlags Flags {
			get;
		}

		public ConnectionProviderFilter (ConnectionTestFlags flags)
		{
			Flags = flags;
		}

		bool IsClientSupported (ConnectionProvider provider)
		{
			if (HasFlag (ConnectionTestFlags.ManualClient))
				return provider.Type == ConnectionProviderType.Manual;
			if (HasFlag (ConnectionTestFlags.RequireMonoClient) && !provider.HasFlag (ConnectionProviderFlags.SupportsMonoExtensions))
				return false;
			if (HasFlag (ConnectionTestFlags.RequireDotNet) && provider.Type != ConnectionProviderType.DotNet)
				return false;
			if (HasFlag (ConnectionTestFlags.RequireCleanClientShutdown) && !provider.HasFlag (ConnectionProviderFlags.SupportsCleanShutdown))
				return false;

			return true;
		}

		bool IsServerSupported (ConnectionProvider provider)
		{
			if (HasFlag (ConnectionTestFlags.ManualServer))
				return provider.Type == ConnectionProviderType.Manual;
			if (HasFlag (ConnectionTestFlags.RequireMonoServer) && !provider.HasFlag (ConnectionProviderFlags.SupportsMonoExtensions))
				return false;
			if (HasFlag (ConnectionTestFlags.RequireDotNet) && provider.Type != ConnectionProviderType.DotNet)
				return false;
			if (HasFlag (ConnectionTestFlags.RequireCleanServerShutdown) && !provider.HasFlag (ConnectionProviderFlags.SupportsCleanShutdown))
				return false;

			return true;
		}

		static (bool match, bool success, bool wildcard) MatchesFilter (ConnectionProvider provider, string filter)
		{
			if (filter == null)
				return (false, false, false);

			var parts = filter.Split (',');
			foreach (var part in parts) {
				if (part.Equals ("*"))
					return (true, true, true);
				if (string.Equals (provider.Name, part, StringComparison.OrdinalIgnoreCase))
					return (true, true, false);
			}

			return (true, false, false);
		}

		bool HasFlag (ConnectionTestFlags flag)
		{
			return (Flags & flag) == flag;
		}

		bool IsSupported (ConnectionProvider provider)
		{
			if (HasFlag (ConnectionTestFlags.AssumeSupportedByTest))
				return true;
			if (HasFlag (ConnectionTestFlags.RequireSslStream) && !provider.HasFlag (ConnectionProviderFlags.SupportsSslStream))
				return false;
			if (HasFlag (ConnectionTestFlags.RequireHttp) && !provider.HasFlag (ConnectionProviderFlags.SupportsHttp))
				return false;
			if (HasFlag (ConnectionTestFlags.RequireTrustedRoots) && !provider.HasFlag (ConnectionProviderFlags.SupportsTrustedRoots))
				return false;
			if (HasFlag (ConnectionTestFlags.RequireHttpListener) && !provider.HasFlag (ConnectionProviderFlags.SupportsHttpListener))
				return false;
			if (HasFlag (ConnectionTestFlags.RequireClientCertificates) && !provider.HasFlag (ConnectionProviderFlags.SupportsClientCertificates))
				return false;
			if (HasFlag (ConnectionTestFlags.RequireCleanShutdown) && !provider.HasFlag (ConnectionProviderFlags.SupportsCleanShutdown))
				return false;
			if (HasFlag (ConnectionTestFlags.RequireMono) && !provider.HasFlag (ConnectionProviderFlags.SupportsMonoExtensions))
				return false;
			if (HasFlag (ConnectionTestFlags.RequireTls12) && !provider.HasFlag (ConnectionProviderFlags.SupportsTls12))
				return false;
			return true;
		}

		bool IsSupported (TestContext ctx, ConnectionProvider provider, string filter)
		{
			if (!IsSupported (provider))
				return false;

			var (match, success, wildcard) = MatchesFilter (provider, filter);
			if (match) {
				if (!success)
					return false;
				if (wildcard && !HasFlag (ConnectionTestFlags.AllowWildcardMatches) && provider.HasFlag (ConnectionProviderFlags.IsExplicit))
					return false;
				return true;
			}

			if (provider.HasFlag (ConnectionProviderFlags.IsExplicit))
				return false;

			if ((Flags & ConnectionTestFlags.AssumeSupportedByTest) != 0)
				return true;

			return true;
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
			var providers = factory.GetProviders (p => IsSupported (p)).ToList ();
			if (providers.Count == 0)
				return new ClientAndServerProvider[0];

			var supportedClientProviders = providers.Where (p => IsClientSupported (p)).ToList ();
			var supportedServerProviders = providers.Where (p => IsServerSupported (p)).ToList ();

			if (supportedClientProviders.Count == 0 || supportedServerProviders.Count == 0)
				return new ClientAndServerProvider[0];

			var filteredClientProviders = supportedClientProviders.Where (p => IsSupported (ctx, p, clientFilter)).ToList ();
			var filteredServerProviders = supportedServerProviders.Where (p => IsSupported (ctx, p, serverFilter)).ToList ();

			if (filter != null) {
				if (filteredClientProviders.Count == 0)
					ctx.LogMessage ($"WARNING: No TLS Provider matches client filter '{clientFilter}'");
				if (filteredServerProviders.Count == 0)
					ctx.LogMessage ($"WARNING: No TLS Provider matches server filter '{serverFilter}'");
			}

			return ConnectionTestHelper.Join (filteredClientProviders, filteredServerProviders, (c, s) => {
				return Create (c, s);
			});
		}

		public ConnectionProvider GetDefaultServer (TestContext ctx, string filter)
		{
			var supported = GetSupportedProviders (ctx, filter).ToList ();
			ctx.Assert (supported.Count, Is.GreaterThan (0), "need at least one supported provider");
			return supported[0].Server;
		}

		public static ConnectionProviderFilter CreateSimpleFilter (ConnectionTestFlags flags)
		{
			return new SimpleFilter (flags);
		}

		class SimpleFilter : ConnectionProviderFilter {
			public SimpleFilter (ConnectionTestFlags flags) : base (flags)
			{
			}

			protected override ClientAndServerProvider Create (ConnectionProvider client, ConnectionProvider server)
			{
				return new ClientAndServerProvider (client, server);
			}
		}
	}
}

