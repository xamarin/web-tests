//
// AppleCertificateHost.cs
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
using Xamarin.WebTests.ConnectionFramework;
using Xamarin.WebTests.MonoTestFramework;
using Xamarin.WebTests.Resources;

namespace Xamarin.WebTests.MonoTestFeatures
{
	public class AppleCertificateHostAttribute : TestHostAttribute, ITestHost<AppleCertificateHost>
	{
		public AppleCertificateHostAttribute ()
			: base (typeof (AppleCertificateHostAttribute))
		{
			Identifier = "AppleCertificateHost";
		}

		public AppleCertificateHostAttribute (CertificateResourceType type)
			: base (typeof (AppleCertificateHostAttribute))
		{
			Identifier = "AppleCertificateHost";
			Type = type;
		}

		public CertificateResourceType? Type {
			get;
			private set;
		}

		public AppleCertificateHost CreateInstance (TestContext context)
		{
			if (Type != null)
				return new AppleCertificateHost (Type.Value);
			else
				return new AppleCertificateHost (ResourceManager.SelfSignedServerCertificate);
		}
	}
}

