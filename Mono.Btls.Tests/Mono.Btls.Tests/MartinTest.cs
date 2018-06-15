//
// MartinTest.cs
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
using System.Security.Cryptography.X509Certificates;
using Xamarin.AsyncTests;
using Xamarin.AsyncTests.Constraints;
using Xamarin.WebTests.TestFramework;
using Xamarin.WebTests.Resources;
using Xamarin.WebTests.MonoTestFeatures;
using Xamarin.WebTests.MonoTestFramework;
using Mono.Btls.Interface;
using Mono.Btls.TestFramework;
using Mono.Security.Interface;
using Xamarin.WebTests.TestAttributes;
using Xamarin.WebTests.HttpFramework;
using Xamarin.WebTests.TestRunners;
using Xamarin.WebTests.ConnectionFramework;
using Xamarin.WebTests.HttpHandlers;
using Xamarin.WebTests.HttpOperations;
using Xamarin.WebTests.MonoConnectionFramework;

namespace Mono.Btls.Tests
{
	[Experimental]
	[AsyncTestFixture]
	public class MartinTest
	{
		// [AsyncTest]
		[CertificateResourceCollection (CertificateResourceCollectionType.TlsTestXamDev)]
		public void TestStore (TestContext ctx,
		                       BoringX509StoreHost store,
		                       BoringX509ChainHost chain)
		{
			ctx.LogMessage ("TEST STORE: {0}", store);

			using (var storeCtx = BtlsProvider.CreateNativeStoreCtx ()) {
				storeCtx.Initialize (store.Instance, chain.Instance);
				TestStore (ctx, storeCtx);
			}
		}

		void TestStore (TestContext ctx, BtlsX509StoreCtx storeCtx)
		{
			ctx.LogMessage ("CALLING VERIFY");
			var ret = storeCtx.Verify ();
			ctx.LogMessage ("VERIFY DONE: {0}", ret);
		}

		// [Martin]
		// [AsyncTest]
		public void Hello (TestContext ctx)
		{
			var chain = BtlsProvider.CreateNativeChain ();
			ctx.LogMessage ("GOT CHAIN: {0}", chain);

			var tlsTestData = ResourceManager.GetCertificateData (CertificateResourceType.TlsTestXamDevExpired2);
			var tlsTestCaData = ResourceManager.GetCertificateData (CertificateResourceType.TlsTestXamDevOldCA);

			var tlsTest = BtlsProvider.CreateNative (tlsTestData, BtlsX509Format.PEM);
			var tlsTestCa = BtlsProvider.CreateNative (tlsTestCaData, BtlsX509Format.PEM);

			ctx.LogMessage ("LET'S BUILD IT!");

			chain.Add (tlsTest);
			chain.Add (tlsTestCa);

			var store = BtlsProvider.CreateNativeStore ();
			store.AddTrustedRoots ();

			var storeCtx = BtlsProvider.CreateNativeStoreCtx ();
			storeCtx.Initialize (store, chain);

			var param = BtlsProvider.GetVerifyParam_SslClient ().Copy ();
			param.SetHost ("test!");
			param.AddHost ("martin.xamdev.com");

			var flags = param.GetFlags ();
			ctx.LogMessage ("FLAGS: {0:x}", flags);
			// param.SetFlags (BoringX509VerifyFlags.CRL_CHECK);
			// param.SetPurpose (BoringX509Purpose.SMIME_ENCRYPT);

			param.SetTime (DateTime.Now.AddDays (3));

			storeCtx.SetVerifyParam (param);

			ctx.LogMessage ("CALLING VERIFY");
			var ret = storeCtx.Verify ();
			ctx.LogMessage ("VERIFY DONE: {0}", ret);

			var error = storeCtx.GetError ();
			ctx.LogMessage ("VERIFY ERROR: {0}", error);

			ctx.LogMessage ("STORE COUNT: {0}", store.GetCount ());
		}

		[Work]
		[AsyncTest]
		[HttpServerFlags (HttpServerFlags.SSL)]
		public async Task TestWebServer (TestContext ctx, HttpServer server, CancellationToken cancellationToken)
		{
			var handler = new HelloWorldHandler ("Hello World");
			using (var operation = new TraditionalOperation (server, handler, true))
				await operation.Run (ctx, cancellationToken).ConfigureAwait (false);

		}

		// [Martin]
		// [AsyncTest]
		[ProtocolVersion (ProtocolVersions.Tls12)]
		[ConnectionTestCategory (ConnectionTestCategory.MartinTest)]
		public void TestProvider (
			TestContext ctx, CancellationToken cancellationToken,
			[ConnectionTestProvider ("btls")] ConnectionTestProvider provider)
		{
			ctx.LogMessage ("TEST PROVIDER: {0}", provider);
			var tlsProvider = MonoTlsProviderFactory.GetProvider ();
			ctx.LogMessage ("DEFAULT TLS PROVIDER: {0} {1} {2}", tlsProvider, tlsProvider.Name, tlsProvider.ID);
		}
	}
}

