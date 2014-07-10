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

		public TestConfiguration Configuration {
			get;
			private set;
		}

		int selectedIndex;
		List<TestCategory> categories;

		public IList<TestCategory> Categories {
			get { return categories; }
		}

		public int SelectedIndex {
			get { return selectedIndex; }
			set {
				if (selectedIndex == value)
					return;
				selectedIndex = value;
				if (selectedIndex >= 0) {
					Configuration.CurrentCategory = Categories [selectedIndex];
					SaveSettings ();
				}
			}
		}

		public TestCategoriesModel (TestApp app, TestConfiguration config)
		{
			App = app;
			Configuration = config;

			categories = new List<TestCategory> ();
			Configuration.PropertyChanged += OnConfigurationChanged;
			UpdateConfiguration ();
		}

		void OnConfigurationChanged (object sender, PropertyChangedEventArgs args)
		{
			if (args.PropertyName == "CurrentCategory") {
				SelectedIndex = categories.IndexOf (Configuration.CurrentCategory);
				return;
			} else if (args.PropertyName != "Categories") {
				return;
			}

			UpdateConfiguration ();
		}

		void UpdateConfiguration ()
		{
			SelectedIndex = -1;

			categories.Clear ();
			foreach (var category in Configuration.Categories) {
				categories.Add (category);
			}
			selectedIndex = categories.IndexOf (Configuration.CurrentCategory);
			LoadSettings ();
			OnPropertyChanged ("Categories");
			OnPropertyChanged ("SelectedIndex");
			OnPropertyChanged ("Configuration");
		}

		void LoadSettings ()
		{
			if (App.SettingsHost == null)
				return;

			var value = App.SettingsHost.GetValue ("CurrentCategory");
			if (value != null) {
				var index = categories.FindIndex (c => c.Name.Equals (value));
				if (index >= 0) {
					selectedIndex = index;
					Configuration.CurrentCategory = categories [selectedIndex];
				} else if (categories.Count > 0)
					selectedIndex = 0;
				else
					selectedIndex = -1;
			}
		}

		void SaveSettings ()
		{
			if (App.SettingsHost == null || !App.TestSuiteManager.HasInstance)
				return;

			if (selectedIndex >= 0)
				App.SettingsHost.SetValue ("CurrentCategory", categories [selectedIndex].Name);
		}
	}
}

