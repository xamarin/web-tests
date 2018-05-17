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
using System.Net.Security;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using Xamarin.AsyncTests;
using Xamarin.WebTests.ConnectionFramework;
using Xamarin.WebTests.Resources;

using MSI = Mono.Security.Interface;

namespace Xamarin.WebTests.MonoConnectionFramework
{
	using MonoTestFramework;

	public class MonoConnectionProvider : ConnectionProvider, ISslStreamProvider
	{
		readonly MSI.MonoTlsProvider tlsProvider;
		readonly string name;

		internal MonoConnectionProvider (ConnectionProviderFactory factory, ConnectionProviderType type, ConnectionProviderFlags flags,
		                                 string name, MSI.MonoTlsProvider tlsProvider)
			: base (factory, type, GetFlags (flags, tlsProvider))
		{
			this.name = name;
			this.tlsProvider = tlsProvider;
		}

		static ConnectionProviderFlags GetFlags (ConnectionProviderFlags flags, MSI.MonoTlsProvider tlsProvider)
		{
			if (((flags & ConnectionProviderFlags.DisableMonoExtensions) != 0) || !tlsProvider.SupportsMonoExtensions)
				return flags;

			flags |= ConnectionProviderFlags.SupportsMonoExtensions | ConnectionProviderFlags.SupportsHttpListener;
			var frameworkSetup = DependencyInjector.Get<IMonoConnectionFrameworkSetup> ();
			if (frameworkSetup.ProviderSupportsCleanShutdown (tlsProvider))
				flags |= ConnectionProviderFlags.SupportsCleanShutdown;
			if (frameworkSetup.SupportsRenegotiation) {
				flags |= ConnectionProviderFlags.SupportsClientRenegotiation;
				if (tlsProvider.ID == ConnectionProviderFactory.AppleTlsGuid)
					flags |= ConnectionProviderFlags.SupportsServerRenegotiation;
			}

			return flags;
		}

		public override string Name {
			get { return name; }
		}

		public override ProtocolVersions SupportedProtocols {
			get { return (ProtocolVersions)tlsProvider.SupportedProtocols; }
		}

		public override Connection CreateClient (ConnectionParameters parameters)
		{
			if (SupportsMonoExtensions)
				return new MonoClient (this, parameters);
			else
				return new DotNetClient (this, parameters, this);
		}

		public override Connection CreateServer (ConnectionParameters parameters)
		{
			if (SupportsMonoExtensions)
				return new MonoServer (this, parameters);
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

		public SslProtocols GetProtocol (ConnectionParameters parameters, bool server)
		{
			var protocol = (ProtocolVersions)tlsProvider.SupportedProtocols;
			protocol &= server ? ProtocolVersions.ServerMask : ProtocolVersions.ClientMask;
			if (parameters.ProtocolVersion != null)
				protocol &= parameters.ProtocolVersion.Value;
			if (protocol == ProtocolVersions.Unspecified)
				throw new NotSupportedException ();
			return (SslProtocols)protocol;
		}

		public X509CertificateCollection GetClientCertificates (ConnectionParameters parameters)
		{
			return DotNetConnectionProvider.GetClientCertificates (parameters);
		}

		MSI.MonoTlsSettings GetSettings (ConnectionParameters parameters, bool requireSettings, bool server)
		{
			MSI.MonoTlsSettings settings = null;

			requireSettings |= parameters.CleanShutdown;
			requireSettings |= parameters.ValidationParameters != null;
			requireSettings |= server && parameters.ServerCertificateValidator != null;
			requireSettings |= !server && parameters.ClientCertificateValidator != null;

			if (requireSettings)
				settings = MSI.MonoTlsSettings.CopyDefaultSettings ();

			if (parameters.ValidationParameters != null && parameters.ValidationParameters.TrustedRoots != null) {
				settings.TrustAnchors = new X509CertificateCollection ();
				foreach (var trustedRoot in parameters.ValidationParameters.TrustedRoots) {
					var trustedRootCert = ResourceManager.GetCertificate (trustedRoot);
					settings.TrustAnchors.Add (trustedRootCert);
				}
			}

			if (parameters.CleanShutdown) {
				var setup = DependencyInjector.Get<IMonoConnectionFrameworkSetup> ();
				setup.SendCloseNotify (settings, true);
			}

			if (server && parameters.ClientCertificateIssuers != null) {
				var setup = DependencyInjector.Get<IMonoConnectionFrameworkSetup> ();
				if (!setup.SupportsClientCertificateIssuers)
					throw new NotSupportedException ("MonoTlsSettings.ClientCertificateIssuers is not supported!");
				setup.SetClientCertificateIssuers (settings, parameters.ClientCertificateIssuers);
			}

			if (server) {
				CallbackHelpers.AddCertificateValidator (settings, parameters.ServerCertificateValidator);
			} else {
				CallbackHelpers.AddCertificateValidator (settings, parameters.ClientCertificateValidator);
				CallbackHelpers.AddCertificateSelector (settings, parameters.ClientCertificateSelector);
			}

			return settings;
		}

		public bool SupportsWebRequest => true;

		public HttpWebRequest CreateWebRequest (Uri uri, ConnectionParameters parameters)
		{
			var settings = GetSettings (parameters, false, false);
			return MSI.MonoTlsProviderFactory.CreateHttpsRequest (uri, tlsProvider, settings);
		}

		public bool SupportsHttpListener => true;

		public HttpListener CreateHttpListener (ConnectionParameters parameters)
		{
			var certificate = parameters.ServerCertificate;

			var settings = GetSettings (parameters, false, true);
			return MSI.MonoTlsProviderFactory.CreateHttpListener (certificate, tlsProvider, settings);
		}

		public bool SupportsHttpListenerContext => true;

		public SslStream GetSslStream (HttpListenerContext context)
		{
			return MSI.MonoTlsProviderFactory.GetMonoSslStream (context).SslStream;
		}

		public SslStream CreateSslStream (TestContext ctx, Stream stream, ConnectionParameters parameters, bool server)
		{
			var settings = GetSettings (parameters, true, server);
			if (parameters is MonoConnectionParameters monoParams) {
				if (monoParams.ClientCiphers != null)
					settings.EnabledCiphers = monoParams.ClientCiphers.ToArray ();
			}

			var monoSslStream = tlsProvider.CreateSslStream (stream, false, settings);
			return monoSslStream.SslStream;
		}

		public override string ToString ()
		{
			return string.Format ("[MonoConnectionProvider: {0}]", Type);
		}
	}
}

