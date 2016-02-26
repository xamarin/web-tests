//
// DotNetSslStream.cs
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
using System.IO;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using Xamarin.AsyncTests;

namespace Xamarin.WebTests.ConnectionFramework
{
	class DotNetSslStream : ISslStream
	{
		readonly SslStream stream;

		public DotNetSslStream (SslStream stream)
		{
			this.stream = stream;
		}

		public bool IsAuthenticated {
			get { return stream.IsAuthenticated; }
		}

		public bool IsMutuallyAuthenticated {
			get { return stream.IsMutuallyAuthenticated; }
		}

		public bool HasLocalCertificate {
			get { return stream.LocalCertificate != null; }
		}

		public bool HasRemoteCertificate {
			get { return stream.RemoteCertificate != null; }
		}

		public X509Certificate RemoteCertificate {
			get {
				var certificate = stream.RemoteCertificate;
				if (certificate == null)
					throw new InvalidOperationException ();

				return certificate;
			}
		}

		public Stream AuthenticatedStream {
			get { return stream; }
		}

		public ProtocolVersions ProtocolVersion {
			get { return (ProtocolVersions)stream.SslProtocol; }
		}

		public void Close ()
		{
			stream.Dispose ();
		}
	}
}

