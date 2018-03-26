﻿//
// SampleFeatures.cs
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
using Xamarin.AsyncTests.Sample;

[assembly: AsyncTestSuite (typeof (SampleFeatures))]
[assembly: DependencyProvider (typeof (SampleDependencyProvider))]

namespace Xamarin.AsyncTests.Sample
{
	public class NotWorkingAttribute : TestFeatureAttribute
	{
		public override TestFeature Feature {
			get { return SampleFeatures.NotWorking; }
		}
	}

	public class WorkAttribute : TestCategoryAttribute
	{
		public override TestCategory Category {
			get { return SampleFeatures.Work; }
		}
	}

	public class SampleDependencyProvider : IDependencyProvider
	{
		public void Initialize ()
		{
			DependencyInjector.RegisterDependency (() => new SampleFeatures ());
		}
	}

	public class SampleFeatures : ITestConfigurationProvider, ISingletonInstance
	{
		public static readonly SampleFeatures Instance;

		public static readonly TestCategory Work = new TestCategory ("Work");
		public static readonly TestFeature Hello = new TestFeature ("Hello", "Hello World");
		public static readonly TestFeature NotWorking = new TestFeature ("NotWorking", "Not Working");

		static SampleFeatures ()
		{
			Instance = new SampleFeatures ();
		}

		public string Name {
			get { return "Xamarin.AsyncTests.Sample"; }
		}

		public IEnumerable<TestFeature> Features {
			get {
				yield return Hello;
				yield return NotWorking;
			}
		}

		public IEnumerable<TestCategory> Categories {
			get {
				yield return Work;
			}
		}
	}
}

