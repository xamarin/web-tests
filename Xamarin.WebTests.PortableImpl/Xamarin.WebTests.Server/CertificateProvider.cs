//
// CertificateProvider.cs
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
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;

namespace Xamarin.WebTests.Server
{
	using Portable;
	using Providers;

	class CertificateProvider : ICertificateProvider
	{
		static readonly ICertificateValidator acceptAll = new CertificateValidator ((s, c, ch, e) => true);
		static readonly ICertificateValidator rejectAll = new CertificateValidator ((s, c, ch, e) => false);
		static readonly ICertificateValidator acceptNull = new CertificateValidator ((s, c, ch, e) => {
			return c == null && e == SslPolicyErrors.RemoteCertificateNotAvailable;
		});

		public static ICertificateValidator AcceptAll {
			get { return acceptAll; }
		}

		public static ICertificateValidator RejectAll {
			get { return rejectAll; }
		}

		public static ICertificateValidator AcceptNull {
			get { return acceptNull; }
		}

		public ICertificateValidator GetDefault ()
		{
			return RejectAll;
		}

		ICertificateValidator ICertificateProvider.AcceptNull ()
		{
			return AcceptNull;
		}

		ICertificateValidator ICertificateProvider.AcceptThisCertificate (IServerCertificate certificate)
		{
			return AcceptThisCertificate (certificate);
		}

		internal CertificateValidator AcceptThisCertificate (IServerCertificate certificate)
		{
			var cert = GetCertificate (certificate);
			var serverHash = cert.GetCertHash ();

			return new CertificateValidator ((s, c, ch, e) => {
				if (c == null || e == SslPolicyErrors.RemoteCertificateNotAvailable)
					return false;
				if (e == SslPolicyErrors.None)
					return true;
				return Compare (c.GetCertHash (), serverHash);
			});
		}

		ICertificateValidator ICertificateProvider.AcceptFromCA (ICertificate certificate)
		{
			return AcceptFromCA (certificate);
		}

		internal CertificateValidator AcceptFromCA (ICertificate certificate)
		{
			var cert = GetCertificate (certificate);

			return new CertificateValidator ((s, c, ch, e) => {
				if (c == null || e == SslPolicyErrors.RemoteCertificateNotAvailable)
					return false;
				if (e == SslPolicyErrors.None)
					return true;
				return c.Issuer.Equals (cert.Issuer);
			});
		}

		void ICertificateProvider.InstallDefaultValidator (ICertificateValidator validator)
		{
			InstallDefaultValidator ((CertificateValidator)validator);
		}

		public void InstallDefaultValidator (CertificateValidator validator)
		{
			if (validator != null)
				ServicePointManager.ServerCertificateValidationCallback = validator.ValidationCallback;
			else
				ServicePointManager.ServerCertificateValidationCallback = null;
		}

		static bool Compare (byte[] first, byte[] second)
		{
			if (first.Length != second.Length)
				return false;
			for (int i = 0; i < first.Length; i++) {
				if (first[i] != second[i])
					return false;
			}
			return true;
		}

		ICertificateValidator ICertificateProvider.RejectAll ()
		{
			return RejectAll;
		}

		ICertificateValidator ICertificateProvider.AcceptAll ()
		{
			return AcceptAll;
		}

		public IServerCertificate GetServerCertificate (byte[] data, string password)
		{
			return new CertificateFromPFX (data, password);
		}

		public IClientCertificate GetClientCertificate (byte[] data, string password)
		{
			return new CertificateFromPFX (data, password);
		}

		public static ICertificate GetCertificate (X509Certificate certificate)
		{
			return certificate != null ? new CertificateFromData (certificate) : null;
		}

		public static X509Certificate GetCertificate (ICertificate certificate)
		{
			return certificate != null ? ((CertificateFromData)certificate).Certificate : null;
		}

		public ICertificate GetCertificateFromData (byte[] data)
		{
			return new CertificateFromData (data);
		}

		static ICertificate[] GetCertificateCollection (X509CertificateCollection collection)
		{
			if (collection == null)
				return null;
			var array = new ICertificate [collection.Count];
			for (int i = 0; i < array.Length; i++)
				array [i] = GetCertificate (collection [i]);
			return array;
		}

		public ICertificateValidator GetCustomCertificateValidator (CertificateValidationDelegate func)
		{
			return new CertificateValidator ((s, c, ch, e) => func (GetCertificate (c)));
		}

		public ICertificateSelector GetCustomCertificateSelector (CertificateSelectionDelegate func)
		{
			return new CertificateSelector ((s, t, lc, rc, ai) => {
				var localCertificates = GetCertificateCollection (lc);
				var remoteCertificate = GetCertificate (rc);
				var result = func (t, localCertificates, remoteCertificate, ai);
				return GetCertificate (result);
			});
		}

		public bool AreEqual (ICertificate a, ICertificate b)
		{
			if (a == b)
				return true;

			var aImpl = (CertificateFromData)a;
			var bImpl = (CertificateFromData)b;
			var aHash = aImpl.GetCertificateHash ();
			var bHash = bImpl.GetCertificateHash ();
			return string.Equals (aHash, bHash);
		}

		class CertificateFromData : ICertificate
		{
			public byte[] Data {
				get;
				private set;
			}

			public X509Certificate Certificate {
				get;
				private set;
			}

			public string Issuer {
				get { return Certificate.Issuer; }
			}

			public string Subject {
				get { return Certificate.Subject; }
			}

			public string GetSerialNumber ()
			{
				return Certificate.GetSerialNumberString ();
			}

			public string GetCertificateHash ()
			{
				return Certificate.GetCertHashString ();
			}

			public CertificateFromData (X509Certificate certificate)
			{
				Certificate = certificate;
				Data = certificate.GetRawCertData ();
			}

			public CertificateFromData (byte[] data)
			{
				Data = data;
				Certificate = new X509Certificate (data);
			}

			protected CertificateFromData (byte[] data, X509Certificate certificate)
			{
				Data = data;
				Certificate = certificate;
			}
		}

		class CertificateFromPFX : CertificateFromData, IServerCertificate, IClientCertificate
		{
			public string Password {
				get;
				private set;
			}

			new public X509Certificate2 Certificate {
				get { return (X509Certificate2)base.Certificate; }
			}

			public CertificateFromPFX (byte[] data, string password)
				: base (data, new X509Certificate2 (data, password))
			{
				Password = password;
			}
		}

	}
}

