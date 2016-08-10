//
// CertificateInfoTestRunner.cs
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
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using Xamarin.AsyncTests;
using Xamarin.AsyncTests.Constraints;
using Xamarin.WebTests.ConnectionFramework;
using Xamarin.WebTests.Resources;
using Xamarin.WebTests.TestFramework;

namespace Xamarin.WebTests.TestRunners
{
	public static class CertificateInfoTestRunner
	{
		public static void TestManagedCertificate (TestContext ctx, X509Certificate2 cert, CertificateInfo expected, bool debug = false)
		{
			var subject = cert.SubjectName;
			if (debug)
				ctx.LogMessage ("MANAGED SUBJECT: {0}", subject.Name);
			if (ctx.Expect (subject, Is.Not.Null, "SubjectName")) {
				ctx.Expect (subject.Name, Is.EqualTo (expected.ManagedSubjectName), "SubjectName.Name");
			}

			var issuer = cert.IssuerName;
			if (debug)
				ctx.LogMessage ("MANAGED ISSUER: {0}", issuer.Name);
			if (ctx.Expect (issuer, Is.Not.Null, "IssuerName")) {
				ctx.Expect (issuer.Name, Is.EqualTo (expected.ManagedIssuerName), "IssuerName.Name");
			}

			ctx.Expect (cert.Subject, Is.EqualTo (expected.ManagedSubjectName), "Subject");
			ctx.Expect (cert.Issuer, Is.EqualTo (expected.ManagedIssuerName), "Issue");
			ctx.Expect (cert.NotBefore.ToUniversalTime (), Is.EqualTo (expected.NotBefore), "NotBefore");
			ctx.Expect (cert.NotAfter.ToUniversalTime (), Is.EqualTo (expected.NotAfter), "NotAfter");
			ctx.Expect (cert.GetCertHash (), Is.EqualTo (expected.Hash), "GetCertHash()");

			ctx.Expect (cert.GetSerialNumber (), Is.EqualTo (expected.SerialNumberMono), "GetSerialNumber()");

			ctx.Expect (cert.Version, Is.EqualTo (expected.Version), "Version");

			ctx.Expect (cert.GetPublicKey (), Is.EqualTo (expected.PublicKeyData), "GetPublicKey()");

			var signatureAlgorithm = cert.SignatureAlgorithm;
			if (ctx.Expect (signatureAlgorithm, Is.Not.Null, "SignatureAlgorithm"))
				ctx.Expect (signatureAlgorithm.Value, Is.EqualTo (expected.SignatureAlgorithmOid), "SignatureAlgorithm.Value");

			var publicKey = cert.PublicKey;
			if (ctx.Expect (publicKey, Is.Not.Null, "PublicKey")) {
				if (ctx.Expect (publicKey.Oid, Is.Not.Null, "PublicKey.Oid"))
					ctx.Expect (publicKey.Oid.Value, Is.EqualTo (expected.PublicKeyAlgorithmOid), "PublicKey.Oid.Value");

				var value = publicKey.EncodedKeyValue;
				if (ctx.Expect (value, Is.Not.Null, "PublicKey.EncodedKeyValue")) {
					if (ctx.Expect (value.Oid, Is.Not.Null, "PublicKey.Oid"))
						ctx.Expect (value.Oid.Value, Is.EqualTo (expected.PublicKeyAlgorithmOid), "PublicKey.Oid.Value");

					ctx.Expect (value.RawData, Is.EqualTo (expected.PublicKeyData), "PublicKey.RawData");
				}

				var publicKeyParams = publicKey.EncodedParameters;
				if (ctx.Expect (publicKeyParams, Is.Not.Null, "PublicKey.EncodedParameters")) {
					if (ctx.Expect (publicKeyParams.Oid, Is.Not.Null, "PublicKey.EncodedParameters.Oid"))
						ctx.Expect (publicKeyParams.Oid.Value, Is.EqualTo (expected.PublicKeyAlgorithmOid), "PublicKey.EncodedParameters.Oid.Value");
					ctx.Expect (publicKeyParams.RawData, Is.EqualTo (expected.PublicKeyParameters), "PublicKey.EncodedParameters.RawData");
				}
			}
		}

