//
// TestBoringCertificates.cs
//
// Author:
//       Martin Baulig <martin.baulig@xamarin.com>
//
// Copyright (c) 2016 Xamarin Inc. (http://www.xamarin.com)

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
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using Xamarin.AsyncTests;
using Mono.Btls.TestFramework;
using Xamarin.WebTests.Resources;
using Xamarin.WebTests.TestFramework;
using Xamarin.WebTests.TestRunners;
using Mono.Btls.Interface;

namespace Mono.Btls.Tests
{
	[Work]
	[AsyncTestFixture]
	public class TestBoringCertificates
	{
		[BoringCertificateResourceType]
		public CertificateResourceType ResourceType {
			get; set;
		}

		public bool Managed {
			get;
			private set;
		}

		[AsyncTest]
		public void Run (TestContext ctx, BoringX509Host x509)
		{
			var data = ResourceManager.GetCertificateData (ResourceType);
			var info = ResourceManager.GetCertificateInfo (ResourceType);


			if (!Managed) {
				BoringCertificateInfoTestRunner.TestNativeCertificate (ctx, x509.Instance, info);
				return;
			}

			using (var cert = BtlsProvider.CreateCertificate2 (data, BtlsX509Format.PEM, true)) {
				CertificateInfoTestRunner.TestManagedCertificate (ctx, cert, info);
			}
		}
	}
}

