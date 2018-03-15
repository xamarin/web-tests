﻿using System;
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
		static readonly byte[] cacertData;
		static readonly X509Certificate cacert;
		static readonly byte[] serverCertNoKeyData;
		static readonly X509Certificate serverCertNoKey;
		static readonly byte[] selfServerCertNoKeyData;
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

		static readonly byte[] tlsTestXamDevExpiredData;
		static readonly X509Certificate tlsTestXamDevExpired;
		static readonly byte[] tlsTestXamDevExpired2Data;
		static readonly X509Certificate tlsTestXamDevExpired2;
		static readonly byte[] tlsTestXamDevNewData;
		static readonly X509Certificate tlsTestXamDevNew;
		static readonly byte[] tlsTestXamDevOldCAData;
		static readonly X509Certificate tlsTestXamDevOldCA;
		static readonly byte[] tlsTestXamDevNewCAData;
		static readonly X509Certificate tlsTestXamDevNewCA;
		static readonly byte[] tlsTestInternalxData;
		static readonly X509Certificate tlsTestInternalx;
		static readonly byte[] tlsTestInternalxCAData;
		static readonly X509Certificate tlsTestInternalxCA;

		static readonly byte[] intermediateCAData;
		static readonly X509Certificate intermediateCA;
		static readonly byte[] intermediateServerData;
		static readonly X509Certificate intermediateServer;

		static readonly byte[] hamillerTubeIMData;
		static readonly X509Certificate hamillerTubeIM;

		static readonly HamillerTubeCAData hamillerTubeCAInfo;
		static readonly TlsTestXamDevExpired2Data tlsTestXamDevExpired2Info;
		static readonly TlsTestXamDevNewData tlsTestXamDevNewInfo;
		static readonly TlsTestXamDevExpiredData tlsTestXamDevExpiredInfo;
		static readonly TlsTestXamDevOldCAData tlsTestXamDevOldCAInfo;
		static readonly TlsTestInternalData tlsTestInternalxInfo;
		static readonly TlsTestInternalDataCA tlsTestInternalxInfoCA;
		static readonly SelfSignedServerData selfSignedServerInfo;
		static readonly IntermediateCAData intermediateCAInfo;
		static readonly IntermediateServerData intermediateServerInfo;

		static readonly byte[] serverCertWithCAData;
		static readonly X509Certificate serverCertWithCA;

		static readonly byte[] intermediateServerCertData;
		static readonly X509Certificate intermediateServerCert;
		static readonly byte[] intermediateServerCertNoKeyData;
		static readonly X509Certificate intermediateServerCertNoKey;
		static readonly byte[] intermediateServerCertBareData;
		static readonly X509Certificate intermediateServerCertBare;
		static readonly byte[] intermediateServerCertFullData;
		static readonly X509Certificate intermediateServerCertFull;

		static readonly byte[] trustedIMCAData;
		static readonly X509Certificate trustedIMCA;
		static readonly byte[] serverCertTrustedIMBareData;
		static readonly X509Certificate serverCertTrustedIMBare;
		static readonly byte[] serverCertTrustedIMData;
		static readonly X509Certificate serverCertTrustedIM;

		const string caCertHash = "AAAB625A1F5EA1DBDBB658FB360613BE49E67AEC";
		const string serverCertHash = "68295BFCB5B109738399DFFF86A5BEDE0694F334";
		const string serverSelfHash = "EC732FEEE493A91635E6BDC18377EEB3C11D6E16";

		static ResourceManager ()
		{
			provider = DependencyInjector.Get<ICertificateProvider> ();
			cacertData = ReadResource ("CA.Hamiller-Tube-CA.pem");
			cacert = provider.GetCertificateFromData (cacertData);
			hamillerTubeIMData = ReadResource ("CA.Hamiller-Tube-IM.pem");
			hamillerTubeIM = provider.GetCertificateFromData (hamillerTubeIMData);
			serverCertNoKeyData = ReadResource ("CA.server-cert.pem");
			serverCertNoKey = provider.GetCertificateFromData (serverCertNoKeyData);
			selfServerCertNoKeyData = ReadResource ("CA.server-self.pem");
			selfServerCertNoKey = provider.GetCertificateFromData (selfServerCertNoKeyData);
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

			tlsTestXamDevExpiredData = ReadResource ("CA.tlstest-xamdev-expired.pem");
			tlsTestXamDevExpired = provider.GetCertificateFromData (tlsTestXamDevExpiredData);

			tlsTestXamDevExpired2Data = ReadResource ("CA.tlstest-xamdev-expired2.pem");
			tlsTestXamDevExpired2 = provider.GetCertificateFromData (tlsTestXamDevExpired2Data);

			tlsTestXamDevNewData = ReadResource ("CA.tlstest-xamdev-new.pem");
			tlsTestXamDevNew = provider.GetCertificateFromData (tlsTestXamDevNewData);
			tlsTestXamDevNewCAData = ReadResource ("CA.tlstest-xamdev-new-ca.pem");
			tlsTestXamDevNewCA = provider.GetCertificateFromData (tlsTestXamDevNewCAData);

			tlsTestXamDevOldCAData = ReadResource ("CA.tlstest-xamdev-old-ca.pem");
			tlsTestXamDevOldCA = provider.GetCertificateFromData (tlsTestXamDevOldCAData);

			tlsTestInternalxData = ReadResource ("CA.tlstest-internalx.pem");
			tlsTestInternalx = provider.GetCertificateFromData (tlsTestInternalxData);
			tlsTestInternalxCAData = ReadResource ("CA.tlstest-internalx-ca.pem");
			tlsTestInternalxCA = provider.GetCertificateFromData (tlsTestInternalxCAData);

			intermediateCAData = ReadResource ("CA.intermediate-ca.pem");
			intermediateCA = provider.GetCertificateFromData (intermediateCAData);
			intermediateServerData = ReadResource ("CA.intermediate-server.pem");
			intermediateServer = provider.GetCertificateWithKey (ReadResource ("CA.intermediate-server.pfx"), "monkey");

			hamillerTubeCAInfo = new HamillerTubeCAData (cacertData);
			selfSignedServerInfo = new SelfSignedServerData (selfServerCertNoKeyData);
			tlsTestXamDevExpired2Info = new TlsTestXamDevExpired2Data (tlsTestXamDevExpired2Data);
			tlsTestXamDevNewInfo = new TlsTestXamDevNewData (tlsTestXamDevNewData);
			tlsTestXamDevExpiredInfo = new TlsTestXamDevExpiredData (tlsTestXamDevExpiredData);
			tlsTestXamDevOldCAInfo = new TlsTestXamDevOldCAData (tlsTestXamDevOldCAData);
			tlsTestInternalxInfo = new TlsTestInternalData (tlsTestInternalxData);
			tlsTestInternalxInfoCA = new TlsTestInternalDataCA (tlsTestInternalxCAData);
			intermediateCAInfo = new IntermediateCAData (intermediateCAData);
			intermediateServerInfo = new IntermediateServerData (intermediateServerData);

			serverCertWithCAData = ReadResource ("CA.server-cert-with-ca.pfx");
			serverCertWithCA = provider.GetCertificateWithKey (serverCertWithCAData, "monkey");

			intermediateServerCertData = ReadResource ("CA.server-cert-im.pfx");
			intermediateServerCert = provider.GetCertificateWithKey (intermediateServerCertData, "monkey");
			intermediateServerCertNoKeyData = ReadResource ("CA.server-cert-im.pem");
			intermediateServerCertNoKey = provider.GetCertificateFromData (intermediateServerCertNoKeyData);
			intermediateServerCertBareData = ReadResource ("CA.server-cert-im-bare.pfx");
			intermediateServerCertBare = provider.GetCertificateWithKey (intermediateServerCertBareData, "monkey");
			intermediateServerCertFullData = ReadResource ("CA.server-cert-im-full.pfx");
			intermediateServerCertFull = provider.GetCertificateWithKey (intermediateServerCertFullData, "monkey");

			trustedIMCAData = ReadResource ("CA.trusted-im-ca.pem");
			trustedIMCA = provider.GetCertificateFromData (trustedIMCAData);

			serverCertTrustedIMBareData = ReadResource ("CA.server-cert-trusted-im-bare.pfx");
			serverCertTrustedIMBare = provider.GetCertificateWithKey (serverCertTrustedIMBareData, "monkey");
			serverCertTrustedIMData = ReadResource ("CA.server-cert-trusted-im.pfx");
			serverCertTrustedIM = provider.GetCertificateWithKey (serverCertTrustedIMData, "monkey");
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

		public static X509Certificate ServerCertificateWithCA {
			get { return serverCertWithCA; }
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
			case CertificateResourceType.HamillerTubeIM:
				return hamillerTubeIM;
			case CertificateResourceType.ServerCertificateFromLocalCA:
				return serverCertNoKey;
			case CertificateResourceType.SelfSignedServerCertificate:
				return selfServerCertNoKey;
			case CertificateResourceType.TlsTestXamDevExpired:
				return tlsTestXamDevExpired;
			case CertificateResourceType.TlsTestXamDevExpired2:
				return tlsTestXamDevExpired2;
			case CertificateResourceType.TlsTestXamDevNew:
				return tlsTestXamDevNew;
			case CertificateResourceType.TlsTestXamDevOldCA:
				return tlsTestXamDevOldCA;
			case CertificateResourceType.TlsTestXamDevNewCA:
				return tlsTestXamDevNewCA;
			case CertificateResourceType.TlsTestInternal:
				return tlsTestInternalx;
			case CertificateResourceType.TlsTestInternalCA:
				return tlsTestInternalxCA;
			case CertificateResourceType.IntermediateCA:
				return intermediateCA;
			case CertificateResourceType.IntermediateServer:
				return intermediateServer;
			case CertificateResourceType.ServerCertificateWithCA:
				return serverCertWithCA;
			case CertificateResourceType.IntermediateServerCertificate:
				return intermediateServerCert;
			case CertificateResourceType.IntermediateServerCertificateBare:
				return intermediateServerCertBare;
			case CertificateResourceType.IntermediateServerCertificateFull:
				return intermediateServerCertFull;
			case CertificateResourceType.IntermediateServerCertificateNoKey:
				return intermediateServerCertNoKey;
			case CertificateResourceType.TrustedIntermediateCA:
				return trustedIMCA;
			case CertificateResourceType.ServerFromTrustedIntermediataCA:
				return serverCertTrustedIM;
			case CertificateResourceType.ServerFromTrustedIntermediateCABare:
				return serverCertTrustedIMBare;
			default:
				throw new InternalErrorException ();
			}
		}

		public static byte[] GetCertificateData (CertificateResourceType type)
		{
			switch (type) {
			case CertificateResourceType.HamillerTubeCA:
				return cacertData;
			case CertificateResourceType.HamillerTubeIM:
				return hamillerTubeIMData;
			case CertificateResourceType.ServerCertificateFromLocalCA:
				return serverCertNoKeyData;
			case CertificateResourceType.SelfSignedServerCertificate:
				return selfServerCertNoKeyData;
			case CertificateResourceType.TlsTestXamDevExpired:
				return tlsTestXamDevExpiredData;
			case CertificateResourceType.TlsTestXamDevExpired2:
				return tlsTestXamDevExpired2Data;
			case CertificateResourceType.TlsTestXamDevNew:
				return tlsTestXamDevNewData;
			case CertificateResourceType.TlsTestXamDevOldCA:
				return tlsTestXamDevOldCAData;
			case CertificateResourceType.TlsTestXamDevNewCA:
				return tlsTestXamDevNewCAData;
			case CertificateResourceType.TlsTestInternal:
				return tlsTestInternalxData;
			case CertificateResourceType.TlsTestInternalCA:
				return tlsTestInternalxCAData;
			case CertificateResourceType.IntermediateCA:
				return intermediateCAData;
			case CertificateResourceType.IntermediateServer:
				return intermediateServerData;
			case CertificateResourceType.ServerCertificateWithCA:
				return serverCertWithCAData;
			case CertificateResourceType.IntermediateServerCertificate:
				return intermediateServerCertData;
			case CertificateResourceType.IntermediateServerCertificateBare:
				return intermediateServerCertBareData;
			case CertificateResourceType.IntermediateServerCertificateFull:
				return intermediateServerCertFullData;
			case CertificateResourceType.IntermediateServerCertificateNoKey:
				return intermediateServerCertNoKeyData;
			case CertificateResourceType.TrustedIntermediateCA:
				return trustedIMCAData;
			case CertificateResourceType.ServerFromTrustedIntermediataCA:
				return serverCertTrustedIMData;
			case CertificateResourceType.ServerFromTrustedIntermediateCABare:
				return serverCertTrustedIMBareData;
			default:
				throw new InternalErrorException ();
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

		public static CertificateInfo GetCertificateInfo (CertificateResourceType type)
		{
			switch (type) {
			case CertificateResourceType.HamillerTubeCA:
				return hamillerTubeCAInfo;
			case CertificateResourceType.SelfSignedServerCertificate:
				return selfSignedServerInfo;
			case CertificateResourceType.TlsTestXamDevExpired:
				return tlsTestXamDevExpiredInfo;
			case CertificateResourceType.TlsTestXamDevExpired2:
				return tlsTestXamDevExpired2Info;
			case CertificateResourceType.TlsTestXamDevNew:
				return tlsTestXamDevNewInfo;
			case CertificateResourceType.TlsTestXamDevOldCA:
				return tlsTestXamDevOldCAInfo;
			case CertificateResourceType.TlsTestInternal:
				return tlsTestInternalxInfo;
			case CertificateResourceType.TlsTestInternalCA:
				return tlsTestInternalxInfoCA;
			case CertificateResourceType.IntermediateCA:
				return intermediateCAInfo;
			case CertificateResourceType.IntermediateServer:
				return intermediateServerInfo;
			default:
				throw new InvalidOperationException ();
			}
		}

		[Obsolete]
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

		public static Uri TlsTestUri => new Uri ("https://tlstest.internalx.com/");
		public static Uri TlsTest1Uri => new Uri ("https://tlstest.internalx.com/");
		public static Uri TlsTest11Uri => new Uri ("https://tlstest.internalx.com/");
		public static Uri TlsTest12Uri => new Uri ("https://tlstest.internalx.com/");
	}
}
