//
// MonoConnectionFrameworkSetup.cs
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
using System.Net;
using System.Security.Cryptography.X509Certificates;
using Mono.Security.Interface;
#if !__MOBILE__
using System.Reflection;
#endif
using Xamarin.AsyncTests;

namespace Xamarin.WebTests.MonoTestProvider
{
	using MonoConnectionFramework;
	using ConnectionFramework;

	public sealed class MonoConnectionFrameworkSetup : IMonoConnectionFrameworkSetup
	{
		public string Name {
			get;
		}

		public string TlsProviderName {
			get { return TlsProvider.Name; }
		}

		public Guid TlsProviderId {
			get { return TlsProvider.ID; }
		}

		public MonoTlsProvider TlsProvider {
			get;
		}

		public bool InstallDefaultCertificateValidator {
			get { return true; }
		}

		public bool SupportsTls12 {
			get;
		}

		public bool UsingBtls {
			get;
		}

		public bool UsingAppleTls {
			get;
		}

		public MonoConnectionFrameworkSetup (string name)
		{
			Name = name;

#if !__MOBILE__ && !__UNIFIED__
			var providerEnvVar = Environment.GetEnvironmentVariable ("MONO_TLS_PROVIDER");
			switch (providerEnvVar) {
			case "btls":
			case "default":
			case null:
				MonoTlsProviderFactory.Initialize ("btls");
				break;
			case "legacy":
				MonoTlsProviderFactory.Initialize ("legacy");
				break;
			default:
				throw new NotSupportedException (string.Format ("Unsupported TLS Provider: `{0}'", providerEnvVar));
			}
#endif
			TlsProvider = MonoTlsProviderFactory.GetProvider ();
			UsingBtls = TlsProvider.ID == ConnectionProviderFactory.BoringTlsGuid;
			UsingAppleTls = TlsProvider.ID == ConnectionProviderFactory.AppleTlsGuid;
			SupportsTls12 = UsingBtls;
		}

		public void Initialize (ConnectionProviderFactory factory)
		{
			MonoConnectionProviderFactory.RegisterProvider (factory, TlsProvider);
		}

		public MonoTlsProvider GetDefaultProvider ()
		{
			return TlsProvider;
		}

		public HttpWebRequest CreateHttpsRequest (Uri requestUri, MonoTlsProvider provider, MonoTlsSettings settings)
		{
			return MonoTlsProviderFactory.CreateHttpsRequest (requestUri, provider, settings);
		}

		public HttpListener CreateHttpListener (X509Certificate certificate, MonoTlsProvider provider, MonoTlsSettings settings)
		{
			return MonoTlsProviderFactory.CreateHttpListener (certificate, provider, settings);
		}

		public ICertificateValidator GetCertificateValidator (MonoTlsSettings settings)
		{
			return CertificateValidationHelper.GetValidator (settings);
		}
	}
}
