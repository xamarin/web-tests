//
// HttpWebRequestImpl.cs
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
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Security.Cryptography.X509Certificates;

namespace Xamarin.WebTests.Server
{
	using Portable;

	class HttpWebRequestImpl : IHttpWebRequest
	{
		static readonly PropertyInfo callbackProp;

		static HttpWebRequestImpl ()
		{
			/*
			 * We use reflection to support older Mono runtimes, which don't have the property.
			 *
			 * HttpWebRequest.ServerCertificateValidationCallback is a new public property in .NET 4.5,
			 * but has not been added to mono/master yet.
			 */
			var type = typeof(HttpWebRequest);
			callbackProp = type.GetProperty ("ServerCertificateValidationCallback", BindingFlags.Public | BindingFlags.Instance);
		}

		public HttpWebRequest Request {
			get;
			private set;
		}

		public HttpWebRequestImpl (HttpWebRequest request)
		{
			Request = request;	
		}

		public void SetProxy (IPortableProxy proxy)
		{
			Request.Proxy = (PortableProxy)proxy;
		}

		public void SetAllowWriteStreamBuffering (bool value)
		{
			Request.AllowWriteStreamBuffering = value;
		}

		public void SetKeepAlive (bool value)
		{
			Request.KeepAlive = value;
		}

		public void SetSendChunked (bool value)
		{
			Request.SendChunked = value;
		}

		public void SetContentLength (long length)
		{
			Request.ContentLength = length;
		}

		public Stream GetRequestStream ()
		{
			return Request.GetRequestStream ();
		}

		public Task<Stream> GetRequestStreamAsync ()
		{
			return Request.GetRequestStreamAsync ();
		}

		public HttpWebResponse GetResponse ()
		{
			return (HttpWebResponse)Request.GetResponse ();
		}

		public async Task<HttpWebResponse> GetResponseAsync ()
		{
			return (HttpWebResponse)await Request.GetResponseAsync ();
		}

		internal static bool SupportsCertificateValidator {
			get { return callbackProp != null; }
		}

		public void InstallCertificateValidator (ICertificateValidator validator)
		{
			if (!SupportsCertificateValidator)
				throw new NotSupportedException ();
			callbackProp.SetValue (Request, ((CertificateValidator)validator).ValidationCallback);
		}

		public ICertificate GetCertificate ()
		{
			var certificate = Request.ServicePoint.Certificate;
			if (certificate == null)
				return null;
			return CertificateProvider.GetCertificate (certificate);
		}

		public ICertificate GetClientCertificate ()
		{
			var certificate = Request.ServicePoint.ClientCertificate;
			if (certificate == null)
				return null;
			return CertificateProvider.GetCertificate (certificate);
		}

		public void SetClientCertificates (IClientCertificate[] clientCertificates)
		{
			var certificates = new X509CertificateCollection ();
			foreach (var certificate in clientCertificates) {
				certificates.Add (CertificateProvider.GetCertificate (certificate));
			}
			Request.ClientCertificates = certificates;
		}
	}
}

