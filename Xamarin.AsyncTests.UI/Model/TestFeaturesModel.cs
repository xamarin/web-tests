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
using System.ComponentModel;
using System.Collections.Generic;
using Xamarin.Forms;

namespace Xamarin.AsyncTests.UI
{
	public class TestFeaturesModel : BindableObject
	{
		public TestApp App {
			get;
			private set;
		}

		List<TestFeatureModel> features;
		public IList<TestFeatureModel> Features {
			get { return features; }
		}

		public TestFeaturesModel (TestApp app)
		{
			App = app;

			features = new List<TestFeatureModel> ();
			app.Context.Features.PropertyChanged += (sender, e) => OnFeaturesChanged ();
		}

		void OnFeaturesChanged ()
		{
			features.Clear ();
			foreach (var feature in App.Context.Features.Features) {
				if (feature.CanModify)
					features.Add (new TestFeatureModel (App, feature));
			}
			LoadSettings ();
			OnPropertyChanged ("Features");
		}

		void LoadSettings ()
		{
			if (App.Settings == null)
				return;
			foreach (var feature in features) {
				if (!feature.Feature.CanModify)
					continue;
				var value = App.Settings.GetValue (feature.Path);
				if (value != null)
					feature.IsEnabled = bool.Parse (value);
			}
		}
	}
}

