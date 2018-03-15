//
// CertificateResourceTypeAttribute.cs
//
// Author:
//       Martin Baulig <mabaul@microsoft.com>
//
// Copyright (c) 2017 Xamarin Inc. (http://www.xamarin.com)
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
using Xamarin.WebTests.Resources;
using Xamarin.AsyncTests;

namespace Xamarin.WebTests.TestAttributes
{
	[AttributeUsage (AttributeTargets.Parameter | AttributeTargets.Property, AllowMultiple = false)]
	public class CertificateResourceTypeAttribute : TestParameterAttribute, ITestParameterSource<CertificateResourceType> {
		public CertificateResourceType? ResourceType {
			get;
			private set;
		}

		public CertificateResourceTypeAttribute (string filter = null)
			: base (filter, TestFlags.Browsable | TestFlags.ContinueOnError)
		{
		}

		public CertificateResourceTypeAttribute (CertificateResourceType type)
			: base (null, TestFlags.Browsable | TestFlags.ContinueOnError)
		{
			ResourceType = type;
		}

		public IEnumerable<CertificateResourceType> GetParameters (TestContext ctx, string filter)
		{
			if (ResourceType != null) {
				yield return ResourceType.Value;
				yield break;
			}

			yield return CertificateResourceType.IntermediateCA;
			yield return CertificateResourceType.IntermediateServer;
			yield return CertificateResourceType.HamillerTubeCA;
			yield return CertificateResourceType.TlsTestXamDevExpired2;
			yield return CertificateResourceType.TlsTestXamDevNew;
			yield return CertificateResourceType.TlsTestXamDevExpired;
			yield return CertificateResourceType.TlsTestXamDevOldCA;
			yield return CertificateResourceType.TlsTestInternal;
			yield return CertificateResourceType.TlsTestInternalCA;
			yield return CertificateResourceType.SelfSignedServerCertificate;
		}
	}
}
