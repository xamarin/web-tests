//
// BoringValidationTestRunner.cs
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
using System.Text;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using Xamarin.AsyncTests;
using Xamarin.AsyncTests.Constraints;
using Xamarin.WebTests.Resources;
using Xamarin.WebTests.ConnectionFramework;
using Xamarin.WebTests.MonoConnectionFramework;
using Xamarin.WebTests.MonoTestFramework;
using Mono.Btls.Interface;

namespace Mono.Btls.TestFramework
{
	[BoringValidationTestRunner]
	public class BoringValidationTestRunner : ValidationTestRunner
	{
		new public BoringValidationTestParameters Parameters {
			get {
				return (BoringValidationTestParameters)base.Parameters;
			}
		}

		public BoringValidationTestType Type {
			get { return Parameters.Type; }
		}

		public BoringValidationTestRunner (BoringValidationTestParameters parameters)
			: base (parameters)
		{
		}

		public static IEnumerable<BoringValidationTestType> GetTestTypes (TestContext ctx, ValidationTestCategory category)
		{
			switch (category) {
			case ValidationTestCategory.Default:
				yield return BoringValidationTestType.Simple;
				yield return BoringValidationTestType.NoTrustedRoots;
				yield return BoringValidationTestType.ExplicitTrustedRoot;
				yield return BoringValidationTestType.SslClientParameters;
				yield return BoringValidationTestType.WrongPurpose;
				yield return BoringValidationTestType.BeforeExpirationDate;
				yield return BoringValidationTestType.AfterExpirationDate;
				yield return BoringValidationTestType.CorrectHost;
				yield return BoringValidationTestType.IncorrectHost;
				yield return BoringValidationTestType.MissingIntermediateCert;
				yield return BoringValidationTestType.WrongChainOrder;
				yield return BoringValidationTestType.IntermediateServer;
				yield return BoringValidationTestType.IntermediateServerChain;
				yield return BoringValidationTestType.SelfSignedServer;
				yield return BoringValidationTestType.MartinTest;
				yield break;

			default:
				ctx.AssertFail ("Unspported validation category: '{0}.", category);
				yield break;
			}
		}

		public static IEnumerable<BoringValidationTestParameters> GetParameters (TestContext ctx, ValidationTestCategory category)
		{
			return GetTestTypes (ctx, category).Select (t => Create (ctx, category, t));
		}

		static BoringValidationTestParameters CreateParameters (ValidationTestCategory category, BoringValidationTestType type, params object[] args)
		{
			var sb = new StringBuilder ();
			sb.Append (type);
			foreach (var arg in args) {
				sb.AppendFormat (":{0}", arg);
			}
			var name = sb.ToString ();

			return new BoringValidationTestParameters (category, type, name);
		}

