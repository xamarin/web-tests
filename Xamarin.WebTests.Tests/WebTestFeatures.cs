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
using System.Threading;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using Xamarin.AsyncTests;
using Xamarin.AsyncTests.Portable;
using Xamarin.WebTests;
using Xamarin.WebTests.HttpClient;
using Xamarin.WebTests.ConnectionFramework;
using Xamarin.WebTests.TestFramework;

[assembly: AsyncTestSuite (typeof (WebTestFeatures), "Tests", typeof (SharedWebTestFeatures))]
[assembly: RequireDependency (typeof (IHttpClientProvider))]
[assembly: RequireDependency (typeof (ConnectionProviderFactory))]
[assembly: DependencyProvider (typeof (WebTestFeatures.Provider))]

namespace Xamarin.WebTests
{
	using HttpFramework;
	using TestFramework;
	using Resources;
	using Server;
	using Tests;

	public class WebTestFeatures : ITestConfigurationProvider, ISingletonInstance
	{
		public static WebTestFeatures Instance {
			get { return DependencyInjector.Get<WebTestFeatures> (); }
		}

		public readonly TestFeature NTLM = new TestFeature ("NTLM", "NTLM Authentication", true);
		public readonly TestFeature Redirect = new TestFeature ("Redirect", "Redirect Tests", true);
		public readonly TestFeature Proxy = new TestFeature ("Proxy", "Proxy Tests", true);
		public readonly TestFeature ProxyAuth = new TestFeature ("ProxyAuth", "Proxy Authentication", true);

		public readonly TestFeature ReuseConnection = new TestFeature ("ReuseConnection", "Reuse Connection", true);

		public readonly TestCategory HeavyCategory = new TestCategory ("Heavy") { IsExplicit = true };
		public readonly TestCategory RecentlyFixedCategory = new TestCategory ("RecentlyFixed") { IsExplicit = true };

		#region ITestConfigurationProvider implementation
		public string Name {
			get { return "Xamarin.WebTests.Tests"; }
		}

		public IEnumerable<TestFeature> Features {
			get {
				yield return NTLM;
				yield return Redirect;
				yield return Proxy;
				yield return ProxyAuth;
				yield return ReuseConnection;
				yield return NetworkAttribute.Instance;
				yield return Tls12Attribute.Instance;
			}
		}

		public IEnumerable<TestCategory> Categories {
			get {
				yield return HeavyCategory;
				yield return RecentlyFixedCategory;
			}
		}
		#endregion

		[AttributeUsage (AttributeTargets.Parameter | AttributeTargets.Property, AllowMultiple = false)]
		public class SelectSSLAttribute : TestParameterAttribute, ITestParameterSource<bool>
		{
			public SelectSSLAttribute (string filter = null, TestFlags flags = TestFlags.None)
				: base (filter, flags)
			{
			}

			#region ITestParameterSource implementation
			public IEnumerable<bool> GetParameters (TestContext ctx, string filter)
			{
				yield return false;
				yield return true;
			}
			#endregion
		}

		[AttributeUsage (AttributeTargets.Parameter | AttributeTargets.Property, AllowMultiple = false)]
		public class SelectReuseConnectionAttribute : TestParameterAttribute, ITestParameterSource<bool>
		{
			public SelectReuseConnectionAttribute (string filter = null, TestFlags flags = TestFlags.None)
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

		[AttributeUsage (AttributeTargets.Method, AllowMultiple = false)]
		public class UseProxyKindAttribute : FixedTestParameterAttribute
		{
			public override Type Type {
				get { return typeof(ProxyKind); }
			}

			public override object Value {
				get { return kind; }
			}

			public override string Identifier {
				get { return identifier; }
			}

			public ProxyKind Kind {
				get { return kind; }
			}

			readonly string identifier;
			readonly ProxyKind kind;

			public UseProxyKindAttribute (ProxyKind kind)
			{
				this.kind = kind;
				this.identifier = Type.Name;
			}
		}

		[AttributeUsage (AttributeTargets.Parameter | AttributeTargets.Property, AllowMultiple = false)]
		public class SelectProxyKindAttribute : TestParameterAttribute, ITestParameterSource<ProxyKind>
		{
			public SelectProxyKindAttribute (string filter = null, TestFlags flags = TestFlags.Browsable)
				: base (filter, flags)
			{
			}

			public bool IncludeSSL {
				get; set;
			}

			public IEnumerable<ProxyKind> GetParameters (TestContext ctx, string filter)
			{
				if (!ctx.IsEnabled (Instance.Proxy))
					yield break;

				yield return ProxyKind.Simple;

				if (ctx.IsEnabled (Instance.ProxyAuth)) {
					yield return ProxyKind.BasicAuth;
					if (ctx.IsEnabled (Instance.NTLM))
						yield return ProxyKind.NtlmAuth;
				}

				yield return ProxyKind.Unauthenticated;
				yield return ProxyKind.SSL;
			}
		}

		[AttributeUsage (AttributeTargets.Parameter | AttributeTargets.Property, AllowMultiple = false)]
		public class SelectServerCertificateAttribute : TestParameterAttribute, ITestParameterSource<CertificateResourceType>
		{
			public SelectServerCertificateAttribute (string filter = null, TestFlags flags = TestFlags.Browsable)
				: base (filter, flags)
			{
			}

			public IEnumerable<CertificateResourceType> GetParameters (TestContext ctx, string filter)
			{
				yield return CertificateResourceType.SelfSignedServerCertificate;
				yield return CertificateResourceType.ServerCertificateFromLocalCA;
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

		internal class Provider : IDependencyProvider
		{
			public void Initialize ()
			{
				DependencyInjector.RegisterDependency<WebTestFeatures> (() => new WebTestFeatures ());
			}
		}
	}
}

