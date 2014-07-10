//
// TestCategorySelector.xaml.cs
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
	public partial class TestCategorySelector : ContentView
	{
		public TestCategorySelector ()
		{
			InitializeComponent ();

			Picker.SelectedIndexChanged += OnSelectedIndexChanged;
		}

		public TestCategoriesModel Model {
			get;
			private set;
		}

		protected override void OnBindingContextChanged ()
		{
			base.OnBindingContextChanged ();

			if (Model != null)
				Model.PropertyChanged -= OnConfigChanged;

			Model = (TestCategoriesModel)BindingContext;
			if (Model != null)
				Model.PropertyChanged += OnConfigChanged;

			LoadConfiguration ();
		}

		bool loadingConfig;

		void OnSelectedIndexChanged (object sender, EventArgs args)
		{
			if (!loadingConfig && Model != null)
				Model.SelectedIndex = Picker.SelectedIndex;
		}

		void OnConfigChanged (object sender, PropertyChangedEventArgs args)
		{
			BatchBegin ();
			switch (args.PropertyName) {
			case "SelectedItem":
				Picker.SelectedIndex = Model.SelectedIndex;
				break;
			case "Categories":
				LoadConfiguration ();
				break;
			}
			BatchCommit ();
		}

		void LoadConfiguration ()
		{
			loadingConfig = true;
			Picker.BatchBegin ();
			Picker.SelectedIndex = -1;
			Picker.Items.Clear ();

			if (Model != null) {
				foreach (var category in Model.Categories)
					Picker.Items.Add (category.Name);

				Picker.SelectedIndex = Model.SelectedIndex;
			}

			Picker.BatchCommit ();
			loadingConfig = false;
		}
	}
}

