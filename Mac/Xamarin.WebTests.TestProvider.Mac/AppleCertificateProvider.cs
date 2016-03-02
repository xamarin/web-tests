//
// AppleCertificateProvider.cs
//
// Author:
//       Martin Baulig <martin.baulig@xamarin.com>
//
// Copyright (c) 2016 Xamarin, Inc.
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
using System.Threading;
using System.Threading.Tasks;

using System.Security.Cryptography.X509Certificates;
using Xamarin.AsyncTests;
using Xamarin.WebTests.MonoTestFramework;
using Xamarin.WebTests.ConnectionFramework;

using Security;

namespace Xamarin.WebTests.TestProvider.Mobile
{
	using TestFramework;

	public class AppleCertificateProvider : IAppleCertificateProvider
	{
		#region IAppleCertificateProvider implementation

		public void InstallIntoKeyChain (X509Certificate certificate)
		{
			var provider = DependencyInjector.Get<ICertificateProvider> ();
			string password;
			var data = provider.GetRawCertificateData (certificate, out password);
			using (var identity = SecIdentity.Import (data, password)) {
				SecKeyChain.AddIdentity (identity);
			}
		}

		public void RemoveFromKeyChain (X509Certificate certificate)
		{
			using (var secCert = new SecCertificate (certificate))
			using (var identity = SecKeyChain.FindIdentity (secCert, true)) {
				SecKeyChain.RemoveIdentity (identity);
			}
		}

		public X509Certificate GetAppleCertificate (X509Certificate certificate)
		{
			// This will remove the private key if we have any.
			using (var appleCert = new SecCertificate (certificate))
				return appleCert.ToX509Certificate ();
		}

		public bool IsInKeyChain (X509Certificate certificate)
		{
			using (var secCert = new SecCertificate (certificate)) {
				var identity = SecKeyChain.FindIdentity (secCert, false);
				if (identity != null)
					identity.Dispose ();
				return identity != null;
			}
		}

		#endregion
	}
}

