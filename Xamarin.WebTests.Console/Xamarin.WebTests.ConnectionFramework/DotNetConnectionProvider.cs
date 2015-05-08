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

namespace Xamarin.WebTests.ConnectionFramework
{
	using Providers;
	using Server;

	sealed class DotNetConnectionProvider : ConnectionProvider
	{
		readonly ISslStreamProvider sslStreamProvider;
		readonly IHttpProvider httpProvider;

		public DotNetConnectionProvider (ConnectionProviderFactory factory, ISslStreamProvider sslStreamProvider, IHttpProvider httpProvider)
			: base (factory, ConnectionProviderType.DotNet, ConnectionProviderFlags.SupportsSslStream | ConnectionProviderFlags.SupportsSslStream)
		{
			this.sslStreamProvider = sslStreamProvider;
			this.httpProvider = httpProvider;
		}

		public override bool IsCompatibleWith (ConnectionProviderType type)
		{
			switch (type) {
			case ConnectionProviderType.DotNet:
			case ConnectionProviderType.OpenSsl:
				return true;
			default:
				return false;
			}
		}

		public override IClient CreateClient (ClientParameters parameters)
		{
			return new DotNetClient (parameters, SslStreamProvider);
		}

		public override IServer CreateServer (ServerParameters parameters)
		{
			return new DotNetServer (parameters, SslStreamProvider);
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

