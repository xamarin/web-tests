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

	public class WebTestFeatures : SharedWebTestFeatures
	{
		public static WebTestFeatures Instance {
			get { return DependencyInjector.Get<WebTestFeatures> (); }
		}

		public readonly TestFeature NTLM = new TestFeature ("NTLM", "NTLM Authentication");
		public readonly TestFeature Redirect = new TestFeature ("Redirect", "Redirect Tests", true);
		public readonly TestFeature Proxy = new TestFeature ("Proxy", "Proxy Tests", true);
		public readonly TestFeature ProxyAuth = new TestFeature ("ProxyAuth", "Proxy Authentication", true);

		public readonly TestFeature ReuseConnection = new TestFeature ("ReuseConnection", "Reuse Connection", false);

		public readonly TestCategory HeavyCategory = new TestCategory ("Heavy") { IsExplicit = true };
		public readonly TestCategory RecentlyFixedCategory = new TestCategory ("RecentlyFixed") { IsExplicit = true };

		readonly TestFeature sslFeature = new TestFeature ("SSL", "Use SSL", true);

		public override TestFeature SSL {
			get { return sslFeature; }
		}

		#region ITestConfigurationProvider implementation
		public override string Name {
			get { return "Xamarin.WebTests"; }
		}

		public override IEnumerable<TestFeature> Features {
			get {
				foreach (var features in base.Features)
					yield return features;

				yield return NTLM;
				yield return Redirect;
				yield return Proxy;
				yield return ProxyAuth;
				yield return ReuseConnection;
			}
		}

		public override IEnumerable<TestCategory> Categories {
			get {
				foreach (var category in base.Categories)
					yield return category;

				yield return HeavyCategory;
				yield return RecentlyFixedCategory;
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

				if (ctx.CurrentCategory == WorkAttribute.Instance) {
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

		public WebTestFeatures ()
		{
			DependencyInjector.RegisterDependency<NTLMHandler> (() => new NTLMHandlerImpl ());
		}
	}
}

