//
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

namespace Xamarin.WebTests.Features
{
	using Portable;

	public abstract class SharedWebTestFeatures : ITestConfigurationProvider, IWebTestFeatures
	{
		public readonly TestFeature HasNetwork;

		public readonly TestFeature Mono38;
		public readonly TestFeature Mono381;
		public readonly TestFeature Mono361;

		public abstract string Name {
			get;
		}

		public abstract TestFeature SSL {
			get;
		}

		public TestFeature CertificateTests {
			get;
			private set;
		}

		public virtual IEnumerable<TestFeature> Features {
			get {
				yield return SSL;

				yield return HasNetwork;
				yield return Mono38;
				yield return Mono381;
				yield return Mono361;

				yield return ExperimentalAttribute.Instance;
				yield return IncludeNotWorkingAttribute.Instance;
				yield return MonoWithNewTlsAttribute.Instance;

				yield return CertificateTests;
				yield return PuppyAttribute.Instance;
			}
		}

		public virtual IEnumerable<TestCategory> Categories {
			get {
				yield return WorkAttribute.Instance;
				yield return MartinAttribute.Instance;

				yield return NotWorkingAttribute.Instance;
				yield return ManualClientAttribute.Instance;
				yield return ManualServerAttribute.Instance;
			}
		}

		public SharedWebTestFeatures ()
		{
			DependencyInjector.RegisterDependency<IWebTestFeatures> (() => this);

			Mono38 = new TestFeature (
				"Mono38", "Mono 3.8.0", () => HasMonoVersion (new Version (3, 8, 0)));
			Mono381 = new TestFeature (
				"Mono381", "Mono 3.8.1", () => HasMonoVersion (new Version (3, 8, 1)));
			Mono361 = new TestFeature (
				"Mono361", "Mono 3.6.1", () => HasMonoVersion (new Version (3, 6, 1)));
			CertificateTests = new TestFeature (
				"CertificateTests", "Whether the SSL Certificate tests are supported", () => SupportsCertificateTests ());
			HasNetwork = new TestFeature ("Network", "HasNetwork", () => IsNetworkAvailable ());
		}

		protected virtual bool IsNetworkAvailable ()
		{
			var support = DependencyInjector.Get<IPortableWebSupport> ();
			return support.HasNetwork;
		}

		protected virtual bool SupportsCertificateTests ()
		{
			var support = DependencyInjector.Get<IPortableWebSupport> ();
			return support.SupportsPerRequestCertificateValidator;
		}

		protected virtual bool HasMonoVersion (Version version)
		{
			var support = DependencyInjector.Get<IPortableSupport> ();
			if (support.IsMicrosoftRuntime)
				return true;
			return support.MonoRuntimeVersion != null && support.MonoRuntimeVersion >= version;
		}
	}
}

