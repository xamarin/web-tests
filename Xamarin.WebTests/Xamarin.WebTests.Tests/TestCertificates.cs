//
// TestCertificates.cs
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
using System.Security.Cryptography.X509Certificates;
using Xamarin.AsyncTests;
using Xamarin.WebTests.Resources;
using Xamarin.WebTests.TestFramework;
using Xamarin.WebTests.TestRunners;

namespace Xamarin.WebTests.Tests
{
	[AsyncTestFixture]
	public class TestCertificates
	{
		[AsyncTest]
		[CertificateResourceType (CertificateResourceType.HamillerTubeCA)]
		public void TestSimpleCertificate (TestContext ctx)
		{
			Run (ctx);
		}

		[AsyncTest]
		[CertificateResourceType (CertificateResourceType.TlsTestXamDevExpired)]
		public void TestSimpleCertificate2 (TestContext ctx)
		{
			Run (ctx);
		}

		[AsyncTest]
		[CertificateResourceType (CertificateResourceType.TlsTestXamDevCA)]
		public void TestSimpleCertificate3 (TestContext ctx)
		{
			Run (ctx);
		}

		[AsyncTest]
		[CertificateResourceType (CertificateResourceType.SelfSignedServerCertificate)]
		public void TestSimpleCertificate4 (TestContext ctx)
		{
			Run (ctx);
		}

		void Run (TestContext ctx)
		{
			var type = ctx.GetParameter<CertificateResourceType> ();
			var data = ResourceManager.GetCertificateData (type);
			var info = ResourceManager.GetCertificateInfo (type);
			var cert = new X509Certificate2 (data);

			CertificateInfoTestRunner.TestManagedCertificate (ctx, cert, info);
		}
	}
}

