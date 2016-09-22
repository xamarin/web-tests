//
// CertificateResourceType.cs
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

namespace Xamarin.WebTests.Resources
{
	public enum CertificateResourceType
	{
		Invalid,
		HamillerTubeCA,
		HamillerTubeIM,
		ServerCertificateFromLocalCA,
		SelfSignedServerCertificate,
		TlsTestXamDevNew,
		TlsTestXamDevExpired,
		TlsTestXamDevCA,
		IntermediateCA,
		IntermediateServer,
		ServerCertificateWithCA,

		// Just the certificate
		IntermediateServerCertificateBare,
		// Same but without the private key
		IntermediateServerCertificateNoKey,
		// Certificate and Intermediate CA
		IntermediateServerCertificate,
		// Certificate, Intermediate CA and Root CA
		IntermediateServerCertificateFull,

		// Install this in the local certificate trust store.
		TrustedIntermediateCA,
		// Server certificate from TrustedIntermediateCA
		ServerFromTrustedIntermediataCA,
		// Same, but without including the CA certificate in the .pfx
		ServerFromTrustedIntermediateCABare
	}
}