		public static void CheckCallbackChain (TestContext ctx, ConnectionTestParameters parameters,
		                                       X509Certificate certificate, X509Chain chain,
		                                       SslPolicyErrors errors)
		{
			if (parameters.ExpectPolicyErrors != null) {
				// FIXME: AppleTls reports RemoteCertificateChainErrors instead of RemoteCertificateNameMismatch.
				if (parameters.ExpectPolicyErrors.Value == SslPolicyErrors.RemoteCertificateNameMismatch) {
					ctx.Expect (errors, Is.EqualTo (SslPolicyErrors.RemoteCertificateNameMismatch).Or.EqualTo (SslPolicyErrors.RemoteCertificateChainErrors));
				} else {
					ctx.Expect (errors, Is.EqualTo (parameters.ExpectPolicyErrors.Value));
				}
			}

			if (parameters.ExpectChainStatus != null && ctx.Expect (chain, Is.Not.Null, "chain")) {
				if (ctx.Expect (chain.ChainStatus, Is.Not.Null, "chain.ChainStatus")) {
					ctx.Expect (chain.ChainStatus.Length, Is.EqualTo (1), "chain.ChainStatus.Length");
					ctx.Expect (chain.ChainStatus[0].Status, Is.EqualTo (parameters.ExpectChainStatus.Value), "chain.ChainStatus.Status");
					ctx.LogMessage ("STATUS: {0}", chain.ChainStatus[0].Status);
				}
				if (ctx.Expect (chain.ChainElements, Is.Not.Null, "chain.ChainElements")) {
					ctx.Expect (chain.ChainElements.Count, Is.EqualTo (1), "chain.ChainElements.Count");
				}
				ctx.LogMessage ("ELEMENTS: {0}", chain.ChainElements[0].ChainElementStatus[0].Status);
			}
		}

		public static void CheckValidationResult (TestContext ctx, ValidationParameters parameters,
							  X509Certificate certificate, X509Chain chain, SslPolicyErrors errors)
		{
			var provider = DependencyInjector.Get<ICertificateProvider> ();
			if (parameters.ExpectedExtraStore != null && ctx.Expect (chain, Is.Not.Null, "chain")) {
				if (ctx.Expect (chain.ChainPolicy, Is.Not.Null,  "chain.ChainPolicy") &&
				    ctx.Expect (chain.ChainPolicy.ExtraStore, Is.Not.Null, "ChainPolicy.ExtraStore") &&
				    ctx.Expect (chain.ChainPolicy.ExtraStore.Count, Is.EqualTo (parameters.ExpectedExtraStore.Count), "ChainPolicy.ExtraStore.Count")) {
					for (int i = 0; i < parameters.ExpectedExtraStore.Count; i++) {
						ctx.LogMessage ("TEST!");
						ExpectCertificate (ctx, chain.ChainPolicy.ExtraStore[i], parameters.ExpectedExtraStore[i], string.Format ("ExtraStore[{0}]", i));
					}
				}
			}

			if (parameters.ExpectSuccess)
				ctx.Assert (errors, Is.EqualTo (SslPolicyErrors.None), "expecting success");
			else
				ctx.Assert (errors, Is.Not.EqualTo (SslPolicyErrors.None), "expecting failure");
		}

		public static bool ExpectCertificate (TestContext ctx, X509Certificate actual, CertificateResourceType expected, string message)
		{
			var provider = DependencyInjector.Get<ICertificateProvider> ();
			var expectedCert = ResourceManager.GetCertificate (expected);
			return ctx.Expect (provider.AreEqual (actual, expectedCert), message);
		}
	}
}

