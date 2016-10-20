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

namespace Xamarin.WebTests.MonoTestProvider
{
	using MonoConnectionFramework;
	using ConnectionFramework;

	public abstract class MonoConnectionFrameworkSetup : IMonoConnectionFrameworkSetup
	{
		public abstract string Name {
			get;
		}

		public abstract string TlsProviderName {
			get;
		}

		public abstract Guid TlsProvider {
			get;
		}

		public bool InstallDefaultCertificateValidator {
			get { return true; }
		}

		public abstract bool SupportsTls12 {
			get;
		}

		public bool UsingBtls {
			get;
			private set;
		}

		public abstract bool UsingAppleTls {
			get;
		}

		public void Initialize (ConnectionProviderFactory factory)
		{
#if !__MOBILE__ && !__UNIFIED__
			var type = typeof (MonoTlsProviderFactory);
			var initialize = type.GetMethod ("Initialize", new Type [] { typeof (string) });
#if BTLS
			if (initialize == null)
				throw new NotSupportedException ("Your Mono runtime is too old to support BTLS!");
			initialize.Invoke (null, new object [] { "btls" });
			UsingBtls = true;
#else
			var providerEnvVar = Environment.GetEnvironmentVariable ("MONO_TLS_PROVIDER");
			if (string.Equals (providerEnvVar, "btls", StringComparison.OrdinalIgnoreCase)) {
				if (initialize == null)
					throw new NotSupportedException ("Your Mono runtime is too old to support BTLS!");
				initialize.Invoke (null, new object [] { "btls" });
				UsingBtls = true;
			} else {
				if (initialize != null)
					initialize.Invoke (null, new object [] { "legacy" });
			}
#endif
#endif
			var provider = MonoTlsProviderFactory.GetProvider ();
			MonoConnectionProviderFactory.RegisterProvider (factory, provider);
		}

		public MonoTlsProvider GetDefaultProvider ()
		{
			return MonoTlsProviderFactory.GetProvider ();
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
#if !__MOBILE__
			var type = typeof (CertificateValidationHelper);
			var getValidator = type.GetMethod ("GetValidator", new Type[] { typeof (MonoTlsSettings) });
			if (getValidator != null)
				return (ICertificateValidator)getValidator.Invoke (null, new object[] { settings });
			getValidator = type.GetMethod ("GetValidator", new Type[] { typeof (MonoTlsSettings), typeof (MonoTlsProvider) });
			return (ICertificateValidator) getValidator.Invoke (null, new object[] { settings, null });
#else
			return CertificateValidationHelper.GetValidator (settings);
#endif
		}

		public IMonoConnectionInfo GetConnectionInfo (IMonoSslStream stream)
		{
			var info = stream.GetConnectionInfo ();
			if (info == null)
				return null;
			return new MonoConnectionInfo (info);
		}

		class MonoConnectionInfo : IMonoConnectionInfo
		{
			MonoTlsConnectionInfo info;

			public MonoConnectionInfo (MonoTlsConnectionInfo info)
			{
				this.info = info;
			}

			public CipherSuiteCode CipherSuiteCode {
				get {
					return info.CipherSuiteCode;
				}
			}
		}
	}
}
