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

[assembly: AsyncTestSuite (typeof (WebTestFeatures))]
[assembly: RequireDependency (typeof (IPortableWebSupport))]
[assembly: RequireDependency (typeof (IHttpClientProvider))]
[assembly: RequireDependency (typeof (IHttpWebRequestProvider))]

namespace Xamarin.WebTests
{
	using Framework;
	using Portable;
	using Resources;
	using Internal;
	using Tests;

	[AttributeUsage (AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
	public class WorkAttribute : TestCategoryAttribute
	{
		public override TestCategory Category {
			get { return WebTestFeatures.WorkCategory; }
		}
	}

	[AttributeUsage (AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
	public class RecentlyFixedAttribute : TestCategoryAttribute
	{
		public override TestCategory Category {
			get { return WebTestFeatures.RecentlyFixedCategory; }
		}
	}

	[AttributeUsage (AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
	public class HeavyAttribute : TestCategoryAttribute
	{
		public override TestCategory Category {
			get { return WebTestFeatures.HeavyCategory; }
		}
	}

	[AttributeUsage (AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
	public class ProxyAttribute : TestFeatureAttribute
	{
		public override TestFeature Feature {
			get { return WebTestFeatures.Proxy; }
		}
	}

	[AttributeUsage (AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
	public class NotWorking : TestCategoryAttribute
	{
		public override TestCategory Category {
			get { return WebTestFeatures.NotWorkingCategory; }
		}
	}

	[AttributeUsage (AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
	public class MartinAttribute : TestFeatureAttribute
	{
		public override TestFeature Feature {
			get { return WebTestFeatures.Martin; }
		}
	}

	[AttributeUsage (AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
	public class Mono38Attribute : TestFeatureAttribute
	{
		public override TestFeature Feature {
			get { return WebTestFeatures.Mono38; }
		}
	}

	[AttributeUsage (AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
	public class Mono381Attribute : TestFeatureAttribute
	{
		public override TestFeature Feature {
			get { return WebTestFeatures.Mono381; }
		}
	}

	[AttributeUsage (AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
	public class Mono61Attribute : TestFeatureAttribute
	{
		public override TestFeature Feature {
			get { return WebTestFeatures.Mono361; }
		}
	}

	[AttributeUsage (AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
	public class SSLAttribute : TestFeatureAttribute
	{
		public override TestFeature Feature {
			get { return WebTestFeatures.SSL; }
		}
	}

	[AttributeUsage (AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
	public class CertificateTestsAttribute : TestFeatureAttribute
	{
		public override TestFeature Feature {
			get { return WebTestFeatures.CertificateTests; }
		}
	}

	public class WebTestFeatures : ITestConfigurationProvider
	{
		public static readonly WebTestFeatures Instance;

		public static readonly TestFeature NTLM = new TestFeature ("NTLM", "NTLM Authentication");
		public static readonly TestFeature SSL = new TestFeature ("SSL", "Use SSL", true);
		public static readonly TestFeature Redirect = new TestFeature ("Redirect", "Redirect Tests", true);
		public static readonly TestFeature Proxy = new TestFeature ("Proxy", "Proxy Tests", true);
		public static readonly TestFeature ProxyAuth = new TestFeature ("ProxyAuth", "Proxy Authentication", true);
		public static readonly TestFeature Experimental = new TestFeature ("Experimental", "Experimental Tests", false);

		public static readonly TestFeature ReuseConnection = new TestFeature ("ReuseConnection", "Reuse Connection", false);

		// Requires special local setup
		public static readonly TestFeature Martin = new TestFeature ("Martin", "Martin's Lab", false);

		public static readonly TestFeature HasNetwork = new TestFeature (
			"Network", "HasNetwork", () => DependencyInjector.Get<IPortableWebSupport> ().HasNetwork);

		public static readonly TestFeature Mono38;
		public static readonly TestFeature Mono381;
		public static readonly TestFeature Mono361;

		public static readonly TestFeature CertificateTests;

		public static readonly TestCategory WorkCategory = new TestCategory ("Work") { IsExplicit = true };
		public static readonly TestCategory HeavyCategory = new TestCategory ("Heavy") { IsExplicit = true };
		public static readonly TestCategory RecentlyFixedCategory = new TestCategory ("RecentlyFixed") { IsExplicit = true };
		public static readonly TestCategory NotWorkingCategory = new TestCategory ("NotWorking") { IsExplicit = true };

		#region ITestConfigurationProvider implementation
		public string Name {
			get { return "Xamarin.WebTests"; }
		}

		public IEnumerable<TestFeature> Features {
			get {
				yield return NTLM;
				yield return SSL;
				yield return Redirect;
				yield return Proxy;
				yield return ProxyAuth;
				yield return Experimental;
				yield return ReuseConnection;
				yield return Martin;

				yield return HasNetwork;
				yield return Mono38;
				yield return Mono381;
				yield return Mono361;

				yield return CertificateTests;
			}
		}

		public IEnumerable<TestCategory> Categories {
			get {
				yield return WorkCategory;
				yield return HeavyCategory;
				yield return RecentlyFixedCategory;
				yield return NotWorkingCategory;
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
				if (ctx.IsEnabled (SSL))
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
				if (ctx.IsEnabled (ReuseConnection))
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
				if (!ctx.IsEnabled (WebTestFeatures.HasNetwork))
					yield break;

				if (!ctx.IsEnabled (WebTestFeatures.Proxy))
					yield break;

				if (ctx.CurrentCategory == WebTestFeatures.WorkCategory) {
					yield return ProxyKind.SSL;
					yield break;
				}

				yield return ProxyKind.Simple;

				if (ctx.IsEnabled (WebTestFeatures.ProxyAuth)) {
					yield return ProxyKind.BasicAuth;
					if (ctx.IsEnabled (WebTestFeatures.NTLM))
						yield return ProxyKind.NtlmAuth;
				}

				if (ctx.IsEnabled (WebTestFeatures.Mono361)) {
					yield return ProxyKind.Unauthenticated;

					if (ctx.IsEnabled (WebTestFeatures.SSL))
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
				if (!ctx.IsEnabled (SSL))
					yield break;

				yield return ServerCertificateType.Default;
				if (!ctx.IsEnabled (CertificateTests))
					yield break;

				yield return ServerCertificateType.SelfSigned;
			}
		}

		static WebTestFeatures ()
		{
			Mono38 = new TestFeature (
				"Mono38", "Mono 3.8.0", () => HasMonoVersion (new Version (3, 8, 0)));
			Mono381 = new TestFeature (
				"Mono381", "Mono 3.8.1", () => HasMonoVersion (new Version (3, 8, 1)));
			Mono361 = new TestFeature (
				"Mono361", "Mono 3.6.1", () => HasMonoVersion (new Version (3, 6, 1)));
			CertificateTests = new TestFeature (
				"CertificateTests", "Whether the SSL Certificate tests are supported", () => SupportsCertificateTests ());

			Instance = new WebTestFeatures ();

			DependencyInjector.RegisterDependency<NTLMHandler> (() => new NTLMHandlerImpl ());
		}

		static bool SupportsCertificateTests ()
		{
			var provider = DependencyInjector.Get<IHttpWebRequestProvider> ();
			return provider.SupportsCertificateValidator;
		}

		static bool HasMonoVersion (Version version)
		{
			var support = DependencyInjector.Get<IPortableSupport> ();
			if (support.IsMicrosoftRuntime)
				return true;
			return support.MonoRuntimeVersion != null && support.MonoRuntimeVersion >= version;
		}
	}
}

