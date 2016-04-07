//
// MonoConnectionProvider.cs
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
using System.Threading;
using System.Threading.Tasks;
using System.Security.Authentication;
using Xamarin.AsyncTests;
using Xamarin.WebTests.ConnectionFramework;

using MSI = Mono.Security.Interface;

namespace Xamarin.WebTests.MonoConnectionFramework
{
	using MonoTestFramework;

	public class MonoConnectionProvider : ConnectionProvider, ISslStreamProvider
	{
		readonly MSI.MonoTlsProvider tlsProvider;
		readonly string name;

		public MonoConnectionProvider (ConnectionProviderFactory factory, ConnectionProviderType type, ConnectionProviderFlags flags, string name, MSI.MonoTlsProvider tlsProvider)
			: base (factory, type, flags)
		{
			this.name = name;
			this.tlsProvider = tlsProvider;
		}

		public override string Name {
			get { return name; }
		}

		public bool SupportsMonoExtensions {
			get { return tlsProvider.SupportsMonoExtensions; }
		}

		public override ProtocolVersions SupportedProtocols {
			get { return (ProtocolVersions)tlsProvider.SupportedProtocols; }
		}

		public IMonoClient CreateMonoClient (ConnectionParameters parameters, IMonoConnectionExtensions extensions)
		{
			if (!SupportsMonoExtensions)
				throw new InvalidOperationException ();
			return new MonoClient (this, parameters, extensions);
		}

		public IMonoServer CreateMonoServer (ConnectionParameters parameters, IMonoConnectionExtensions extensions)
		{
			if (!SupportsMonoExtensions)
				throw new InvalidOperationException ();
			return new MonoServer (this, parameters, extensions);
		}

		public override IClient CreateClient (ConnectionParameters parameters)
		{
			if (SupportsMonoExtensions)
				return new MonoClient (this, parameters, null);
			else
				return new DotNetClient (this, parameters, this);
		}

		public override IServer CreateServer (ConnectionParameters parameters)
		{
			if (SupportsMonoExtensions)
				return new MonoServer (this, parameters, null);
			else
				return new DotNetServer (this, parameters, this);
		}

		protected override ISslStreamProvider GetSslStreamProvider ()
		{
			return this;
		}

		public MSI.MonoTlsProvider MonoTlsProvider {
			get { return tlsProvider; }
		}

		SslProtocols GetProtocol (ConnectionParameters parameters, bool server)
		{
			var protocol = (ProtocolVersions)tlsProvider.SupportedProtocols;
			protocol &= server ? ProtocolVersions.ServerMask : ProtocolVersions.ClientMask;
			if (parameters.ProtocolVersion != null)
				protocol &= parameters.ProtocolVersion.Value;
			if (protocol == ProtocolVersions.Unspecified)
				throw new NotSupportedException ();
			return (SslProtocols)protocol;
		}

		public bool SupportsWebRequest {
			get { return true; }
		}

		public HttpWebRequest CreateWebRequest (Uri uri)
		{
			return MSI.MonoTlsProviderFactory.CreateHttpsRequest (uri, tlsProvider);
		}

		ISslStream ISslStreamProvider.CreateServerStream (Stream stream, ConnectionParameters parameters)
		{
			return CreateServerStream (stream, parameters);
		}

		public MonoSslStream CreateServerStream (Stream stream, ConnectionParameters parameters)
		{
			var settings = new MSI.MonoTlsSettings ();
			var certificate = parameters.ServerCertificate;

			var protocol = GetProtocol (parameters, true);
			CallbackHelpers.AddCertificateValidator (settings, parameters.ServerCertificateValidator);

			var askForCert = parameters.AskForClientCertificate || parameters.RequireClientCertificate;

			var sslStream = tlsProvider.CreateSslStream (stream, false, settings);
			sslStream.AuthenticateAsServer (certificate, askForCert, protocol, false);

			return new MonoSslStream (sslStream);
		}

		async Task<ISslStream> ISslStreamProvider.CreateServerStreamAsync (Stream stream, ConnectionParameters parameters, CancellationToken cancellationToken)
		{
			return await CreateServerStreamAsync (stream, parameters, cancellationToken).ConfigureAwait (false);
		}

		public Task<MonoSslStream> CreateServerStreamAsync (Stream stream, ConnectionParameters parameters, CancellationToken cancellationToken)
		{
			return CreateServerStreamAsync (stream, parameters, new MSI.MonoTlsSettings (), cancellationToken);
		}

		public async Task<MonoSslStream> CreateServerStreamAsync (Stream stream, ConnectionParameters parameters, MSI.MonoTlsSettings settings, CancellationToken cancellationToken)
		{
			var certificate = parameters.ServerCertificate;
			var protocol = GetProtocol (parameters, true);

			CallbackHelpers.AddCertificateValidator (settings, parameters.ServerCertificateValidator);

			var askForCert = parameters.AskForClientCertificate || parameters.RequireClientCertificate;
			var sslStream = tlsProvider.CreateSslStream (stream, false, settings);
			var monoSslStream = new MonoSslStream (sslStream);

			await sslStream.AuthenticateAsServerAsync (certificate, askForCert, protocol, false).ConfigureAwait (false);

			return monoSslStream;
		}

		async Task<ISslStream> ISslStreamProvider.CreateClientStreamAsync (Stream stream, string targetHost, ConnectionParameters parameters, CancellationToken cancellationToken)
		{
			return await CreateClientStreamAsync (stream, targetHost, parameters, cancellationToken).ConfigureAwait (false);
		}

		public Task<MonoSslStream> CreateClientStreamAsync (Stream stream, string targetHost, ConnectionParameters parameters, CancellationToken cancellationToken)
		{
			return CreateClientStreamAsync (stream, targetHost, parameters, new MSI.MonoTlsSettings (), cancellationToken);
		}

		public async Task<MonoSslStream> CreateClientStreamAsync (Stream stream, string targetHost, ConnectionParameters parameters, MSI.MonoTlsSettings settings, CancellationToken cancellationToken)
		{
			var protocol = GetProtocol (parameters, false);

			CallbackHelpers.AddCertificateValidator (settings, parameters.ClientCertificateValidator);
			CallbackHelpers.AddCertificateSelector (settings, parameters.ClientCertificateSelector);
			var clientCertificates = CallbackHelpers.GetClientCertificates (parameters);

			var sslStream = tlsProvider.CreateSslStream (stream, false, settings);
			var monoSslStream = new MonoSslStream (sslStream);

			await sslStream.AuthenticateAsClientAsync (targetHost, clientCertificates, protocol, false).ConfigureAwait (false);

			return monoSslStream;
		}

		public override string ToString ()
		{
			return string.Format ("[MonoConnectionProvider: {0}", Type);
		}
	}
}

