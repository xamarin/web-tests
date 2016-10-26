//
// BoringCertificateInfoTestRunner.cs
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
using Xamarin.AsyncTests;
using Xamarin.AsyncTests.Constraints;
using Xamarin.WebTests.Resources;
using Mono.Btls.Interface;

namespace Mono.Btls.TestFramework
{
	public static class BoringCertificateInfoTestRunner
	{
		public static void PrintName (TestContext ctx, BtlsX509Name name)
		{
			ctx.LogMessage ("STRING: {0}", name.GetString ());
			ctx.LogMessage ("HASH: {0:x}", name.GetHash ());
			ctx.LogMessage ("HASH OLD: {0:x}", name.GetHashOld ());
			ctx.LogBufferAsCSharp ("rawData", "\t\t", name.GetRawData (false));
			ctx.LogBufferAsCSharp ("rawDataCanon", "\t\t", name.GetRawData (true));
		}

		public static void TestName (TestContext ctx, BtlsX509Name actual, CertificateNameInfo expected, string label)
		{
			ctx.Expect (actual.GetString (), Is.EqualTo (expected.String), label + ".String");
			ctx.Expect (actual.GetHash (), Is.EqualTo (expected.Hash), label + ".Hash");
			ctx.Expect (actual.GetHashOld (), Is.EqualTo (expected.HashOld), label + ".HashOld");
			ctx.Expect (actual.GetRawData (false), Is.EqualTo (expected.RawData), label + ".RawData");
			ctx.Expect (actual.GetRawData (true), Is.EqualTo (expected.RawDataCanon), label + ".RawDataCanon");
		}

		public static void TestNativeCertificate (TestContext ctx, BtlsX509 x509, CertificateInfo expected, bool debug = false)
		{
			using (var subjectName = x509.GetSubjectName ()) {
				if (debug)
					PrintName (ctx, subjectName);
				TestName (ctx, subjectName, expected.SubjectName, "GetSubjectName()");
			}

			using (var issuerName = x509.GetIssuerName ()) {
				if (debug)
					PrintName (ctx, issuerName);
				TestName (ctx, issuerName, expected.IssuerName, "GetIssuerName()");
			}

			if (debug) {
				ctx.LogMessage ("NOT BEFORE: {0}", x509.GetNotBefore ());
				ctx.LogMessage ("NOT AFTER: {0}", x509.GetNotAfter ());
				ctx.LogBufferAsCSharp ("serial", "\t\t", x509.GetSerialNumber (false));
				ctx.LogBufferAsCSharp ("serialMono", "\t\t", x509.GetSerialNumber (true));
				ctx.LogBufferAsCSharp ("hash", "\t\t", x509.GetCertHash ());
			}

			ctx.Expect (x509.GetSubjectNameString (), Is.EqualTo (expected.SubjectNameString), "GetSubjectNameString()");
			ctx.Expect (x509.GetIssuerNameString (), Is.EqualTo (expected.IssuerNameString), "GetIssuerNameString()");
			ctx.Expect (x509.GetNotBefore (), Is.EqualTo (expected.NotBefore), "GetNotBefore()");
			ctx.Expect (x509.GetNotAfter (), Is.EqualTo (expected.NotAfter), "GetNotAfter()");
			ctx.Expect (x509.GetCertHash (), Is.EqualTo (expected.Hash), "GetCertHash()");

			ctx.Expect (x509.GetSerialNumber (false), Is.EqualTo (expected.SerialNumber), "GetSerialNumber(false)");
			ctx.Expect (x509.GetSerialNumber (true), Is.EqualTo (expected.SerialNumberMono), "GetSerialNumber(true)");

			ctx.Expect (x509.GetVersion (), Is.EqualTo (expected.Version), "GetVersion()");

			if (debug)
				ctx.LogBufferAsCSharp ("publicKey", "\t\t", x509.GetPublicKeyData ());

			ctx.Expect (x509.GetPublicKeyData (), Is.EqualTo (expected.PublicKeyData), "GetPublicKeyData()");

			var signatureAlgorithm = x509.GetSignatureAlgorithm ();
			if (ctx.Expect (signatureAlgorithm, Is.Not.Null, "GetSignatureAlgorithm()"))
				ctx.Expect (signatureAlgorithm.Value, Is.EqualTo (expected.SignatureAlgorithmOid), "GetSignatureAlgorithm().Value");

			var publicKey = x509.GetPublicKeyAsn1 ();
			if (ctx.Expect (publicKey, Is.Not.Null, "GetPublicKeyAsn1()")) {
				if (ctx.Expect (publicKey.Oid, Is.Not.Null, "GetPublicKeyAsn1().Oid"))
					ctx.Expect (publicKey.Oid.Value, Is.EqualTo (expected.PublicKeyAlgorithmOid), "GetPublicKeyAsn1().Oid.Value");
				ctx.Expect (publicKey.RawData, Is.EqualTo (expected.PublicKeyData), "GetPublicKeyAsn1().RawData");
			}

			var publicKeyParams = x509.GetPublicKeyParameters ();
			if (ctx.Expect (publicKeyParams, Is.Not.Null, "GetPublicKeyParameters()")) {
				if (ctx.Expect (publicKeyParams.Oid, Is.Not.Null, "GetPublicKeyParameters().Oid"))
					ctx.Expect (publicKeyParams.Oid.Value, Is.EqualTo (expected.PublicKeyAlgorithmOid), "GetPublicKeyParameters().Oid.Value");
				ctx.Expect (publicKeyParams.RawData, Is.EqualTo (expected.PublicKeyParameters), "GetPublicKeyParameters().RawData");
			}
		}
	}
}

