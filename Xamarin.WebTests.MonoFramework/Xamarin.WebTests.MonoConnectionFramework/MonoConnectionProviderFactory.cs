//
// MonoConnectionProviderFactory.cs
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
using System.Threading;
using Xamarin.AsyncTests;
using Xamarin.WebTests.ConnectionFramework;
using Mono.Security.Interface;

namespace Xamarin.WebTests.MonoConnectionFramework
{
	public class MonoConnectionProviderFactory : IConnectionProviderFactoryExtension, ISingletonInstance
	{
		int initialized;

		public const string NewTlsID = "e5ff34f1-8b7a-4aa6-aff9-24719d709693";
		public const string OldTlsID = "cf8baa0d-c6ed-40ae-b512-dec8d097e9af";
		public const string BoringTlsID = "432d18c9-9348-4b90-bfbf-9f2a10e1f15b";
		public const string DefaultTlsID = "809e77d5-56cc-4da8-b9f0-45e65ba9cceb";

		// Mobile only
		const string MobileOldTlsID = "97d31751-d0b3-4707-99f7-a6456b972a19";
		const string AppleTlsID = "981af8af-a3a3-419a-9f01-a518e3a17c1c";

		public static readonly Guid NewTlsGuid = new Guid (NewTlsID);
		public static readonly Guid BoringTlsGuid = new Guid (BoringTlsID);

		const ConnectionProviderFlags OldTlsFlags = ConnectionProviderFlags.SupportsSslStream | ConnectionProviderFlags.SupportsHttp;
		const ConnectionProviderFlags NewTlsFlags = OldTlsFlags | ConnectionProviderFlags.SupportsTls12 |
			ConnectionProviderFlags.SupportsAeadCiphers | // ConnectionProviderFlags.SupportsEcDheCiphers |
			ConnectionProviderFlags.SupportsClientCertificates;
		const ConnectionProviderFlags AppleTlsFlags = ConnectionProviderFlags.SupportsSslStream | ConnectionProviderFlags.SupportsHttp |
			ConnectionProviderFlags.SupportsTls12 | ConnectionProviderFlags.SupportsAeadCiphers | ConnectionProviderFlags.SupportsEcDheCiphers |
			ConnectionProviderFlags.SupportsClientCertificates | ConnectionProviderFlags.OverridesCipherSelection | ConnectionProviderFlags.SupportsTrustedRoots;
		const ConnectionProviderFlags BoringTlsFlags = ConnectionProviderFlags.SupportsSslStream | ConnectionProviderFlags.SupportsHttp |
			ConnectionProviderFlags.SupportsTls12 | ConnectionProviderFlags.SupportsAeadCiphers | ConnectionProviderFlags.SupportsEcDheCiphers |
			ConnectionProviderFlags.SupportsClientCertificates | ConnectionProviderFlags.OverridesCipherSelection | ConnectionProviderFlags.SupportsTrustedRoots;

		internal MonoConnectionProviderFactory ()
		{
		}

		public void Initialize (ConnectionProviderFactory factory, IDefaultConnectionSettings settings)
		{
			if (Interlocked.Exchange (ref initialized, 1) != 0)
				throw new InvalidOperationException ();

			var providers = DependencyInjector.GetCollection<IMonoTlsProviderFactory> ();
			if (providers.Count == 0) {
				var provider = MonoTlsProviderFactory.GetDefaultProvider ();
				if (provider != null)
					AddProvider (factory, settings, new DefaultProvider (provider));
			}

			foreach (var provider in providers) {
				AddProvider (factory, settings, provider);
			}
		}

		void AddProvider (ConnectionProviderFactory factory, IDefaultConnectionSettings settings, IMonoTlsProviderFactory provider)
		{
			var monoProvider = new MonoConnectionProvider (factory, provider.ConnectionProviderType, provider.ConnectionProviderFlags, provider.Name, provider.Provider);
			factory.Install (monoProvider);

			if (settings.InstallTlsProvider != null && provider.Provider.ID == settings.InstallTlsProvider.Value)
				MonoTlsProviderFactory.SetDefaultProvider (provider.Provider.Name);
		}

		class DefaultProvider : IMonoTlsProviderFactory
		{
			MonoTlsProvider provider;
			ConnectionProviderType type;
			ConnectionProviderFlags flags;

			public DefaultProvider (MonoTlsProvider provider)
			{
				this.provider = provider;
				type = GetConnectionProviderType (provider.ID);
				flags = GetConnectionProviderFlags (type);
			}

			public string Name {
				get { return type.ToString (); }
			}
			public MonoTlsProvider Provider {
				get { return provider; }
			}
			public ConnectionProviderType ConnectionProviderType {
				get { return type; }
			}
			public ConnectionProviderFlags ConnectionProviderFlags {
				get { return flags; }
			}
		}

		static ConnectionProviderFlags GetConnectionProviderFlags (ConnectionProviderType type)
		{
			switch (type) {
			case ConnectionProviderType.OldTLS:
				return OldTlsFlags;
			case ConnectionProviderType.NewTLS:
				return NewTlsFlags;
			case ConnectionProviderType.AppleTLS:
				return AppleTlsFlags;
			case ConnectionProviderType.BoringTLS:
				return BoringTlsFlags;
			default:
				throw new NotSupportedException (string.Format ("Unknown TLS Provider: {0}", type));
			}
		}

		static ConnectionProviderType GetConnectionProviderType (Guid id)
		{
			switch (id.ToString ().ToLowerInvariant ()) {
			case DefaultTlsID:
			case OldTlsID:
			case MobileOldTlsID:
				return ConnectionProviderType.OldTLS;
			case NewTlsID:
				return ConnectionProviderType.NewTLS;
			case AppleTlsID:
				return ConnectionProviderType.AppleTLS;
			case BoringTlsID:
				return ConnectionProviderType.BoringTLS;
			default:
				throw new NotSupportedException (string.Format ("Unknown TLS Provider: {0}", id));
			}
		}

		public void RegisterProvider (IMonoTlsProviderFactory factory)
		{
			if (initialized != 0)
				throw new InvalidOperationException ();

			DependencyInjector.RegisterCollection<IMonoTlsProviderFactory> (factory);
		}

		public void RegisterProvider (string name, MonoTlsProvider provider, ConnectionProviderType type, ConnectionProviderFlags flags)
		{
			RegisterProvider (new FactoryImpl (name, provider, type, flags));
		}

		class FactoryImpl : IMonoTlsProviderFactory
		{
			public string Name {
				get;
				private set;
			}

			public MonoTlsProvider Provider {
				get;
				private set;
			}

			public ConnectionProviderType ConnectionProviderType {
				get;
				private set;
			}

			public ConnectionProviderFlags ConnectionProviderFlags {
				get;
				private set;
			}

			public FactoryImpl (string name, MonoTlsProvider provider, ConnectionProviderType type, ConnectionProviderFlags flags)
			{
				Name = name;
				Provider = provider;
				ConnectionProviderType = type;
				ConnectionProviderFlags = flags;
			}
		}
	}
}

