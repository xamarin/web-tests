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
using Xamarin.WebTests.MonoTestFeatures;
using Xamarin.WebTests.Resources;

namespace Xamarin.WebTests.MonoTestFramework
{
	[AppleCertificateHost]
	public class AppleCertificateHost : ITestInstance
	{
		IAppleCertificateProvider provider;

		public X509Certificate CertificateAndKey {
			get;
			private set;
		}

		public X509Certificate AppleCertificate {
			get;
			private set;
		}

		public AppleCertificateHost (X509Certificate certificate)
		{
			CertificateAndKey = certificate;
			provider = DependencyInjector.Get<IAppleCertificateProvider> ();
			AppleCertificate = provider.GetAppleCertificate (CertificateAndKey);
		}

		public AppleCertificateHost (CertificateResourceType type)
		{
			CertificateAndKey = ResourceManager.GetCertificateWithKey (type);
			provider = DependencyInjector.Get<IAppleCertificateProvider> ();
			AppleCertificate = provider.GetAppleCertificate (CertificateAndKey);
		}

		public Task Initialize (TestContext ctx, CancellationToken cancellationToken)
		{
			return Task.Run (() => {
				provider.InstallIntoKeyChain (CertificateAndKey);
			});
		}

		public Task PreRun (TestContext ctx, CancellationToken cancellationToken)
		{
			return Task.FromResult<object> (null);
		}

		public Task PostRun (TestContext ctx, CancellationToken cancellationToken)
		{
			return Task.FromResult<object> (null);
		}

		public Task Destroy (TestContext ctx, CancellationToken cancellationToken)
		{
			return Task.Run (() => {
				provider.RemoveFromKeyChain (AppleCertificate);
			});
		}
	}
}

