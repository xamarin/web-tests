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
	public class DotNetSslStreamProvider : ISslStreamProvider
	{
		static SslProtocols GetSslProtocol ()
		{
			return (SslProtocols)ServicePointManager.SecurityProtocol;
		}

		static RemoteCertificateValidationCallback GetServerValidationCallback (ConnectionParameters parameters)
		{
			var validator = parameters.ServerCertificateValidator;
			if (validator == null)
				return null;

			return validator.ValidationCallback;
		}

		static RemoteCertificateValidationCallback GetClientValidationCallback (ConnectionParameters parameters)
		{
			var validator = parameters.ClientCertificateValidator;
			if (validator == null)
				return null;

			return validator.ValidationCallback;
		}

		static LocalCertificateSelectionCallback GetSelectionCallback (ConnectionParameters parameters)
		{
			var selector = parameters.ClientCertificateSelector;
			if (selector == null)
				return null;

			return selector.SelectionCallback;
		}

		static X509CertificateCollection GetClientCertificates (ConnectionParameters parameters)
		{
			if (parameters.ClientCertificate == null)
				return null;

			var clientCertificateCollection = new X509CertificateCollection ();
			var certificate = parameters.ClientCertificate;
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

		public ISslStream CreateServerStream (Stream stream, ConnectionParameters parameters)
		{
			var certificate = parameters.ServerCertificate;

			var protocol = GetProtocol (parameters);
			var validator = GetServerValidationCallback (parameters);

			var askForCert = parameters.AskForClientCertificate || parameters.RequireClientCertificate;

			var sslStream = new SslStream (stream, false, validator);
			sslStream.AuthenticateAsServer (certificate, askForCert, protocol, false);

			return new DotNetSslStream (sslStream);
		}

		public async Task<ISslStream> CreateServerStreamAsync (
			Stream stream, ConnectionParameters parameters, CancellationToken cancellationToken)
		{
			var certificate = parameters.ServerCertificate;

			var protocol = GetProtocol (parameters);
			var validator = GetServerValidationCallback (parameters);

			var askForCert = parameters.AskForClientCertificate || parameters.RequireClientCertificate;

			var sslStream = new SslStream (stream, false, validator);
			await sslStream.AuthenticateAsServerAsync (certificate, askForCert, protocol, false);

			return new DotNetSslStream (sslStream);
		}

		public async Task<ISslStream> CreateClientStreamAsync (
			Stream stream, string targetHost, ConnectionParameters parameters, CancellationToken cancellationToken)
		{
			var protocol = GetProtocol (parameters);
			var clientCertificates = GetClientCertificates (parameters);
			var validator = GetClientValidationCallback (parameters);
			var selector = GetSelectionCallback (parameters);

			var sslStream = new SslStream (stream, false, validator, selector);
			await sslStream.AuthenticateAsClientAsync (targetHost, clientCertificates, protocol, false);

			return new DotNetSslStream (sslStream);
		}

		public bool SupportsWebRequest {
			get { return true; }
		}

		public HttpWebRequest CreateWebRequest (Uri uri)
		{
			return (HttpWebRequest)HttpWebRequest.Create (uri);
		}
	}
}

