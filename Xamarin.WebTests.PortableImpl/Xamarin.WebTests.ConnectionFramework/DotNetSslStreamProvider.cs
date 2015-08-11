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

		static RemoteCertificateValidationCallback GetValidationCallback (ServerParameters parameters)
		{
			var validator = parameters.ServerCertificateValidator;
			if (validator == null)
				return null;

			return ((CertificateValidator)validator).ValidationCallback;
		}

		static RemoteCertificateValidationCallback GetValidationCallback (ClientParameters parameters)
		{
			var validator = parameters.ClientCertificateValidator;
			if (validator == null)
				return null;

			return ((CertificateValidator)validator).ValidationCallback;
		}

		static X509Certificate2Collection GetClientCertificates (ClientParameters parameters)
		{
			if (parameters.ClientCertificate == null)
				return null;

			var clientCertificateCollection = new X509Certificate2Collection ();
			var certificate = (X509Certificate2)CertificateProvider.GetCertificate (parameters.ClientCertificate);
			clientCertificateCollection.Add (certificate);

			return clientCertificateCollection;
		}

		public ProtocolVersions SupportedProtocols {
			get { return (ProtocolVersions)GetSslProtocol (); }
		}

		static SslProtocols GetProtocol (ConnectionParameters parameters)
		{
			var protocol = parameters.ProtocolVersion ?? (ProtocolVersions)GetSslProtocol ();

			if ((protocol & ProtocolVersions.Tls10) != 0)
				protocol |= ProtocolVersions.Tls10;
			if ((protocol & ProtocolVersions.Tls11) != 0)
				protocol |= ProtocolVersions.Tls11;
			if ((protocol & ProtocolVersions.Tls12) != 0)
				protocol |= ProtocolVersions.Tls12;

			return (SslProtocols)protocol;
		}

		public ISslStream CreateServerStream (Stream stream, ServerParameters parameters)
		{
			var certificate = CertificateProvider.GetCertificate (parameters.ServerCertificate);

			var protocol = GetProtocol (parameters);
			var validator = GetValidationCallback (parameters);

			var askForCert = (parameters.Flags & (ServerFlags.AskForClientCertificate|ServerFlags.RequireClientCertificate)) != 0;

			var sslStream = new SslStream (stream, false, validator);
			sslStream.AuthenticateAsServer (certificate, askForCert, protocol, false);

			return new DotNetSslStream (sslStream);
		}

		public async Task<ISslStream> CreateServerStreamAsync (
			Stream stream, ServerParameters parameters, CancellationToken cancellationToken)
		{
			var certificate = CertificateProvider.GetCertificate (parameters.ServerCertificate);

			var protocol = GetProtocol (parameters);
			var validator = GetValidationCallback (parameters);

			var askForCert = (parameters.Flags & (ServerFlags.AskForClientCertificate|ServerFlags.RequireClientCertificate)) != 0;

			var sslStream = new SslStream (stream, false, validator);
			await sslStream.AuthenticateAsServerAsync (certificate, askForCert, protocol, false);

			return new DotNetSslStream (sslStream);
		}

		public async Task<ISslStream> CreateClientStreamAsync (
			Stream stream, string targetHost, ClientParameters parameters, CancellationToken cancellationToken)
		{
			var protocol = GetProtocol (parameters);
			var clientCertificates = GetClientCertificates (parameters);
			var validator = GetValidationCallback (parameters);

			var sslStream = new SslStream (stream, false, validator, null);
			await sslStream.AuthenticateAsClientAsync (targetHost, clientCertificates, protocol, false);

			return new DotNetSslStream (sslStream);
		}
	}
}

