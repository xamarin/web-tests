//
// HttpWebRequestProvider.cs
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
using System.Reflection;

namespace Xamarin.WebTests.Server
{
	using Portable;

	class HttpWebRequestProvider : IHttpWebRequestProvider
	{
		readonly PropertyInfo callbackProp;

		public HttpWebRequestProvider ()
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

		public IHttpWebRequest Create (Uri uri)
		{
			var request = (HttpWebRequest)HttpWebRequest.Create (uri);
			return new HttpWebRequestImpl (request);
		}

		public IHttpWebRequest Create (HttpWebRequest request)
		{
			return new HttpWebRequestImpl (request);
		}

		public void InstallDefaultCertificateValidator (ICertificateValidator validator)
		{
			ServicePointManager.ServerCertificateValidationCallback = ((CertificateValidator)validator).ValidationCallback;
		}

		public bool SupportsPerRequestValidator {
			get { return callbackProp != null; }
		}

		public void InstallCertificateValidator (IHttpWebRequest request, ICertificateValidator validator)
		{
			if (!SupportsPerRequestValidator)
				throw new NotSupportedException ();
			callbackProp.SetValue (request.Request, ((CertificateValidator)validator).ValidationCallback);
		}
	}
}

