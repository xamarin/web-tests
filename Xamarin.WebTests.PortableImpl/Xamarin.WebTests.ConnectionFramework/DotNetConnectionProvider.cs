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

		static ConnectionProviderFlags GetFlags ()
		{
			var flags = ConnectionProviderFlags.SupportsSslStream | ConnectionProviderFlags.SupportsHttp;
			var support = DependencyInjector.Get<IPortableSupport> ();
			if (support.IsMicrosoftRuntime)
				flags |= ConnectionProviderFlags.IsNewTls | ConnectionProviderFlags.SupportsTls12;
			return flags;
		}

		public override bool IsCompatibleWith (ConnectionProviderType type)
		{
			var support = DependencyInjector.Get<IPortableSupport> ();

			switch (type) {
			case ConnectionProviderType.PlatformDefault:
			case ConnectionProviderType.DotNet:
				return true;
			case ConnectionProviderType.NewTLS:
				return support.IsMicrosoftRuntime;
			case ConnectionProviderType.OpenSsl:
				return !support.IsMicrosoftRuntime;
			default:
				return false;
			}
		}

		public override ProtocolVersions SupportedProtocols {
			get { return protocols; }
		}

		public override IClient CreateClient (ClientParameters parameters)
		{
			return new DotNetClient (this, parameters, SslStreamProvider);
		}

		public override IServer CreateServer (ServerParameters parameters)
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

