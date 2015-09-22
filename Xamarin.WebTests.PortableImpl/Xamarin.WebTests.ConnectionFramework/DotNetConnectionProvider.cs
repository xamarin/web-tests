//
// DefaultConnectionProvider.cs
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
using Xamarin.AsyncTests;
using Xamarin.AsyncTests.Portable;

namespace Xamarin.WebTests.ConnectionFramework
{
	using Providers;
	using Server;

	sealed class DotNetConnectionProvider : ConnectionProvider
	{
		readonly ISslStreamProvider sslStreamProvider;
		readonly IHttpProvider httpProvider;
		readonly ProtocolVersions protocols;

		public DotNetConnectionProvider (ConnectionProviderFactory factory, ConnectionProviderType type, ISslStreamProvider sslStreamProvider, IHttpProvider httpProvider)
			: base (factory, type, GetFlags ())
		{
			this.sslStreamProvider = sslStreamProvider;
			this.httpProvider = httpProvider;

			protocols = ProtocolVersions.Tls10;
			if ((Flags & ConnectionProviderFlags.SupportsTls12) != 0)
				protocols |= ProtocolVersions.Tls11 | ProtocolVersions.Tls12;
		}

		static bool IsMicrosoftRuntime {
			get { return DependencyInjector.Get<IPortableSupport> ().IsMicrosoftRuntime; }
		}

		static ConnectionProviderFlags GetFlags ()
		{
			var flags = ConnectionProviderFlags.SupportsSslStream | ConnectionProviderFlags.SupportsHttp;
			if (IsMicrosoftRuntime)
				flags |= ConnectionProviderFlags.SupportsTls12 | ConnectionProviderFlags.SupportsAeadCiphers | ConnectionProviderFlags.SupportsEcDheCiphers;
			return flags;
		}

		public override bool IsCompatibleWith (ConnectionProviderType type)
		{
			switch (type) {
			case ConnectionProviderType.DotNet:
				return true;
			case ConnectionProviderType.NewTLS:
				return IsMicrosoftRuntime;
			case ConnectionProviderType.OpenSsl:
				return !IsMicrosoftRuntime;
			case ConnectionProviderType.Manual:
				return true;
			default:
				return false;
			}
		}

		public override ProtocolVersions SupportedProtocols {
			get { return protocols; }
		}

		public override IClient CreateClient (ConnectionParameters parameters)
		{
			return new DotNetClient (this, parameters, SslStreamProvider);
		}

		public override IServer CreateServer (ConnectionParameters parameters)
		{
			return new DotNetServer (this, parameters, SslStreamProvider);
		}

		protected override ISslStreamProvider GetSslStreamProvider ()
		{
			return sslStreamProvider;
		}

		protected override IHttpProvider GetHttpProvider ()
		{
			return httpProvider;
		}
	}
}