		static BoringValidationTestParameters Create (TestContext ctx, ValidationTestCategory category, BoringValidationTestType type)
		{
			var parameters = CreateParameters (category, type);

			switch (type) {
			case BoringValidationTestType.Simple:
				parameters.Add (CertificateResourceType.TlsTestXamDevNew);
				parameters.Add (CertificateResourceType.TlsTestXamDevCA);
				parameters.VerifyParamType = BoringVerifyParamType.None;
				parameters.AddTrustedRoots = true;
				parameters.ExpectSuccess = true;
				break;

			case BoringValidationTestType.SslClientParameters:
				parameters.Add (CertificateResourceType.TlsTestXamDevNew);
				parameters.Add (CertificateResourceType.TlsTestXamDevCA);
				parameters.VerifyParamType = BoringVerifyParamType.SslClient;
				parameters.AddTrustedRoots = true;
				parameters.ExpectSuccess = true;
				break;

			case BoringValidationTestType.NoTrustedRoots:
				parameters.Add (CertificateResourceType.TlsTestXamDevNew);
				parameters.Add (CertificateResourceType.TlsTestXamDevCA);
				parameters.AddTrustedRoots = false;
				parameters.ExpectedResult = BtlsX509Error.UNABLE_TO_GET_ISSUER_CERT_LOCALLY;
				break;

			case BoringValidationTestType.ExplicitTrustedRoot:
				parameters.Add (CertificateResourceType.ServerCertificateFromLocalCA);
				parameters.AddTrustedRoot (CertificateResourceType.HamillerTubeCA);
				parameters.ExpectSuccess = true;
				break;

			case BoringValidationTestType.BeforeExpirationDate:
				parameters.Add (CertificateResourceType.TlsTestXamDevExpired);
				parameters.Add (CertificateResourceType.TlsTestXamDevCA);
				parameters.VerifyParamType = BoringVerifyParamType.CopySslServer;
				parameters.CheckTime = new DateTime (2016, 3, 17);
				parameters.AddTrustedRoots = true;
				parameters.ExpectSuccess = true;
				break;

			case BoringValidationTestType.AfterExpirationDate:
				parameters.Add (CertificateResourceType.TlsTestXamDevExpired);
				parameters.Add (CertificateResourceType.TlsTestXamDevCA);
				parameters.VerifyParamType = BoringVerifyParamType.CopySslServer;
				parameters.CheckTime = new DateTime (2016, 4, 17);
				parameters.ExpectedResult = BtlsX509Error.CERT_HAS_EXPIRED;
				parameters.AddTrustedRoots = true;
				break;

			case BoringValidationTestType.WrongPurpose:
				parameters.Add (CertificateResourceType.ServerCertificateFromLocalCA);
				parameters.AddTrustedRoot (CertificateResourceType.HamillerTubeCA);
				parameters.VerifyParamType = BoringVerifyParamType.SslClient;
				parameters.ExpectedResult = BtlsX509Error.INVALID_PURPOSE;
				break;

			case BoringValidationTestType.CorrectHost:
				parameters.Add (CertificateResourceType.ServerCertificateFromLocalCA);
				parameters.AddTrustedRoot (CertificateResourceType.HamillerTubeCA);
				parameters.VerifyParamType = BoringVerifyParamType.CopySslServer;
				parameters.Host = "Hamiller-Tube.local";
				parameters.ExpectSuccess = true;
				break;

			case BoringValidationTestType.IncorrectHost:
				parameters.Add (CertificateResourceType.ServerCertificateFromLocalCA);
				parameters.AddTrustedRoot (CertificateResourceType.HamillerTubeCA);
				parameters.VerifyParamType = BoringVerifyParamType.CopySslServer;
				parameters.Host = "Hamiller-TubeX.local";
				parameters.ExpectedResult = BtlsX509Error.HOSTNAME_MISMATCH;
				break;

			case BoringValidationTestType.MissingIntermediateCert:
				parameters.Add (CertificateResourceType.IntermediateServer);
				parameters.AddTrustedRoot (CertificateResourceType.HamillerTubeCA);
				parameters.VerifyParamType = BoringVerifyParamType.CopySslServer;
				parameters.Host = "intermediate-server.local";
				parameters.ExpectedResult = BtlsX509Error.UNABLE_TO_GET_ISSUER_CERT_LOCALLY;
				break;

			case BoringValidationTestType.IntermediateServer:
				parameters.Add (CertificateResourceType.IntermediateServer);
				parameters.Add (CertificateResourceType.IntermediateCA);
				parameters.AddTrustedRoot (CertificateResourceType.HamillerTubeCA);
				parameters.VerifyParamType = BoringVerifyParamType.CopySslServer;
				parameters.Host = "intermediate-server.local";
				parameters.ExpectSuccess = true;
				break;

			case BoringValidationTestType.IntermediateServerChain:
				parameters.Add (CertificateResourceType.IntermediateServer);
				parameters.Add (CertificateResourceType.IntermediateCA);
				parameters.AddTrustedRoot (CertificateResourceType.HamillerTubeCA);
				parameters.AddExpectedChainEntry (CertificateResourceType.IntermediateServer);
				parameters.AddExpectedChainEntry (CertificateResourceType.IntermediateCA);
				parameters.AddExpectedChainEntry (CertificateResourceType.HamillerTubeCA);
				parameters.VerifyParamType = BoringVerifyParamType.CopySslServer;
				parameters.Host = "intermediate-server.local";
				parameters.ExpectSuccess = true;
				break;

			case BoringValidationTestType.WrongChainOrder:
				parameters.Add (CertificateResourceType.IntermediateCA);
				parameters.Add (CertificateResourceType.IntermediateServer);
				parameters.AddTrustedRoot (CertificateResourceType.HamillerTubeCA);
				parameters.VerifyParamType = BoringVerifyParamType.CopySslServer;
				parameters.Host = "intermediate-server.local";
				parameters.ExpectedResult = BtlsX509Error.INVALID_PURPOSE;
				break;

			case BoringValidationTestType.SelfSignedServer:
				parameters.Add (CertificateResourceType.SelfSignedServerCertificate);
				parameters.VerifyParamType = BoringVerifyParamType.CopySslServer;
				parameters.ExpectedResult = BtlsX509Error.DEPTH_ZERO_SELF_SIGNED_CERT;
				break;

			case BoringValidationTestType.MartinTest:
				parameters.Add (CertificateResourceType.TlsTestXamDevNew);
				parameters.Add (CertificateResourceType.TlsTestXamDevCA);
				parameters.AddTrustedRoots = true;
				parameters.ExpectSuccess = true;
				break;

			default:
				ctx.AssertFail ("Unsupported validation type: '{0}'.", type);
				break;
			}

			return parameters;
		}

		BtlsX509Store store;
		BtlsX509Chain chain;
		BtlsX509StoreCtx storeCtx;
		BtlsX509VerifyParam verifyParam;

