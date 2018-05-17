//
// ConnectionProviderFactory.cs
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
using System.Net;
using System.Linq;
using System.Threading;
using System.Collections.Generic;
using Xamarin.AsyncTests;

namespace Xamarin.WebTests.ConnectionFramework
{
	public sealed class ConnectionProviderFactory : ISingletonInstance
	{
		public const string BoringTlsID = "432d18c9-9348-4b90-bfbf-9f2a10e1f15b";
		public const string LegacyTlsID = "809e77d5-56cc-4da8-b9f0-45e65ba9cceb";

		// Mobile only
		const string AppleTlsID = "981af8af-a3a3-419a-9f01-a518e3a17c1c";

		public static readonly Guid BoringTlsGuid = new Guid (BoringTlsID);
		public static readonly Guid LegacyTlsGuid = new Guid (LegacyTlsID);
		public static readonly Guid AppleTlsGuid = new Guid (AppleTlsID);

		const ConnectionProviderFlags LegacyFlags = ConnectionProviderFlags.SupportsSslStream | ConnectionProviderFlags.SupportsHttp;
		const ConnectionProviderFlags AppleTlsFlags = ConnectionProviderFlags.SupportsSslStream | ConnectionProviderFlags.SupportsHttp |
			ConnectionProviderFlags.SupportsTls12 | ConnectionProviderFlags.SupportsTrustedRoots;
		const ConnectionProviderFlags BoringTlsFlags = ConnectionProviderFlags.SupportsSslStream | ConnectionProviderFlags.SupportsHttp |
			ConnectionProviderFlags.SupportsTls12 | ConnectionProviderFlags.SupportsTrustedRoots;

		readonly Dictionary<ConnectionProviderType,ConnectionProvider> providers;
		readonly DotNetSslStreamProvider dotNetSslStreamProvider;
		readonly DotNetConnectionProvider defaultConnectionProvider;
		readonly ManualConnectionProvider manualConnectionProvider;
		IConnectionFrameworkSetup frameworkSetup;
		static object syncRoot = new object ();
		bool initialized;

		public ConnectionProviderFactory ()
		{
			providers = new Dictionary<ConnectionProviderType,ConnectionProvider> ();
			dotNetSslStreamProvider = new DotNetSslStreamProvider ();

			defaultConnectionProvider = new DotNetConnectionProvider (this, ConnectionProviderType.DotNet, dotNetSslStreamProvider);
			Install (defaultConnectionProvider);

			manualConnectionProvider = new ManualConnectionProvider (this, ConnectionProviderFlags.IsExplicit);
			Install (manualConnectionProvider);

			Initialize ();
		}

		public bool IsSupported (ConnectionProviderType type)
		{
			lock (syncRoot) {
				Initialize ();
				return providers.ContainsKey (type);
			}
		}

		public ConnectionProviderFlags GetProviderFlags (ConnectionProviderType type)
		{
			lock (syncRoot) {
				Initialize ();
				ConnectionProvider provider;
				if (!providers.TryGetValue (type, out provider))
					return ConnectionProviderFlags.None;
				return provider.Flags;
			}
		}

		public bool IsExplicit (ConnectionProviderType type)
		{
			var flags = GetProviderFlags (type);
			return (flags & ConnectionProviderFlags.IsExplicit) != 0;
		}

		public IEnumerable<ConnectionProviderType> GetProviderTypes ()
		{
			lock (syncRoot) {
				Initialize ();
				return providers.Keys;
			}
		}

		public IEnumerable<ConnectionProvider> GetProviders (Func<ConnectionProvider,bool> filter = null)
		{
			lock (syncRoot) {
				Initialize ();
				return providers.Values.Where (p => filter != null ? filter (p) : !IsExplicit (p.Type));
			}
		}

		public ConnectionProvider GetProvider (ConnectionProviderType type)
		{
			lock (syncRoot) {
				Initialize ();
				return providers [type];
			}
		}

		public void Initialize ()
		{
			lock (syncRoot) {
				if (initialized)
					return;

				frameworkSetup = DependencyInjector.Get<IConnectionFrameworkSetup> ();
				frameworkSetup.Initialize (this);

				initialized = true;
			}
		}

		public void Install (ConnectionProvider provider)
		{
			lock (syncRoot) {
				if (initialized)
					throw new InvalidOperationException ();
				providers.Add (provider.Type, provider);
			}
		}

		public ISslStreamProvider DefaultSslStreamProvider {
			get {
				Initialize ();
				return dotNetSslStreamProvider;
			}
		}

		public IConnectionFrameworkSetup FrameworkSetup {
			get {
				Initialize ();
				return frameworkSetup;
			}
		}

		public static ConnectionProviderFlags GetConnectionProviderFlags (ConnectionProviderType type)
		{
			switch (type) {
			case ConnectionProviderType.Legacy:
				return LegacyFlags;
			case ConnectionProviderType.AppleTLS:
				return AppleTlsFlags;
			case ConnectionProviderType.BoringTLS:
				return BoringTlsFlags;
			default:
				throw new NotSupportedException (string.Format ("Unknown TLS Provider: {0}", type));
			}
		}

		public static ConnectionProviderType GetConnectionProviderType (Guid id)
		{
			switch (id.ToString ().ToLowerInvariant ()) {
			case LegacyTlsID:
				return ConnectionProviderType.Legacy;
			case AppleTlsID:
				return ConnectionProviderType.AppleTLS;
			case BoringTlsID:
				return ConnectionProviderType.BoringTLS;
			default:
				throw new NotSupportedException (string.Format ("Unknown TLS Provider: {0}", id));
			}
		}
	}
}

