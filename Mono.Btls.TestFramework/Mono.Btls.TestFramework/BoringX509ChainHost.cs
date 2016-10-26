//
// BoringX509ChainHost.cs
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
using System.Threading;
using System.Threading.Tasks;
using Xamarin.AsyncTests;
using Xamarin.WebTests.Resources;

using Mono.Btls.Interface;

namespace Mono.Btls.TestFramework
{
	[BoringX509ChainHost]
	public class BoringX509ChainHost : ITestInstance
	{
		BtlsX509Chain chain;
		readonly CertificateResourceCollectionType collectionType;

		public BoringX509ChainHost (CertificateResourceCollectionType collectionType)
		{
			this.collectionType = collectionType;
		}

		public CertificateResourceCollectionType CollectionType {
			get { return collectionType; }
		}

		public BtlsX509Chain Instance {
			get {
				if (chain == null)
					throw new InvalidOperationException ();
				return chain;
			}
		}

		void AddCertificate (CertificateResourceType type)
		{
			var data = ResourceManager.GetCertificateData (type);
			using (var x509 = BtlsProvider.CreateNative (data, BtlsX509Format.PEM))
				chain.Add (x509);
		}

		void PopulateChain (TestContext ctx)
		{
			switch (collectionType) {
			case CertificateResourceCollectionType.None:
				break;
			case CertificateResourceCollectionType.HamillerTubeCA:
				AddCertificate (CertificateResourceType.HamillerTubeCA);
				break;
			case CertificateResourceCollectionType.SelfSignedServerCertificate:
				AddCertificate (CertificateResourceType.SelfSignedServerCertificate);
				break;
			case CertificateResourceCollectionType.TlsTestXamDev:
				AddCertificate (CertificateResourceType.TlsTestXamDevNew);
				break;
			case CertificateResourceCollectionType.TlsTestXamDevWithCA:
				AddCertificate (CertificateResourceType.TlsTestXamDevNew);
				AddCertificate (CertificateResourceType.TlsTestXamDevCA);
				break;
			default:
				ctx.AssertFail ("Invalid certificate chain collection type: '{0}'.", collectionType);
				break;
			}
		}

		public Task Destroy (TestContext ctx, CancellationToken cancellationToken)
		{
			return Task.Run (() => {
				if (chain != null) {
					chain.Dispose ();
					chain = null;
				}
			});
		}

		public Task Initialize (TestContext ctx, CancellationToken cancellationToken)
		{
			return Task.Run (() => {
				chain = BtlsProvider.CreateNativeChain ();
				PopulateChain (ctx);
			});
		}

		public Task PostRun (TestContext ctx, CancellationToken cancellationToken)
		{
			return Task.FromResult<object> (null);
		}

		public Task PreRun (TestContext ctx, CancellationToken cancellationToken)
		{
			return Task.FromResult<object> (null);
		}
	}
}