		protected override void PreRun (TestContext ctx)
		{
			base.PreRun (ctx);

			store = BtlsProvider.CreateNativeStore ();
			if (Parameters.AddTrustedRoots)
				store.AddTrustedRoots ();
			AddTrustedRoots (ctx);

			chain = BtlsProvider.CreateNativeChain ();
			SetupChain (ctx);

			storeCtx = BtlsProvider.CreateNativeStoreCtx ();
			storeCtx.Initialize (store, chain);

			verifyParam = SetupVerifyParameters (ctx);

			if (Parameters.CheckTime != null)
				verifyParam.SetTime (Parameters.CheckTime.Value);
			if (Parameters.Host != null)
				verifyParam.SetHost (Parameters.Host);

			if (verifyParam != null)
				storeCtx.SetVerifyParam (verifyParam);
		}

		protected BtlsX509VerifyParam SetupVerifyParameters (TestContext ctx)
		{
			switch (Parameters.VerifyParamType) {
			case BoringVerifyParamType.None:
				return null;
			case BoringVerifyParamType.SslClient:
				return BtlsProvider.GetVerifyParam_SslClient ();
			case BoringVerifyParamType.SslServer:
				return BtlsProvider.GetVerifyParam_SslServer ();
			case BoringVerifyParamType.CopySslClient:
				return BtlsProvider.GetVerifyParam_SslClient ().Copy ();
			case BoringVerifyParamType.CopySslServer:
				return BtlsProvider.GetVerifyParam_SslServer ().Copy ();
			default:
				ctx.AssertFail ("Unsupported VerifyParamType: '{0}'.", Parameters.VerifyParamType);
				throw new InvalidOperationException ();
			}
		}

		protected void AddTrustedRoots (TestContext ctx)
		{
			if (Parameters.TrustedRoots == null)
				return;
			var certificates = new X509CertificateCollection ();
			foreach (var type in Parameters.TrustedRoots) {
				var certificate = ResourceManager.GetCertificate (type);
				certificates.Add (certificate);
			}
			var trust = BtlsX509TrustKind.TRUST_ALL;
			store.AddLookup (certificates, trust);
		}

		protected void SetupChain (TestContext ctx)
		{
			foreach (var type in Parameters.Types) {
				var data = ResourceManager.GetCertificateData (type);
				var x509 = BtlsProvider.CreateNative (data, BtlsX509Format.PEM);
				chain.Add (x509);
			}
		}

		protected override void PostRun (TestContext ctx)
		{
			if (verifyParam != null) {
				verifyParam.Dispose ();
				verifyParam = null;
			}
			if (storeCtx != null) {
				storeCtx.Dispose ();
				storeCtx = null;
			}
			if (chain != null) {
				chain.Dispose ();
				chain = null;
			}
			if (store != null) {
				store.Dispose ();
				store = null;
			}
			base.PostRun (ctx);
		}

		public override void Run (TestContext ctx)
		{
			ctx.LogMessage ("BORING VALIDATION RUNNER: {0}", store);
			var result = storeCtx.Verify ();
			var error = storeCtx.GetError ();
			ctx.LogMessage ("BORING VALIDATION RUNNER #1: {0} {1}", result, error);

			if (Parameters.ExpectSuccess) {
				ctx.Assert (result, Is.EqualTo (1), "validation success");
				ctx.Assert (error, Is.EqualTo (BtlsX509Error.OK), "success result");
			} else {
				ctx.Assert (result, Is.EqualTo (0), "validation failed");
				ctx.Assert (error, Is.EqualTo (Parameters.ExpectedResult), "validation result");
			}

			using (var nativeChain = storeCtx.GetChain ())
			using (var managedChain = BtlsProvider.GetManagedChain (nativeChain)) {
				ExpectManagedChain (ctx, managedChain);
				ExpectNativeChain (ctx, nativeChain);
			}
		}

		bool ExpectManagedChain (TestContext ctx, X509Chain managedChain)
		{
			bool ok = true;
			if (false && Parameters.ExpectSuccess) {
				if (ctx.Expect (managedChain.ChainStatus, Is.Not.Null, "X509Chain.ChainStatus")) {
					ok &= ctx.Expect (managedChain.ChainStatus.Length, Is.EqualTo (0), "X509Chain.ChainStatus.Length");
				}
			}
			return ok;
		}

		bool ExpectNativeChain (TestContext ctx, BtlsX509Chain nativeChain)
		{
			bool ok = true;
			if (Parameters.ExpectedChain != null) {
				ok &= ctx.Expect (nativeChain.Count, Is.EqualTo (Parameters.ExpectedChain.Count), "chain length");
				if (ok) {
					for (int i = 0; i < nativeChain.Count; i++) {
						ok &= ExpectChainItem (ctx, nativeChain, i);
					}
				}
			}
			return ok;
		}

		bool ExpectChainItem (TestContext ctx, BtlsX509Chain nativeChain, int index)
		{
			using (var item = nativeChain[index]) {
				var expected = ResourceManager.GetCertificateInfo (Parameters.ExpectedChain[index]);
				return ctx.Expect (item.GetCertHash (), Is.EqualTo (expected.Hash), "chain item " + index);
			}
		}
	}
}
