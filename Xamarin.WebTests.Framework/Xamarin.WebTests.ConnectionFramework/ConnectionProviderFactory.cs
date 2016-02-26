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
		readonly Dictionary<ConnectionProviderType,ConnectionProvider> providers;
		readonly DotNetSslStreamProvider dotNetSslStreamProvider;
		readonly DotNetConnectionProvider defaultConnectionProvider;
		readonly ManualConnectionProvider manualConnectionProvider;
		IDefaultConnectionSettings defaultSettings;
		ISslStreamProvider defaultSslStreamProvider;
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

		public IEnumerable<ConnectionProviderType> GetProviders ()
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

				defaultSettings = DependencyInjector.GetDefaults<IDefaultConnectionSettings> ();
				if (defaultSettings == null)
					defaultSettings = new DefaultConnectionSettings ();

				var extensions = DependencyInjector.GetCollection<IConnectionProviderFactoryExtension> ();
				foreach (var extension in extensions)
					extension.Initialize (this, defaultSettings);

				defaultSslStreamProvider = defaultSettings.DefaultSslStreamProvider;
				if (defaultSslStreamProvider == null)
					defaultSslStreamProvider = dotNetSslStreamProvider;

				if (defaultSettings.SecurityProtocol != null)
					ServicePointManager.SecurityProtocol = defaultSettings.SecurityProtocol.Value;

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
				return defaultSslStreamProvider;
			}
		}

		public IDefaultConnectionSettings DefaultSettings {
			get {
				Initialize ();
				return defaultSettings;
			}
		}
	}
}

