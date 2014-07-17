//
// TestCategoriesModel.cs
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
using Xamarin.Forms;

namespace Xamarin.AsyncTests.UI
{
	public class TestCategoriesModel : BindableObject
	{
		public TestApp App {
			get;
			private set;
		}

		public readonly BindableProperty ConfigurationProperty = BindableProperty.Create (
			"Configuration", typeof(TestConfiguration), typeof(TestCategoriesModel), null,
			propertyChanged: (bo, o, n) => ((TestCategoriesModel)bo).UpdateConfiguration ());

		public TestConfiguration Configuration {
			get { return (TestConfiguration)GetValue (ConfigurationProperty); }
			set { SetValue (ConfigurationProperty, value); }
		}

		int selectedIndex;
		List<string> categories;

		public IList<string> Categories {
			get { return categories; }
		}

		public int SelectedIndex {
			get { return selectedIndex; }
			set {
				if (selectedIndex == value)
					return;
				selectedIndex = value;
				if (selectedIndex >= 0)
					App.Settings.CurrentCategory = Categories [selectedIndex];
			}
		}

		public TestCategoriesModel (TestApp app)
		{
			App = app;
			categories = new List<string> ();

			app.Settings.PropertyChanged += (sender, e) => LoadSettings ();
			LoadSettings ();
		}

		void OnConfigurationChanged (object sender, PropertyChangedEventArgs args)
		{
			if (args.PropertyName == "CurrentCategory") {
				LoadSettings ();
				OnPropertyChanged ("SelectedIndex");
				return;
			} else if (args.PropertyName != "Categories") {
				return;
			}

			UpdateConfiguration ();
		}

		void UpdateConfiguration ()
		{
			selectedIndex = -1;

			categories.Clear ();
			if (Configuration != null) {
				foreach (var category in Configuration.Categories) {
					categories.Add (category.Name);
				}
				LoadSettings ();
			}
			OnPropertyChanged ("Categories");
			OnPropertyChanged ("SelectedIndex");
			OnPropertyChanged ("Configuration");
		}

		void LoadSettings ()
		{
			var key = App.Settings.CurrentCategory;
			if (key != null) {
				var index = categories.FindIndex (c => c.Equals (key));
				if (index >= 0)
					selectedIndex = index;
				else if (categories.Count > 0)
					selectedIndex = 0;
				else
					selectedIndex = -1;
			}
		}
	}
}

