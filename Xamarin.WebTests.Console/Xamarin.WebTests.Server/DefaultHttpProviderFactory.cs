//
// DefaultHttpProviderFactory.cs
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
using System.Collections.Generic;

namespace Xamarin.WebTests.Server
{
	using Providers;
	using Portable;

	class DefaultHttpProviderFactory : IHttpProviderFactory
	{
		static readonly DefaultHttpProvider defaultProvider = new DefaultHttpProvider ();
		readonly Dictionary<HttpProviderType,IHttpProvider> providers;

		internal DefaultHttpProviderFactory ()
		{
			providers = new Dictionary<HttpProviderType, IHttpProvider> ();
			providers.Add (HttpProviderType.Default, defaultProvider);
		}

		public IHttpProvider Default {
			get { return defaultProvider; }
		}

		public bool IsSupported (HttpProviderType type)
		{
			return providers.ContainsKey (type);
		}

		protected void Install (HttpProviderType type, IHttpProvider provider)
		{
			providers.Add (type, provider);
		}

		public IHttpProvider GetProvider (HttpProviderType type)
		{
			return providers [type];
		}

		public bool SupportsPerRequestCertificateValidator {
			get { return HttpWebRequestImpl.SupportsCertificateValidator; }
		}

		public void InstallCertificateValidator (IHttpWebRequest request, ICertificateValidator validator)
		{
			((HttpWebRequestImpl)request).InstallCertificateValidator (validator);
		}

		public void InstallDefaultCertificateValidator (ICertificateValidator validator)
		{
			ServicePointManager.ServerCertificateValidationCallback = ((CertificateValidator)validator).ValidationCallback;
		}
	}
}

