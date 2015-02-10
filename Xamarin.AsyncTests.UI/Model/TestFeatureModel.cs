//
// TestFeatureModel.cs
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
using Xamarin.AsyncTests;
using Xamarin.AsyncTests.Framework;

namespace Xamarin.AsyncTests.UI
{
	public class TestFeatureModel : BindableObject
	{
		public UITestApp App {
			get;
			private set;
		}

		public TestFeature Feature {
			get;
			private set;
		}

		bool isEnabled;
		public bool IsEnabled {
			get {
				return isEnabled;
			}
			set {
				if (value == isEnabled)
					return;
				isEnabled = value;
				App.Configuration.SetIsEnabled (Feature, value);
				OnPropertyChanged ("Feature");
			}
		}

		string fullName;
		public string FullName {
			get { return fullName; }
		}

		public TestFeatureModel (UITestApp app, TestFeature feature)
		{
			App = app;
			Feature = feature;

			if (feature.Constant != null)
				fullName = string.Format ("{0} ({1})", feature.Name, feature.Constant.Value ? "enabled" : "disabled");
			else
				fullName = string.Empty;

			App.Settings.PropertyChanged += (sender, e) => LoadConfiguration ();
			LoadConfiguration ();
		}

		void LoadConfiguration ()
		{
			isEnabled = App.Configuration.IsEnabled (Feature);
			OnPropertyChanged ("IsEnabled");
		}
	}
}

