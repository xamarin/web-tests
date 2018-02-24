//
// HttpValidationTestParameters.cs
//
// Author:
//       Martin Baulig <martin.baulig@xamarin.com>
//
// Copyright (c) 2015 Xamarin, Inc.
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
using System.Net;
using System.Security.Cryptography.X509Certificates;

namespace Xamarin.WebTests.TestFramework
{
	using ConnectionFramework;
	using HttpFramework;
	using TestAttributes;
	using TestRunners;
	using Resources;

	[HttpValidationTestParameters]
	public class HttpValidationTestParameters : ConnectionTestParameters
	{
		public HttpValidationTestType Type {
			get;
		}

		public HttpValidationTestParameters (ConnectionTestCategory category, HttpValidationTestType type, string identifier, X509Certificate certificate)
			: base (category, identifier, certificate)
		{
			Type = type;
		}

		public HttpValidationTestParameters (ConnectionTestCategory category, HttpValidationTestType type, string identifier, CertificateResourceType certificate)
			: base (category, identifier, null)
		{
			Type = type;
			CertificateType = certificate;
		}

		protected HttpValidationTestParameters (HttpValidationTestParameters other)
			: base (other)
		{
			Type = other.Type;
			CertificateType = other.CertificateType;
			ExternalServer = other.ExternalServer;
			OverrideTargetHost = other.OverrideTargetHost;
			SendChunked = other.SendChunked;
			ChunkedResponse = other.ChunkedResponse;
			ExpectedStatus = other.ExpectedStatus;
			ExpectedError = other.ExpectedError;
			Flags = other.Flags;
		}

		HttpStatusCode expectedStatus = HttpStatusCode.OK;
		WebExceptionStatus exepectedError = WebExceptionStatus.Success;

		public override ConnectionParameters DeepClone ()
		{
			return new HttpValidationTestParameters (this);
		}

		public CertificateResourceType? CertificateType {
			get; set;
		}

		public bool SendChunked {
			get; set;
		}

		public bool ChunkedResponse {
			get; set;
		}

		public Uri ExternalServer {
			get; set;
		}

		public string OverrideTargetHost {
			get; set;
		}

		public HttpStatusCode ExpectedStatus {
			get { return expectedStatus; }
			set { expectedStatus = value; }
		}

		public WebExceptionStatus ExpectedError {
			get { return exepectedError; }
			set { exepectedError = value; }
		}

		public HttpOperationFlags Flags {
			get; set;
		}
	}
}

