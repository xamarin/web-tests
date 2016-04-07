//
// CertificateInfo.cs
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

namespace Xamarin.WebTests.Resources
{
	public abstract class CertificateInfo
	{
		readonly CertificateResourceType type;
		readonly byte[] rawData;

		protected const string Oid_Rsa = "1.2.840.113549.1.1.1";
		protected const string Oid_RsaWithSha1 = "1.2.840.113549.1.1.5";
		protected const string Oid_RsaWithSha256 = "1.2.840.113549.1.1.11";

		protected static readonly byte[] EmptyPublicKeyParameters = new byte[] {
			0x05, 0x00
		};

		protected CertificateInfo (CertificateResourceType type, byte[] rawData)
		{
			this.type = type;
			this.rawData = rawData;
		}

		public CertificateResourceType ResourceType {
			get {
				return type;
			}
		}

		public byte[] RawData {
			get {
				return rawData;
			}
		}

		public abstract string ManagedSubjectName { get; }
		public abstract string ManagedIssuerName { get; }

		public abstract byte[] Hash { get; }
		public abstract CertificateNameInfo IssuerName { get; }
		public abstract string IssuerNameString { get; }
		public abstract DateTime NotAfter { get; }
		public abstract DateTime NotBefore { get; }
		public abstract string PublicKeyAlgorithmOid { get; }
		public abstract byte[] PublicKeyData { get; }
		public abstract byte[] PublicKeyParameters { get; }

		public abstract byte[] SerialNumber { get; }
		public abstract byte[] SerialNumberMono { get; }
		public abstract string SignatureAlgorithmOid { get; }
		public abstract CertificateNameInfo SubjectName { get; }
		public abstract string SubjectNameString { get; }
		public abstract int Version { get; }
	}
}

