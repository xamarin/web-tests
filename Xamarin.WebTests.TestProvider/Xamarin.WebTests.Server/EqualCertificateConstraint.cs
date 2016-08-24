//
// EqualCertificateConstraint.cs
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
using System.Security.Cryptography.X509Certificates;
using Xamarin.AsyncTests.Constraints;

namespace Xamarin.WebTests.Server
{
	class EqualCertificateConstraint : Constraint
	{
		public X509Certificate Expected {
			get;
			private set;
		}

		public EqualCertificateConstraint (X509Certificate expected)
		{
			Expected = expected;
		}

		public override bool Evaluate (object actual, out string message)
		{
			if (actual == null) {
				message = string.Format ("Expected certificate, but got null.");
				return false;
			}

			var actualCert = actual as X509Certificate;
			if (actualCert == null) {
				message = string.Format ("Expected X509Certificate, got instance of type `{0}'.", actual.GetType ());
				return false;
			}

			var expectedHash = Expected.GetCertHashString ();
			var actualHash = actualCert.GetCertHashString ();

			if (string.Equals (actualHash, expectedHash)) {
				message = null;
				return true;
			}

			message = string.Format ("Expected certificate `{0}' ({1}), got `{2}' ({3}).",
			                         Expected.Subject, Expected.GetSerialNumberString (),
			                         actualCert.Subject, actualCert.GetSerialNumberString ());
			return false;
		}

		public override string Print ()
		{
			return string.Format ("Equal({0}:{1})", Expected.GetSerialNumberString (), Expected.Subject);
		}

	}
}

