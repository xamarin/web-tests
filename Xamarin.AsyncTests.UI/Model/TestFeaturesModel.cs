//
// TestFeaturesModel.cs
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
using System.Linq;
using System.ComponentModel;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Xamarin.AsyncTests.Framework;

namespace Xamarin.AsyncTests.UI
{
	public class TestFeaturesModel : BindableObject
	{
		public UITestApp App {
			get;
			private set;
		}

		ObservableCollection<TestFeatureModel> features;
		ObservableCollection<TestFeatureModel> systemFeatures;

		public ObservableCollection<TestFeatureModel> Features {
			get { return features; }
		}

		public ObservableCollection<TestFeatureModel> SystemFeatures {
			get { return systemFeatures; }
		}

		public TestFeaturesModel (UITestApp app)
		{
			App = app;
			features = new ObservableCollection<TestFeatureModel> ();
			systemFeatures = new ObservableCollection<TestFeatureModel> ();

			features.Clear ();
			systemFeatures.Clear ();
			foreach (var feature in App.Configuration.Features) {
				if (feature.CanModify)
					features.Add (new TestFeatureModel (App, feature));
				else
					systemFeatures.Add (new TestFeatureModel (App, feature));
			}
		}
	}
}

