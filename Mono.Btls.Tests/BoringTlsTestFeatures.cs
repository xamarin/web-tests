//
// BoringTlsTestFeatures.cs
//
// Author:
//       Martin Baulig <martin.baulig@xamarin.com>
//
// Copyright (c) 2016 Xamarin Inc. (http://www.xamarin.com)
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
using System.Collections.Generic;
using Xamarin.AsyncTests;
using Xamarin.WebTests.TestFramework;
using Mono.Btls.Tests;

[assembly: AsyncTestSuite (typeof (BoringTlsTestFeatures), "BoringSslTests", typeof (SharedWebTestFeatures))]
[assembly: DependencyProvider (typeof (BoringTlsTestFeatures.Provider))]

namespace Mono.Btls.Tests
{
	public class BoringTlsTestFeatures : ITestConfigurationProvider, ISingletonInstance
	{
		public static BoringTlsTestFeatures Instance {
			get { return DependencyInjector.Get<BoringTlsTestFeatures> (); }
		}

		internal class Provider : IDependencyProvider
		{
			public void Initialize ()
			{
				DependencyInjector.RegisterDependency<BoringTlsTestFeatures> (() => new BoringTlsTestFeatures ());
			}
		}

		BoringTlsTestFeatures ()
		{
		}

		public IEnumerable<TestFeature> Features {
			get { return new TestFeature[0]; }
		}

		public IEnumerable<TestCategory> Categories {
			get { return new TestCategory[0]; }
		}

		public string Name {
			get { return "BoringSslTests"; }
		}
	}
}
