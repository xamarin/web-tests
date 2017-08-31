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
using System.Net.Security;
using System.Threading;
using System.Threading.Tasks;
using System.Security.Cryptography.X509Certificates;
using Mono.Security.Interface;
#if !__MOBILE__ && !__IOS__
using System.Reflection;
#endif
using Xamarin.AsyncTests;

namespace Xamarin.WebTests.MonoTestProvider
{
	using MonoConnectionFramework;
	using ConnectionFramework;
	using System.Net.Security;

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

		public MonoTlsProvider SecondTlsProvider {
			get;
		}

		public bool InstallDefaultCertificateValidator {
			get { return true; }
		}

		public bool SupportsTls12 {
			get;
		}

		public bool SupportsCleanShutdown {
			get;
		}

		public bool UsingBtls {
			get;
		}

		public bool UsingAppleTls {
			get;
		}

		public bool HasNewWebStack {
			get;
		}

		public bool SupportsRenegotiation {
			get;
		}

		public MonoConnectionFrameworkSetup (string name)
		{
			Name = name;

#if !__MOBILE__ && !__UNIFIED__ && !__IOS__
			var providerEnvVar = Environment.GetEnvironmentVariable ("MONO_TLS_PROVIDER");
			switch (providerEnvVar) {
			case "btls":
				MonoTlsProviderFactory.Initialize ("btls");
				break;
			case "apple":
				MonoTlsProviderFactory.Initialize ("apple");
				break;
			case "default":
			case null:
#if APPLETLS
				MonoTlsProviderFactory.Initialize ("apple");
#elif LEGACY
				MonoTlsProviderFactory.Initialize ("legacy");
#else
				MonoTlsProviderFactory.Initialize ("btls");
#endif
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
			SupportsTls12 = UsingBtls || UsingAppleTls;

			SupportsCleanShutdown = CheckCleanShutdown ();
			HasNewWebStack = CheckNewWebStack ();
			SupportsRenegotiation = CheckRenegotiation ();

			if (CheckAppleTls ()) {
#if !XAMMAC
				if (UsingBtls)
					SecondTlsProvider = MonoTlsProviderFactory.GetProvider ("apple");
				if (UsingAppleTls)
					SecondTlsProvider = MonoTlsProviderFactory.GetProvider ("btls");
#endif
			} else {
				if (UsingAppleTls)
					throw new NotSupportedException ("AppleTls is not supported in this version of the Mono runtime.");
			}

			InitReflection ();
		}

		public void Initialize (ConnectionProviderFactory factory)
		{
			MonoConnectionProviderFactory.RegisterProvider (factory, TlsProvider, false);

			if (SecondTlsProvider != null)
				MonoConnectionProviderFactory.RegisterProvider (factory, SecondTlsProvider, true);
		}

		public MonoTlsProvider GetDefaultProvider ()
		{
			return TlsProvider;
		}

		void InitReflection ()
		{
#if !__IOS__ && !__MOBILE__
			var type = typeof (MonoTlsProviderFactory);
			getSslStreamFromHttpListenerContext = type.GetRuntimeMethod ("GetMonoSslStream", new Type[] { typeof (HttpListenerContext) });
			clientCertIssuersProp = typeof (MonoTlsSettings).GetTypeInfo ().GetDeclaredProperty ("ClientCertificateIssuers");
#endif
		}

		bool CheckCleanShutdown ()
		{
#if __IOS__ || __MOBILE__
			return false;
#else
			var type = typeof (MonoTlsProvider);
			supportsCleanShutdown = type.GetProperty ("SupportsCleanShutdown", BindingFlags.Instance | BindingFlags.NonPublic);
			if (supportsCleanShutdown == null)
				return false;
			getSupportsCleanShutdown = supportsCleanShutdown.GetMethod;
			var settings = typeof (MonoTlsSettings);
			sendCloseNotify = settings.GetProperty ("SendCloseNotify", BindingFlags.Instance | BindingFlags.NonPublic);
			if (sendCloseNotify == null)
				return false;
			setSendCloseNotify = sendCloseNotify.SetMethod;
			return true;
#endif
		}

		bool CheckNewWebStack ()
		{
#if __IOS__ || __MOBILE__
			return false;
#else
			var asm = typeof (HttpWebRequest).Assembly;
			var type = asm.GetType ("System.Net.WebOperation");
			return type != null;
#endif
		}

		bool CheckAppleTls ()
		{
#if __IOS__
			return true;
#elif !__MOBILE__
			/*
			 * AppleTls is unusable broken on Jenkins because it would hang the bots
			 * prior to mono/master commit 1eb27c1df8ed29d84ca935496ffd6c230447895a.
			 * 
			 * This commit also added a new internal class called Mono.AppleTls.SecAccess;
			 * we use reflection to check for its existance to determine whether our runtime
			 * is recent enough.
			 * 
			 */
			var assembly = typeof (HttpWebRequest).Assembly;
			return assembly.GetType ("Mono.AppleTls.SecAccess", false) != null;
#else
			return false;
#endif
		}

#if !__IOS__ && !__MOBILE__
		PropertyInfo canRenegotiate;
		MethodInfo getCanRenegotiate;
		MethodInfo renegotiateAsync;
		PropertyInfo supportsCleanShutdown;
		MethodInfo getSupportsCleanShutdown;
		PropertyInfo sendCloseNotify;
		MethodInfo setSendCloseNotify;
		MethodInfo getSslStreamFromHttpListenerContext;
		PropertyInfo clientCertIssuersProp;
#endif

		bool CheckRenegotiation ()
		{
#if __IOS__ || __MOBILE__
			return false;
#else
			var type = typeof (IMonoSslStream);
			canRenegotiate = type.GetProperty ("CanRenegotiate");
			if (canRenegotiate == null)
				return false;
			getCanRenegotiate = canRenegotiate.GetGetMethod ();
			renegotiateAsync = type.GetMethod ("RenegotiateAsync");
			return renegotiateAsync != null;
#endif
		}

		public bool CanRenegotiate (IMonoSslStream stream)
		{
#if __IOS__ || __MOBILE__
			throw new NotSupportedException ();
#else
			return (bool)getCanRenegotiate.Invoke (stream, null);
#endif
		}

		public Task RenegotiateAsync (IMonoSslStream stream, CancellationToken cancellationToken)
		{
#if __IOS__ || __MOBILE__
			throw new NotSupportedException ();
#else
			return (Task)renegotiateAsync.Invoke (stream, new object[] { cancellationToken });
#endif
		}

		public bool ProviderSupportsCleanShutdown (MonoTlsProvider provider)
		{
#if __IOS__ || __MOBILE__
			return false;
#else
			if (getSupportsCleanShutdown == null)
				return false;
			return (bool)getSupportsCleanShutdown.Invoke (provider, null);
#endif
		}

		public void SendCloseNotify (MonoTlsSettings settings, bool value)
		{
#if __IOS__ || __MOBILE__
			throw new NotSupportedException ();
#else
			setSendCloseNotify.Invoke (settings, new object[] { value });
#endif
		}

		public Task ShutdownAsync (SslStream stream)
		{
#if __IOS__ || __MOBILE__
			throw new NotSupportedException ();
#else
			return stream.ShutdownAsync ();
#endif
		}

		public bool SupportsHttpListenerContext {
			get {
#if __IOS__ || __MOBILE__
				return false;
#else
				return getSslStreamFromHttpListenerContext != null;
#endif
			}
		}

		public SslStream GetSslStream (IMonoSslStream stream)
		{
			return (SslStream)stream;
		}

		public SslStream GetSslStream (HttpListenerContext context)
		{
#if __IOS__ || __MOBILE__
			throw new NotSupportedException ();
#else
			if (getSslStreamFromHttpListenerContext == null)
				throw new NotSupportedException ();
			var sslStream = (IMonoSslStream)getSslStreamFromHttpListenerContext.Invoke (null, new object[] { context });
			return (SslStream)sslStream;
#endif
		}

		public bool SupportsClientCertificateIssuers {
			get {
#if __IOS__ || __MOBILE__
				return false;
#else
				return clientCertIssuersProp != null;
#endif
			}
		}

		public void SetClientCertificateIssuers (MonoTlsSettings settings, string[] issuers)
		{
#if __IOS__ || __MOBILE__
			throw new NotSupportedException ();
#else
			if (clientCertIssuersProp == null)
				throw new NotSupportedException ();
			clientCertIssuersProp.SetValue (settings, issuers);
#endif
		}

	}
}
