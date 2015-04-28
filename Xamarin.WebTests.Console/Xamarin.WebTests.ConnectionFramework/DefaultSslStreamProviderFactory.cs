//
// DefaultSslStreamProvider.cs
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
using System.IO;
using System.Net;
using System.Net.Security;
using System.Collections.Generic;
using Xamarin.WebTests.Server;
using System.Security.Authentication;

namespace Xamarin.WebTests.ConnectionFramework
{
	using Portable;
	using Providers;

	class DefaultSslStreamProviderFactory : ISslStreamProviderFactory
	{
		readonly ISslStreamProvider defaultProvider;

		internal DefaultSslStreamProviderFactory ()
		{
			defaultProvider = new DotNetProvider ();
		}

		public bool IsSupported (SslStreamProviderType type)
		{
			return type == SslStreamProviderType.DotNet;
		}

		public IEnumerable<SslStreamProviderType> GetSupportedProviders ()
		{
			yield return SslStreamProviderType.DotNet;
		}

		public ISslStreamProvider GetProvider (SslStreamProviderType type)
		{
			if (type == SslStreamProviderType.DotNet)
				return defaultProvider;
			throw new NotSupportedException ();
		}

		public ISslStreamProvider GetDefaultProvider ()
		{
			return defaultProvider;
		}

		class DotNetProvider : ISslStreamProvider
		{
			public Stream CreateServerStream (Stream stream, IServerCertificate serverCertificate, ICertificateValidator validator, ListenerFlags flags)
			{
				var certificate = CertificateProvider.GetCertificate (serverCertificate);

				var clientCertificateRequired = (flags & ListenerFlags.RequireClientCertificate) != 0;
				var protocols = (SslProtocols)ServicePointManager.SecurityProtocol;

				var sslStream = new SslStream (stream, false, ((CertificateValidator)validator).ValidationCallback);
				sslStream.AuthenticateAsServer (certificate, clientCertificateRequired, protocols, false);

				if (clientCertificateRequired && !sslStream.IsMutuallyAuthenticated)
					throw new WebException ("Not mutually authenticated", System.Net.WebExceptionStatus.TrustFailure);

				return sslStream;
			}
		}
	}
}

