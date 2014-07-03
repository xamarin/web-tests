//
// TestConfiguration.cs
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

namespace Xamarin.AsyncTests
{
	public class TestConfiguration : INotifyPropertyChanged
	{
		Dictionary<TestFeature,bool> features = new Dictionary<TestFeature, bool> ();
		List<TestCategory> categories = new List<TestCategory> ();
		TestCategory currentCategory;

		public TestConfiguration ()
		{
			categories.Add (TestCategory.All);
			currentCategory = TestCategory.All;
		}

		public IEnumerable<TestFeature> Features {
			get { return features.Keys; }
		}

		public IEnumerable<TestCategory> Categories {
			get { return categories; }
		}

		public void AddTestSuite (ITestConfiguration config)
		{
			foreach (var feature in config.Features) {
				features.Add (feature, feature.Constant ?? feature.DefaultValue ?? false);
			}
			foreach (var category in config.Categories) {
				categories.Add (category);
			}
			currentCategory = config.DefaultCategory ?? TestCategory.All;
			OnPropertyChanged ("Features");
			OnPropertyChanged ("Categories");
			OnPropertyChanged ("CurrentCategory");
		}

		public void Clear ()
		{
			currentCategory = TestCategory.All;
			categories.RemoveAll (c => !c.IsBuiltin);
			features.Clear ();
			OnPropertyChanged ("Features");
			OnPropertyChanged ("Categories");
			OnPropertyChanged ("CurrentCategory");
		}

		public void AddCategory (TestCategory category)
		{
			if (category.IsBuiltin)
				throw new InvalidOperationException ();
			categories.Add (category);
			OnPropertyChanged ("Categories");
		}

		public void AddFeature (TestFeature feature, bool enabled)
		{
			features.Add (feature, enabled);
			OnPropertyChanged ("Features");
		}

		public void Merge (TestConfiguration other, bool fullUpdate)
		{
			currentCategory = TestCategory.All;
			categories.RemoveAll (c => !c.IsBuiltin);
			features.Clear ();

			foreach (var feature in other.features)
				features.Add (feature.Key, feature.Value);
			foreach (var category in other.categories) {
				if (!category.IsBuiltin)
					categories.Add (category);
			}
			currentCategory = other.currentCategory;
			OnPropertyChanged ("Features");
			OnPropertyChanged ("Categories");
			OnPropertyChanged ("CurrentCategory");
		}

		public TestCategory CurrentCategory {
			get { return currentCategory; }
			set {
				if (currentCategory == value)
					return;
				currentCategory = value;
				OnPropertyChanged ("CurrentCategory");
			}
		}

		public bool IsEnabled (TestFeature feature)
		{
			return features [feature];
		}

		public bool CanModify (TestFeature feature)
		{
			return feature.Constant == null;
		}

		public void Enable (TestFeature feature)
		{
			if (!features [feature]) {
				if (!CanModify (feature))
					throw new InvalidOperationException ();
				features [feature] = true;
			}
		}

		public void Disable (TestFeature feature)
		{
			if (features [feature]) {
				if (!CanModify (feature))
					throw new InvalidOperationException ();
				features [feature] = false;
			}
		}

		#region INotifyPropertyChanged implementation

		public event PropertyChangedEventHandler PropertyChanged;

		protected virtual void OnPropertyChanged (string propertyName)
		{
			if (PropertyChanged != null)
				PropertyChanged (this, new PropertyChangedEventArgs (propertyName));
		}

		#endregion
	}
}

