//
// BoringX509HostAttribute.cs
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
using Mono.Btls.Interface;

namespace Mono.Btls.TestFramework
{
	public class BoringX509HostAttribute : TestHostAttribute, ITestHost<BoringX509Host>
	{
		public BoringX509HostAttribute (CertificateResourceType type, BtlsX509Format format)
			: base (typeof (BoringX509HostAttribute))
		{
			ResourceType = type;
			Format = format;
		}

		public BoringX509HostAttribute ()
			: base (typeof (BoringX509HostAttribute))
		{
		}

		public CertificateResourceType? ResourceType {
			get;
			private set;
		}

		public BtlsX509Format? Format {
			get;
			private set;
		}

		public BoringX509Host CreateInstance (TestContext ctx)
		{
			CertificateResourceType type;
			if (ResourceType != null)
				type = ResourceType.Value;
			else
				type = ctx.GetParameter<CertificateResourceType> ();

			BtlsX509Format format;
			if (Format != null)
				format = Format.Value;
			else if (!ctx.TryGetParameter<BtlsX509Format> (out format))
				format = BtlsX509Format.DER;

			var cert = ResourceManager.GetCertificate (type);
			return new BoringX509Host (cert.GetRawCertData (), format);
		}
	}
}

