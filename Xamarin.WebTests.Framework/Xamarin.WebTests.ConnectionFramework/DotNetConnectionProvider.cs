﻿//
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
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using Xamarin.AsyncTests;
using Xamarin.AsyncTests.Portable;

namespace Xamarin.WebTests.ConnectionFramework
{
	public sealed class DotNetConnectionProvider : ConnectionProvider
	{
		readonly ISslStreamProvider sslStreamProvider;
		readonly ProtocolVersions protocols;

		public DotNetConnectionProvider (ConnectionProviderFactory factory, ConnectionProviderType type, ISslStreamProvider sslStreamProvider)
			: base (factory, type, GetFlags ())
		{
			this.sslStreamProvider = sslStreamProvider;

			protocols = ProtocolVersions.Tls10;
			if ((Flags & ConnectionProviderFlags.SupportsTls12) != 0)
				protocols |= ProtocolVersions.Tls11 | ProtocolVersions.Tls12;
		}

		static bool IsMicrosoftRuntime {
			get { return DependencyInjector.Get<IPortableSupport> ().IsMicrosoftRuntime; }
		}

		static ConnectionProviderFlags GetFlags ()
		{
			var setup = DependencyInjector.Get<IConnectionFrameworkSetup> ();
			var flags = ConnectionProviderFlags.SupportsSslStream | ConnectionProviderFlags.SupportsHttp;
			if (IsMicrosoftRuntime || (flags & ConnectionProviderFlags.SupportsTls12) != 0 || setup.SupportsTls12)
				flags |= ConnectionProviderFlags.SupportsTls12;
			if (IsMicrosoftRuntime)
				flags |= ConnectionProviderFlags.SupportsClientCertificates;
			return flags;
		}

		public override ProtocolVersions SupportedProtocols {
			get { return protocols; }
		}

		public override Connection CreateClient (ConnectionParameters parameters)
		{
			return new DotNetClient (this, parameters, SslStreamProvider);
		}

		public override Connection CreateServer (ConnectionParameters parameters)
		{
			return new DotNetServer (this, parameters, SslStreamProvider);
		}

		protected override ISslStreamProvider GetSslStreamProvider ()
		{
			return sslStreamProvider;
		}

		public static X509CertificateCollection GetClientCertificates (ConnectionParameters parameters)
		{
			if (parameters.ClientCertificates != null)
				return parameters.ClientCertificates;

			if (parameters.ClientCertificate == null)
				return null;

			var clientCertificateCollection = new X509CertificateCollection ();
			var certificate = parameters.ClientCertificate;
			clientCertificateCollection.Add (certificate);

			return clientCertificateCollection;
		}
	}
}

