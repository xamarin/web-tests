using System;
using System.IO;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using Xamarin.AsyncTests;

namespace Xamarin.WebTests.Resources
{
	using ConnectionFramework;

	public static class ResourceManager
	{
		static readonly ICertificateProvider provider;
		static readonly X509Certificate cacert;
		static readonly X509Certificate serverCertNoKey;
		static readonly X509Certificate selfServerCertNoKey;
		static readonly X509Certificate serverCert;
		static readonly X509Certificate selfServerCert;
		static readonly X509Certificate invalidServerCert;
		static readonly X509Certificate invalidClientCert;
		static readonly X509Certificate invalidClientCaCert;
		static readonly X509Certificate invalidClientCertRsa512;
		static readonly X509Certificate monkeyCert;
		static readonly X509Certificate penguinCert;
		static readonly X509Certificate serverCertRsaOnly;
		static readonly X509Certificate serverCertDheOnly;
		static readonly X509Certificate invalidServerCertRsa512;
		static readonly X509Certificate clientCertRsaOnly;
		static readonly X509Certificate clientCertDheOnly;

		const string caCertHash = "AAAB625A1F5EA1DBDBB658FB360613BE49E67AEC";
		const string serverCertHash = "68295BFCB5B109738399DFFF86A5BEDE0694F334";
		const string serverSelfHash = "EC732FEEE493A91635E6BDC18377EEB3C11D6E16";

		static ResourceManager ()
		{
			provider = DependencyInjector.Get<ICertificateProvider> ();
			cacert = provider.GetCertificateFromData (ResourceManager.ReadResource ("CA.Hamiller-Tube-CA.pem"));
			serverCertNoKey = provider.GetCertificateFromData (ResourceManager.ReadResource ("CA.server-cert.pem"));
			selfServerCertNoKey = provider.GetCertificateFromData (ResourceManager.ReadResource ("CA.server-self.pem"));
			selfServerCert = provider.GetCertificateWithKey (ReadResource ("CA.server-self.pfx"), "monkey");
			serverCert = provider.GetCertificateWithKey (ReadResource ("CA.server-cert.pfx"), "monkey");
			invalidServerCert = provider.GetCertificateWithKey (ReadResource ("CA.invalid-server-cert.pfx"), "monkey");
			invalidClientCert = provider.GetCertificateWithKey (ReadResource ("CA.invalid-client-cert.pfx"), "monkey");
			invalidClientCaCert = provider.GetCertificateWithKey (ReadResource ("CA.invalid-client-ca-cert.pfx"), "monkey");
			invalidClientCertRsa512 = provider.GetCertificateWithKey (ReadResource ("CA.client-cert-rsa512.pfx"), "monkey");
			monkeyCert = provider.GetCertificateWithKey (ReadResource ("CA.monkey.pfx"), "monkey");
			penguinCert = provider.GetCertificateWithKey (ReadResource ("CA.penguin.pfx"), "penguin");
			serverCertRsaOnly = provider.GetCertificateWithKey (ReadResource ("CA.server-cert-rsaonly.pfx"), "monkey");
			serverCertDheOnly = provider.GetCertificateWithKey (ReadResource ("CA.server-cert-dhonly.pfx"), "monkey");
			invalidServerCertRsa512 = provider.GetCertificateWithKey (ReadResource ("CA.server-cert-rsa512.pfx"), "monkey");
			clientCertRsaOnly = provider.GetCertificateWithKey (ReadResource ("CA.client-cert-rsaonly.pfx"), "monkey");
			clientCertDheOnly = provider.GetCertificateWithKey (ReadResource ("CA.client-cert-dheonly.pfx"), "monkey");
		}

		public static X509Certificate LocalCACertificate {
			get { return cacert; }
		}

		public static X509Certificate InvalidServerCertificateV1 {
			get { return invalidServerCert; }
		}

		public static X509Certificate SelfSignedServerCertificate {
			get { return selfServerCert; }
		}

		public static X509Certificate ServerCertificateFromCA {
			get { return serverCert; }
		}

		public static X509Certificate InvalidClientCertificateV1 {
			get { return invalidClientCert; }
		}

		public static X509Certificate InvalidClientCaCertificate {
			get { return invalidClientCaCert; }
		}

		public static X509Certificate InvalidClientCertificateRsa512 {
			get { return invalidClientCertRsa512; }
		}

		public static X509Certificate MonkeyCertificate {
			get { return monkeyCert; }
		}

		public static X509Certificate PenguinCertificate {
			get { return penguinCert; }
		}

		public static X509Certificate ServerCertificateRsaOnly {
			get { return serverCertRsaOnly; }
		}

		public static X509Certificate ServerCertificateDheOnly {
			get { return serverCertDheOnly; }
		}

		public static X509Certificate InvalidServerCertificateRsa512 {
			get { return invalidServerCertRsa512; }
		}

		public static X509Certificate ClientCertificateRsaOnly {
			get { return clientCertRsaOnly; }
		}

		public static X509Certificate ClientCertificateDheOnly {
			get { return clientCertDheOnly; }
		}

		public static X509Certificate GetCertificateWithKey (CertificateResourceType type)
		{
			switch (type) {
			case CertificateResourceType.ServerCertificateFromLocalCA:
				return serverCert;
			case CertificateResourceType.SelfSignedServerCertificate:
				return selfServerCert;
			default:
				throw new InvalidOperationException ();
			}
		}

		public static X509Certificate GetCertificate (CertificateResourceType type)
		{
			switch (type) {
			case CertificateResourceType.HamillerTubeCA:
				return cacert;
			case CertificateResourceType.ServerCertificateFromLocalCA:
				return serverCertNoKey;
			case CertificateResourceType.SelfSignedServerCertificate:
				return selfServerCertNoKey;
			default:
				throw new InvalidOperationException ();
			}
		}

		public static string GetCertificateHash (CertificateResourceType type)
		{
			switch (type) {
			case CertificateResourceType.HamillerTubeCA:
				return caCertHash;
			case CertificateResourceType.ServerCertificateFromLocalCA:
				return serverCertHash;
			case CertificateResourceType.SelfSignedServerCertificate:
				return serverSelfHash;
			default:
				throw new InvalidOperationException ();
			}
		}

		public static bool TryLookupByHash (string hash, out CertificateResourceType type)
		{
			switch (hash.ToUpperInvariant ()) {
			case caCertHash:
				type = CertificateResourceType.HamillerTubeCA;
				return true;
			case serverCertHash:
				type = CertificateResourceType.ServerCertificateFromLocalCA;
				return true;
			case serverSelfHash:
				type = CertificateResourceType.SelfSignedServerCertificate;
				return true;
			default:
				type = CertificateResourceType.Invalid;
				return false;
			}
		}

		internal static byte[] ReadResource (string name)
		{
			var assembly = typeof(ResourceManager).GetTypeInfo ().Assembly;
			using (var stream = assembly.GetManifestResourceStream (assembly.GetName ().Name + "." + name)) {
				var data = new byte [stream.Length];
				var ret = stream.Read (data, 0, data.Length);
				if (ret != data.Length)
					throw new IOException ();
				return data;
			}
		}
	}
}

