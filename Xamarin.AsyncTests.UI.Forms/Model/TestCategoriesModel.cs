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

namespace Xamarin.AsyncTests.UI.Forms
{
	public class TestCategoriesModel : BindableObject
	{
		public UITestApp App {
			get;
			private set;
		}

		int selectedIndex;
		List<TestCategory> categories;
		List<string> categoryNames;

		public IList<string> Categories {
			get { return categoryNames; }
		}

		public int SelectedIndex {
			get { return selectedIndex; }
			set {
				if (selectedIndex == value)
					return;
				selectedIndex = value;
				if (selectedIndex >= 0)
					App.Configuration.CurrentCategory = categories [selectedIndex];
			}
		}

		public TestCategoriesModel (UITestApp app)
		{
			App = app;
			categories = new List<TestCategory> ();
			categoryNames = new List<string> ();

			categories.Add (TestCategory.All);
			categoryNames.Add (TestCategory.All.Name);

			foreach (var category in app.Configuration.Categories) {
				categories.Add (category);
				categoryNames.Add (category.Name);
			}

			app.Settings.PropertyChanged += (sender, e) => LoadSettings ();
			LoadSettings ();
		}

		void LoadSettings ()
		{
			var category = App.Configuration.CurrentCategory;
			var index = categories.FindIndex (c => c.Equals (category));
			if (index >= 0)
				selectedIndex = index;
			else if (categories.Count > 0)
				selectedIndex = 0;
			else
				selectedIndex = -1;
		}
	}
}

