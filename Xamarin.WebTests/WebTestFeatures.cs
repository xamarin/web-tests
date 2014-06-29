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
using System.Collections.Generic;
using Xamarin.AsyncTests;

[assembly: AsyncTestSuite (typeof (Xamarin.WebTests.WebTestFeatures))]

namespace Xamarin.WebTests
{
	using Runners;

	[AttributeUsage (AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
	public class WorkAttribute : TestCategoryAttribute
	{
		public override TestCategory Category {
			get { return WebTestFeatures.WorkCategory; }
		}
	}

	[AttributeUsage (AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
	public class ProxyAttribute : TestFeatureAttribute
	{
		public override TestFeature Feature {
			get { return WebTestFeatures.Proxy; }
		}
	}

	public class WebTestFeatures : ITestConfiguration
	{
		public static readonly TestFeature NTLM = new TestFeature ("NTLM", "NTLM Authentication");
		public static readonly TestFeature SSL = new TestFeature ("SSL", "Use SSL", true);
		public static readonly TestFeature Proxy = new TestFeature ("Proxy", "Proxy Tests", true);
		public static readonly TestFeature ProxyAuth = new TestFeature ("ProxyAuth", "Proxy Authentication", true);
		public static readonly TestFeature Experimental = new TestFeature ("Experimental", "Experimental Tests", false);

		public static readonly TestFeature HasNetwork = new TestFeature (
			"Network", "HasNetwork", () => !IPAddress.IsLoopback (TestRunner.GetAddress ()));

		public static readonly TestCategory NotWorking = new TestCategory ("NotWorking");
		public static readonly TestCategory WorkCategory = new TestCategory ("Work");

		#region ITestSuite implementation
		public IEnumerable<TestFeature> Features {
			get {
				yield return NTLM;
				yield return SSL;
				yield return Proxy;
				yield return ProxyAuth;
				yield return Experimental;

				yield return HasNetwork;
			}
		}

		public TestCategory DefaultCategory {
			get { return TestCategory.All; }
		}

		public IEnumerable<TestCategory> Categories {
			get {
				yield return TestCategory.All;
				yield return NotWorking;
				yield return WorkCategory;
			}
		}
		#endregion
	}
}

