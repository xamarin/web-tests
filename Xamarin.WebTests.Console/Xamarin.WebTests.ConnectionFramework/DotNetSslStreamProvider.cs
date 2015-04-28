//
// DotNetSslStreamProvider.cs
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
using System.Security.Authentication;
using System.Threading;
using System.Threading.Tasks;
using System.Security.Cryptography.X509Certificates;

namespace Xamarin.WebTests.ConnectionFramework
{
	using Providers;
	using Portable;
	using Server;

	class DotNetSslStreamProvider : ISslStreamProvider
	{
		static SslProtocols GetSslProtocol ()
		{
			return (SslProtocols)ServicePointManager.SecurityProtocol;
		}

		static RemoteCertificateValidationCallback GetValidationCallback (ICertificateValidator validator)
		{
			if (validator == null)
				return null;
			return ((CertificateValidator)validator).ValidationCallback;
		}

		static X509Certificate2Collection GetCertificates (ICollection<IClientCertificate> clientCertificates)
		{
			if (clientCertificates == null)
				return null;

			var clientCertificateCollection = new X509Certificate2Collection ();
			foreach (var clientCertificate in clientCertificates) {
				var certificate = (X509Certificate2)CertificateProvider.GetCertificate (clientCertificate);
				clientCertificateCollection.Add (certificate);
			}

			return clientCertificateCollection;
		}

		public Stream CreateServerStream (
			Stream stream, IServerCertificate serverCertificate, ICertificateValidator validator, ListenerFlags flags)
		{
			var certificate = CertificateProvider.GetCertificate (serverCertificate);

			var clientCertificateRequired = (flags & ListenerFlags.RequireClientCertificate) != 0;

			var sslStream = new SslStream (stream, false, GetValidationCallback (validator));
			sslStream.AuthenticateAsServer (certificate, clientCertificateRequired, GetSslProtocol (), false);

			if (clientCertificateRequired && !sslStream.IsMutuallyAuthenticated)
				throw new WebException ("Not mutually authenticated", System.Net.WebExceptionStatus.TrustFailure);

			return sslStream;
		}

		public async Task<Stream> CreateServerStreamAsync (
			Stream stream, IServerCertificate serverCertificate, ICertificateValidator validator,
			ListenerFlags flags, CancellationToken cancellationToken)
		{
			var certificate = CertificateProvider.GetCertificate (serverCertificate);

			var clientCertificateRequired = (flags & ListenerFlags.RequireClientCertificate) != 0;

			var sslStream = new SslStream (stream, false, GetValidationCallback (validator));
			await sslStream.AuthenticateAsServerAsync (certificate, clientCertificateRequired, GetSslProtocol (), false);

			if (clientCertificateRequired && !sslStream.IsMutuallyAuthenticated)
				throw new WebException ("Not mutually authenticated", System.Net.WebExceptionStatus.TrustFailure);

			return sslStream;
		}

		public async Task<Stream> CreateClientStreamAsync (
			Stream stream, string targetHost, ICollection<IClientCertificate> clientCertificates,
			ICertificateValidator validator, ListenerFlags flags, CancellationToken cancellationToken)
		{
			var server = new SslStream (stream, false, GetValidationCallback (validator), null);
			await server.AuthenticateAsClientAsync (targetHost, GetCertificates (clientCertificates), GetSslProtocol (), false);

			return server;
		}
	}
}

