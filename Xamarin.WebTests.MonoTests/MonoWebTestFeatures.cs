﻿//
// MonoTestFeatures.cs
//
// Author:
//       Martin Baulig <martin.baulig@xamarin.com>
//
// Copyright (c) 2015 Xamarin Inc. (http://www.xamarin.com)
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
using Xamarin.AsyncTests.Constraints;
using Xamarin.WebTests;
using Xamarin.WebTests.TestFramework;
using Xamarin.WebTests.HttpHandlers;
using Xamarin.WebTests.MonoTests;
using Xamarin.WebTests.MonoConnectionFramework;
using Xamarin.WebTests.MonoTestFeatures;
using Mono.Security.Interface;

[assembly: AsyncTestSuite (typeof (MonoWebTestFeatures), "MonoTests", typeof (SharedWebTestFeatures))]
[assembly: DependencyProvider (typeof (MonoWebTestFeatures.Provider))]

namespace Xamarin.WebTests.MonoTests
{
	public class MonoWebTestFeatures : IAsyncTestAssembly, ISingletonInstance
	{
		public static MonoWebTestFeatures Instance {
			get { return DependencyInjector.Get<MonoWebTestFeatures> (); }
		}

		internal class Provider : IDependencyProvider
		{
			public void Initialize ()
			{
				DependencyInjector.RegisterDependency<MonoWebTestFeatures> (() => new MonoWebTestFeatures ());
			}
		}

		MonoWebTestFeatures ()
		{
		}

		public IEnumerable<TestFeature> Features {
			get {
				yield return MobileAttribute.Instance;
				yield return IOSAttribute.Instance;
				yield return SecurityFrameworkAttribute.Instance;
				yield return AppleTlsAttribute.Instance;
			}
		}

		public IEnumerable<TestCategory> Categories {
			get { return new TestCategory [0]; }
		}

		public string Name {
			get { return "MonoTestFeatures"; }
		}

		public void GlobalSetUp (TestContext ctx)
		{
			var setup = DependencyInjector.Get<IMonoConnectionFrameworkSetup> ();
			var provider = MonoTlsProviderFactory.GetProvider ();

			ctx.Assert (provider.ID, Is.EqualTo (setup.TlsProviderId), "Default TLS Provider");
		}

		public void GlobalTearDown (TestContext ctx)
		{
			Response.CheckLeakingInstances (ctx);
		}
	}
}

