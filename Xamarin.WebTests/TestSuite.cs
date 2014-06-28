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

[assembly: AsyncTestSuite (typeof (Xamarin.WebTests.TestSuite))]

namespace Xamarin.WebTests
{
	using Runners;

	public class TestSuite : ITestSuite
	{
		public static readonly TestFeature NTLM = new TestFeature ("NTLM", "NTLM Authentication");
		public static readonly TestFeature SSL = new TestFeature ("SSL", "Use SSL", true);
		public static readonly TestFeature Proxy = new TestFeature ("Proxy", "Proxy Tests", true);

		public static readonly TestFeature HasNetwork = new TestFeature (
			"Network", "HasNetwork", () => !IPAddress.IsLoopback (TestRunner.GetAddress ()));

		#region ITestSuite implementation
		public IEnumerable<TestFeature> Features {
			get {
				yield return NTLM;
				yield return SSL;
				yield return Proxy;
				yield return HasNetwork;
			}
		}
		#endregion
	}
}

