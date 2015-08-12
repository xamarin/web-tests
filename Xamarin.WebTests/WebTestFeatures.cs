//
// TestSuite.cs
//
// Author:
//       Martin Baulig <martin.baulig@xamarin.com>
//
// Copyright (c) 2014 Xamarin Inc. (http://www.xamarin.com)
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
using System.Text.RegularExpressions;
using System.Collections.Generic;
using Xamarin.AsyncTests;
using Xamarin.AsyncTests.Portable;
using Xamarin.WebTests;
using Xamarin.WebTests.Portable;
using Xamarin.WebTests.HttpClient;
using Xamarin.WebTests.Providers;

[assembly: AsyncTestSuite (typeof (WebTestFeatures))]
[assembly: RequireDependency (typeof (IPortableWebSupport))]
[assembly: RequireDependency (typeof (IHttpClientProvider))]
[assembly: RequireDependency (typeof (ConnectionProviderFactory))]

namespace Xamarin.WebTests
{
	using HttpFramework;
	using Portable;
	using Resources;
	using Internal;
	using Features;
	using Providers;
	using Tests;

	[AttributeUsage (AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
	public class WorkAttribute : TestCategoryAttribute
	{
		public override TestCategory Category {
			get { return WebTestFeatures.Instance.WorkCategory; }
		}
	}

	[AttributeUsage (AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
	public class RecentlyFixedAttribute : TestCategoryAttribute
	{
		public override TestCategory Category {
			get { return WebTestFeatures.Instance.RecentlyFixedCategory; }
		}
	}

	[AttributeUsage (AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
	public class HeavyAttribute : TestCategoryAttribute
	{
		public override TestCategory Category {
			get { return WebTestFeatures.Instance.HeavyCategory; }
		}
	}

	[AttributeUsage (AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
	public class ProxyAttribute : TestFeatureAttribute
	{
		public override TestFeature Feature {
			get { return WebTestFeatures.Instance.Proxy; }
		}
	}

	[AttributeUsage (AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
	public class MartinAttribute : TestCategoryAttribute
	{
		public override TestCategory Category {
			get { return WebTestFeatures.Instance.MartinCategory; }
		}
	}

	[AttributeUsage (AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
	public class Mono38Attribute : TestFeatureAttribute
	{
		public override TestFeature Feature {
			get { return WebTestFeatures.Instance.Mono38; }
		}
	}

	[AttributeUsage (AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
	public class Mono381Attribute : TestFeatureAttribute
	{
		public override TestFeature Feature {
			get { return WebTestFeatures.Instance.Mono381; }
		}
	}

	[AttributeUsage (AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
	public class Mono61Attribute : TestFeatureAttribute
	{
		public override TestFeature Feature {
			get { return WebTestFeatures.Instance.Mono361; }
		}
	}

	[AttributeUsage (AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
	public class SSLAttribute : TestFeatureAttribute
	{
		public override TestFeature Feature {
			get { return WebTestFeatures.Instance.SSL; }
		}
	}

	[AttributeUsage (AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
	public class CertificateTestsAttribute : TestFeatureAttribute
	{
		public override TestFeature Feature {
			get { return WebTestFeatures.Instance.CertificateTests; }
		}
	}

	public class WebTestFeatures : ITestConfigurationProvider
	{
		public static WebTestFeatures Instance {
			get { return DependencyInjector.Get<WebTestFeatures> (); }
		}

		public readonly TestFeature NTLM = new TestFeature ("NTLM", "NTLM Authentication");
		public readonly TestFeature SSL = new TestFeature ("SSL", "Use SSL", true);
		public readonly TestFeature Redirect = new TestFeature ("Redirect", "Redirect Tests", true);
		public readonly TestFeature Proxy = new TestFeature ("Proxy", "Proxy Tests", true);
		public readonly TestFeature ProxyAuth = new TestFeature ("ProxyAuth", "Proxy Authentication", true);
		public readonly TestFeature Experimental = new TestFeature ("Experimental", "Experimental Tests", false);

		public readonly TestFeature ReuseConnection = new TestFeature ("ReuseConnection", "Reuse Connection", false);

		// Requires special local setup
		public readonly TestCategory MartinCategory = new TestCategory ("Martin") { IsExplicit = true };

		public readonly TestFeature HasNetwork = new TestFeature (
			"Network", "HasNetwork", () => DependencyInjector.Get<IPortableWebSupport> ().HasNetwork);

		public readonly TestFeature Mono38;
		public readonly TestFeature Mono381;
		public readonly TestFeature Mono361;

		public readonly TestFeature CertificateTests;

		public readonly TestCategory WorkCategory = new TestCategory ("Work") { IsExplicit = true };
		public readonly TestCategory HeavyCategory = new TestCategory ("Heavy") { IsExplicit = true };
		public readonly TestCategory RecentlyFixedCategory = new TestCategory ("RecentlyFixed") { IsExplicit = true };

		#region ITestConfigurationProvider implementation
		public virtual string Name {
			get { return "Xamarin.WebTests"; }
		}

		public virtual IEnumerable<TestFeature> Features {
			get {
				yield return NTLM;
				yield return SSL;
				yield return Redirect;
				yield return Proxy;
				yield return ProxyAuth;
				yield return Experimental;
				yield return ReuseConnection;

				yield return HasNetwork;
				yield return Mono38;
				yield return Mono381;
				yield return Mono361;

				yield return IncludeNotWorkingAttribute.Instance;
				yield return MonoWithNewTlsAttribute.Instance;

				yield return CertificateTests;
				yield return PuppyAttribute.Instance;
			}
		}

		public virtual IEnumerable<TestCategory> Categories {
			get {
				yield return WorkCategory;
				yield return MartinCategory;
				yield return HeavyCategory;
				yield return RecentlyFixedCategory;

				yield return NotWorkingAttribute.Instance;
				yield return ManualClientAttribute.Instance;
				yield return ManualServerAttribute.Instance;
			}
		}
		#endregion

		[AttributeUsage (AttributeTargets.Parameter | AttributeTargets.Property, AllowMultiple = false)]
		public class SelectSSLAttribute : TestParameterAttribute, ITestParameterSource<bool>
		{
			public SelectSSLAttribute (string filter = null, TestFlags flags = TestFlags.Hidden)
				: base (filter, flags)
			{
			}

			#region ITestParameterSource implementation
			public IEnumerable<bool> GetParameters (TestContext ctx, string filter)
			{
				yield return false;
				if (ctx.IsEnabled (Instance.SSL))
					yield return true;
			}
			#endregion
		}

		[AttributeUsage (AttributeTargets.Parameter | AttributeTargets.Property, AllowMultiple = false)]
		public class SelectReuseConnectionAttribute : TestParameterAttribute, ITestParameterSource<bool>
		{
			public SelectReuseConnectionAttribute (string filter = null, TestFlags flags = TestFlags.Hidden)
				: base (filter, flags)
			{
			}

			#region ITestParameterSource implementation
			public IEnumerable<bool> GetParameters (TestContext ctx, string filter)
			{
				yield return false;
				if (ctx.IsEnabled (Instance.ReuseConnection))
					yield return true;
			}
			#endregion
		}

		[AttributeUsage (AttributeTargets.Parameter | AttributeTargets.Property, AllowMultiple = false)]
		public class SelectProxyKindAttribute : TestParameterAttribute, ITestParameterSource<ProxyKind>
		{
			public SelectProxyKindAttribute (string filter = null, TestFlags flags = TestFlags.Browsable)
				: base (filter, flags)
			{
			}

			public IEnumerable<ProxyKind> GetParameters (TestContext ctx, string filter)
			{
				if (!ctx.IsEnabled (Instance.HasNetwork))
					yield break;

				if (!ctx.IsEnabled (Instance.Proxy))
					yield break;

				if (ctx.CurrentCategory == Instance.WorkCategory) {
					yield return ProxyKind.SSL;
					yield break;
				}

				yield return ProxyKind.Simple;

				if (ctx.IsEnabled (Instance.ProxyAuth)) {
					yield return ProxyKind.BasicAuth;
					if (ctx.IsEnabled (Instance.NTLM))
						yield return ProxyKind.NtlmAuth;
				}

				if (ctx.IsEnabled (Instance.Mono361)) {
					yield return ProxyKind.Unauthenticated;

					if (ctx.IsEnabled (Instance.SSL))
						yield return ProxyKind.SSL;
				}
			}
		}

		[AttributeUsage (AttributeTargets.Parameter | AttributeTargets.Property, AllowMultiple = false)]
		public class SelectServerCertificateAttribute : TestParameterAttribute, ITestParameterSource<ServerCertificateType>
		{
			public SelectServerCertificateAttribute (string filter = null, TestFlags flags = TestFlags.Browsable)
				: base (filter, flags)
			{
			}

			public IEnumerable<ServerCertificateType> GetParameters (TestContext ctx, string filter)
			{
				if (!ctx.IsEnabled (Instance.SSL))
					yield break;

				yield return ServerCertificateType.SelfSigned;
				if (!ctx.IsEnabled (Instance.CertificateTests))
					yield break;

				yield return ServerCertificateType.LocalCA;
			}
		}

		public WebTestFeatures ()
		{
			Mono38 = new TestFeature (
				"Mono38", "Mono 3.8.0", () => HasMonoVersion (new Version (3, 8, 0)));
			Mono381 = new TestFeature (
				"Mono381", "Mono 3.8.1", () => HasMonoVersion (new Version (3, 8, 1)));
			Mono361 = new TestFeature (
				"Mono361", "Mono 3.6.1", () => HasMonoVersion (new Version (3, 6, 1)));
			CertificateTests = new TestFeature (
				"CertificateTests", "Whether the SSL Certificate tests are supported", () => SupportsCertificateTests ());

			DependencyInjector.RegisterDependency<NTLMHandler> (() => new NTLMHandlerImpl ());
		}

		bool SupportsCertificateTests ()
		{
			var support = DependencyInjector.Get<IPortableWebSupport> ();
			return support.SupportsPerRequestCertificateValidator;
		}

		bool HasMonoVersion (Version version)
		{
			var support = DependencyInjector.Get<IPortableSupport> ();
			if (support.IsMicrosoftRuntime)
				return true;
			return support.MonoRuntimeVersion != null && support.MonoRuntimeVersion >= version;
		}
	}
}

