﻿//
// SharedWebTestFeatures.cs
//
// Author:
//       Martin Baulig <martin.baulig@xamarin.com>
//
// Copyright (c) 2015 Xamarin, Inc.
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
using Xamarin.AsyncTests.Framework;
using Xamarin.AsyncTests.Portable;
using Xamarin.WebTests.TestFramework;
using Xamarin.WebTests.TestAttributes;

[assembly: DependencyProvider (typeof (SharedWebTestFeatures.Provider))]

namespace Xamarin.WebTests.TestFramework
{
	using ConnectionFramework;
	using Xamarin.WebTests.Resources;

	public sealed class SharedWebTestFeatures : ITestConfigurationProvider, ISingletonInstance
	{
		public static SharedWebTestFeatures Instance {
			get { return DependencyInjector.Get<SharedWebTestFeatures> (); }
		}

		internal class Provider : IDependencyProvider
		{
			public void Initialize ()
			{
				DependencyInjector.RegisterDependency<SharedWebTestFeatures> (() => new SharedWebTestFeatures ());
			}
		}

		public string Name {
			get { return "SharedWebTestFeatures"; }
		}

		public IEnumerable<TestFeature> Features {
			get {
				yield return TestFeature.ForkedSupport;
				yield return TestFeature.ForkedProcessSupport;
				yield return ExperimentalAttribute.Instance;
				yield return NotWorkingAttribute.Instance;
				yield return RecentlyFixedAttribute.Instance;
				yield return CertificateStoreAttribute.Instance;

				yield return ManualSslStreamAttribute.Instance;

				yield return ManualSelectAttribute.Instance;
			}
		}

		public IEnumerable<TestCategory> Categories {
			get {
				yield return WorkAttribute.Instance;
				yield return NewAttribute.Instance;
				yield return StressAttribute.Instance;
				yield return LongRunningAttribute.Instance;

				yield return ManualClientAttribute.Instance;
				yield return ManualServerAttribute.Instance;
			}
		}

		SharedWebTestFeatures ()
		{
			var factory = DependencyInjector.Get<ConnectionProviderFactory> ();
			if (factory.FrameworkSetup.InstallDefaultCertificateValidator) {
				var provider = DependencyInjector.Get<ICertificateProvider> ();
				var defaultValidator = provider.AcceptThisCertificate (ResourceManager.SelfSignedServerCertificate);
				provider.InstallDefaultValidator (defaultValidator);
			}
		}

		public static bool HasMonoVersion (Version version)
		{
			var support = DependencyInjector.Get<IPortableSupport> ();
			if (support.IsMicrosoftRuntime)
				return true;
			return support.MonoRuntimeVersion != null && support.MonoRuntimeVersion >= version;
		}
	}
}

