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
using System.Linq;
using System.Collections.Generic;

namespace Xamarin.WebTests.Providers
{
	public abstract class ConnectionProviderFactory
	{
		readonly Dictionary<ConnectionProviderType,ConnectionProvider> providers;

		protected ConnectionProviderFactory ()
		{
			providers = new Dictionary<ConnectionProviderType,ConnectionProvider> ();
		}

		public bool IsSupported (ConnectionProviderType type)
		{
			return providers.ContainsKey (type);
		}

		public ConnectionProviderFlags GetProviderFlags (ConnectionProviderType type)
		{
			ConnectionProvider provider;
			if (!providers.TryGetValue (type, out provider))
				return ConnectionProviderFlags.None;
			return provider.Flags;
		}

		public bool IsExplicit (ConnectionProviderType type)
		{
			var flags = GetProviderFlags (type);
			return (flags & ConnectionProviderFlags.IsExplicit) != 0;
		}

		public IEnumerable<ConnectionProviderType> GetSupportedProviders ()
		{
			return providers.Keys.Where (p => !IsExplicit (p));
		}

		public ConnectionProvider GetProvider (ConnectionProviderType type)
		{
			return providers [type];
		}

		public bool IsCompatible (ConnectionProviderType clientType, ConnectionProviderType serverType)
		{
			ConnectionProvider clientProvider, serverProvider;
			if (!providers.TryGetValue (clientType, out clientProvider))
				return false;
			if (!providers.TryGetValue (serverType, out serverProvider))
				return false;

			if (!clientProvider.IsCompatibleWith (serverType))
				return false;
			if (!serverProvider.IsCompatibleWith (clientType))
				return false;

			return true;
		}

		protected void Install (ConnectionProvider provider)
		{
			providers.Add (provider.Type, provider);
		}

		public abstract IHttpProvider DefaultHttpProvider {
			get;
		}

		public abstract ISslStreamProvider DefaultSslStreamProvider {
			get;
		}
	}
}

