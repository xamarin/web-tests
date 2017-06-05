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
using System.Reflection;
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
		static readonly MethodInfo getSslStreamFromHttpListenerContext;
		static readonly PropertyInfo clientCertIssuersProp;

		static MonoConnectionProvider ()
		{
			var type = typeof (MSI.MonoTlsProviderFactory);
			getSslStreamFromHttpListenerContext = type.GetRuntimeMethod ("GetMonoSslStream", new Type[] { typeof (HttpListenerContext) });
			clientCertIssuersProp = typeof (MSI.MonoTlsSettings).GetTypeInfo ().GetDeclaredProperty ("ClientCertificateIssuers");
		}

		internal MonoConnectionProvider (ConnectionProviderFactory factory, ConnectionProviderType type, ConnectionProviderFlags flags,
		                                 string name, MSI.MonoTlsProvider tlsProvider)
			: base (factory, type, GetFlags (flags, tlsProvider))
		{
			this.name = name;
			this.tlsProvider = tlsProvider;
		}

		static ConnectionProviderFlags GetFlags (ConnectionProviderFlags flags, MSI.MonoTlsProvider tlsProvider)
		{
			if (tlsProvider.SupportsMonoExtensions) {
				flags |= ConnectionProviderFlags.SupportsMonoExtensions | ConnectionProviderFlags.SupportsHttpListener;
				// Legacy TLS does not support the clean shutdown.
				if ((flags & ConnectionProviderFlags.SupportsTls12) != 0) {
					if (DependencyInjector.Get<IConnectionFrameworkSetup> ().SupportsCleanShutdown)
						flags |= ConnectionProviderFlags.SupportsCleanShutdown;
				}
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
			return CallbackHelpers.GetClientCertificates (parameters);
		}

		MSI.MonoTlsSettings GetSettings (ConnectionParameters parameters)
		{
			MSI.MonoTlsSettings settings = null;
			if (parameters.ValidationParameters != null && parameters.ValidationParameters.TrustedRoots != null) {
				settings = MSI.MonoTlsSettings.CopyDefaultSettings ();
				settings.TrustAnchors = new X509CertificateCollection ();
				foreach (var trustedRoot in parameters.ValidationParameters.TrustedRoots) {
					var trustedRootCert = ResourceManager.GetCertificate (trustedRoot);
					settings.TrustAnchors.Add (trustedRootCert);
				}
			}

			return settings;
		}

		public bool SupportsWebRequest => true;

		public HttpWebRequest CreateWebRequest (Uri uri, ConnectionParameters parameters)
		{
			var settings = GetSettings (parameters);
			return MSI.MonoTlsProviderFactory.CreateHttpsRequest (uri, tlsProvider, settings);
		}

		public bool SupportsHttpListener => true;

		public HttpListener CreateHttpListener (ConnectionParameters parameters)
		{
			var certificate = parameters.ServerCertificate;

			var settings = GetSettings (parameters);
			return MSI.MonoTlsProviderFactory.CreateHttpListener (certificate, tlsProvider, settings);
		}

		public bool SupportsHttpListenerContext => getSslStreamFromHttpListenerContext != null;

		public SslStream GetSslStream (HttpListenerContext context)
		{
			if (getSslStreamFromHttpListenerContext == null)
				throw new NotSupportedException ();
			var sslStream = (MSI.IMonoSslStream)getSslStreamFromHttpListenerContext.Invoke (null, new object[] { context });
			return sslStream.SslStream;
		}

		public SslStream CreateSslStream (TestContext ctx, Stream stream, ConnectionParameters parameters, bool server)
		{
			var settings = new MSI.MonoTlsSettings ();
			if (parameters is MonoConnectionParameters monoParams) {
				if (monoParams.ClientCiphers != null)
					settings.EnabledCiphers = monoParams.ClientCiphers.ToArray ();

				if (!server && monoParams.ClientCertificateIssuers != null) {
					if (clientCertIssuersProp == null)
						ctx.AssertFail ("MonoTlsSettings.ClientCertificateIssuers is not supported!");
					clientCertIssuersProp.SetValue (settings, monoParams.ClientCertificateIssuers);
				}
			}

			if (server)
				CallbackHelpers.AddCertificateValidator (settings, parameters.ServerCertificateValidator);
			else {
				CallbackHelpers.AddCertificateValidator (settings, parameters.ClientCertificateValidator);
				CallbackHelpers.AddCertificateSelector (settings, parameters.ClientCertificateSelector);
			}

			return tlsProvider.CreateSslStream (stream, false, settings).SslStream;
		}

		public override string ToString ()
		{
			return string.Format ("[MonoConnectionProvider: {0}", Type);
		}
	}
}

