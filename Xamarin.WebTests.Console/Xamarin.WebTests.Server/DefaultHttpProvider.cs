//
// DefaultHttpProvider.cs
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
using System.Net;
using System.Security.Authentication;
using System.Net.Security;
using Http = System.Net.Http;
using Xamarin.AsyncTests;
using Xamarin.AsyncTests.Portable;

namespace Xamarin.WebTests.Server
{
	using Framework;
	using Portable;
	using Providers;
	using HttpClient;

	class DefaultHttpProvider : IHttpProvider, ISslStreamProvider
	{
		public bool SupportsHttpClient {
			get { return true; }
		}

		public IHttpClientHandler CreateHttpClient ()
		{
			var handler = new Http.HttpClientHandler ();
			return new HttpClientHandler (handler);
		}

		public bool SupportsWebRequest {
			get { return true; }
		}

		public IHttpWebRequest CreateWebRequest (Uri uri)
		{
			var request = (HttpWebRequest)HttpWebRequest.Create (uri);
			return new HttpWebRequestImpl (request);
		}

		public IHttpWebRequest CreateWebRequest (HttpWebRequest request)
		{
			return new HttpWebRequestImpl (request);
		}

		public ISslStreamProvider SslStreamProvider {
			get { return null; }
		}

		public ISslStreamProvider DefaultSslStreamProvider {
			get { return this; }
		}

		public HttpServer CreateServer (IPortableEndPoint endpoint, ListenerFlags flags, IServerCertificate serverCertificate)
		{
			return new HttpServer (this, endpoint, flags, serverCertificate);
		}

		public Stream CreateServerStream (Stream stream, IServerCertificate serverCertificate, ICertificateValidator validator, ListenerFlags flags)
		{
			var certificate = CertificateProvider.GetCertificate (serverCertificate);

			var clientCertificateRequired = (flags & ListenerFlags.RequireClientCertificate) != 0;
			var protocols = (SslProtocols)ServicePointManager.SecurityProtocol;

			var sslStream = new SslStream (stream, false, ((CertificateValidator)validator).ValidationCallback);
			sslStream.AuthenticateAsServer (certificate, clientCertificateRequired, protocols, false);

			if (clientCertificateRequired && !sslStream.IsMutuallyAuthenticated)
				throw new WebException ("Not mutually authenticated", System.Net.WebExceptionStatus.TrustFailure);

			return sslStream;
		}
	}
}

