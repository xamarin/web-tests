//
// CertificateValidationProvider.cs
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
using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;

namespace Xamarin.WebTests.Server
{
	using Portable;
	using Resources;

	class CertificateValidationProvider : ICertificateValidationProvider
	{
		internal CertificateValidationProvider (bool installDefaultValidator)
		{
			if (installDefaultValidator) {
				var validator = AcceptThisCertificate (ResourceManager.DefaultServerCertificate);
				ServicePointManager.ServerCertificateValidationCallback = validator.ValidationCallback;
			}
		}

		public ICertificateValidator GetDefault ()
		{
			return RejectAll ();
		}

		ICertificateValidator ICertificateValidationProvider.AcceptThisCertificate (IServerCertificate certificate)
		{
			return AcceptThisCertificate (certificate);
		}

		internal CertificateValidator AcceptThisCertificate (IServerCertificate certificate)
		{
			var cert = new X509Certificate2 (certificate.Data, certificate.Password);
			var serverHash = cert.GetCertHash ();

			return new CertificateValidator ((s, c, ch, e) => {
				return Compare (c.GetCertHash (), serverHash);
			});
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

		public ICertificateValidator RejectAll ()
		{
			return new CertificateValidator ((s, c, ch, e) => false);
		}
	}
}

