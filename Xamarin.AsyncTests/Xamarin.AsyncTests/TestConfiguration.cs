//
// TestConfiguration2.cs
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

namespace Xamarin.AsyncTests
{
	public class TestConfiguration : ITestConfiguration, INotifyPropertyChanged
	{
		ITestConfigurationProvider provider;
		SettingsBag settings;
		Dictionary<string, bool> features;
		List<TestCategory> categories;
		TestCategory currentCategory;

		public TestConfiguration (ITestConfigurationProvider provider, SettingsBag settings)
		{
			this.provider = provider;
			this.settings = settings;
			features = new Dictionary<string, bool> ();
			categories = new List<TestCategory> ();

			foreach (var feature in provider.Features) {
				features.Add (feature.Name, false);
			}

			categories.AddRange (provider.Categories);
			currentCategory = TestCategory.All;
			Load (false);

			settings.PropertyChanged += (sender, e) => Load (true);
		}

		internal void Reload ()
		{
			Load (false);
		}

		void Load (bool sendEvent)
		{
			bool featuresModified = false;
			foreach (var feature in provider.Features) {
				featuresModified |= LoadFeature (feature);
			}

			if (featuresModified && sendEvent)
				OnPropertyChanged ("Feature");

			var categoryModifed = LoadCurrentCategory ();
			if (categoryModifed && sendEvent)
				OnPropertyChanged ("CurrentCategory");
		}

		public IEnumerable<TestFeature> Features {
			get { return provider.Features; }
		}

		public IEnumerable<TestCategory> Categories {
			get { return provider.Categories; }
		}

		public TestCategory CurrentCategory {
			get { return currentCategory; }
			set {
				if (currentCategory == value)
					return;
				currentCategory = value;
				SetCurrentCategory (value.Name);
			}
		}

		public bool IsEnabled (TestFeature feature)
		{
			if (feature.Function != null)
				return feature.Function (settings);
			return features [feature.Name];
		}

		public void SetIsEnabled (TestFeature feature, bool enabled)
		{
			SetIsFeatureEnabled (feature.Name, enabled);
		}

		bool LoadFeature (TestFeature feature)
		{
			if (feature.Function != null)
				return false;

			if (feature.Constant != null) {
				features [feature.Name] = feature.Constant.Value;
				return false;
			}

			var key = provider.Name + ".Feature." + feature.Name;
			string value;
			if (settings.TryGetValue (key, out value)) {
				var enabled = bool.Parse (value);
				if (features [feature.Name] == enabled)
					return false;
				features [feature.Name] = enabled;
				return true;
			} else {
				var enabled = feature.DefaultValue ?? false;
				settings.SetValue (key, enabled.ToString ());
				features [feature.Name] = enabled;
				return true;
			}
		}

		void SetIsFeatureEnabled (string name, bool enabled)
		{
			if (features [name] == enabled)
				return;
			features [name] = enabled;
			var key = provider.Name + ".Feature." + name;
			settings.SetValue (key, enabled.ToString ());
			OnPropertyChanged ("Feature");
		}

		bool LoadCurrentCategory ()
		{
			var key = provider.Name + ".CurrentCategory";
			string value;

			if (settings.TryGetValue (key, out value)) {
				TestCategory category;
				if (string.Equals (value, "all", StringComparison.OrdinalIgnoreCase))
					category = TestCategory.All;
				else if (string.Equals (value, "global", StringComparison.OrdinalIgnoreCase))
					category = TestCategory.Global;
				else
					category = categories.FirstOrDefault (c => c.Name.Equals (value)) ?? TestCategory.All;
				if (category == currentCategory)
					return false;
				currentCategory = category;
				return true;
			}

			currentCategory = TestCategory.All;
			settings.SetValue (key, currentCategory.Name);
			return true;
		}

		void SetCurrentCategory (string value)
		{
			var key = provider.Name + ".CurrentCategory";
			settings.SetValue (key, value);
			OnPropertyChanged ("CurrentCategory");
		}

		class ReadOnlySnapshot : ITestConfiguration
		{
			IReadOnlyDictionary<string, bool> features;
			TestCategory currentCategory;

			public ReadOnlySnapshot (ITestConfigurationProvider provider, IReadOnlyDictionary<string, bool> features, TestCategory currentCategory)
			{
				this.features = features;
				this.currentCategory = currentCategory;
			}

			public bool IsEnabled (TestFeature feature)
			{
				return features [feature.Name];
			}

			public TestCategory CurrentCategory {
				get { return currentCategory; }
			}
		}

		public ITestConfiguration AsReadOnly ()
		{
			return new ReadOnlySnapshot (provider, features, currentCategory);
		}

		class EverythingEnabled : ITestConfiguration
		{
			bool ITestConfiguration.IsEnabled (TestFeature feature)
			{
				return true;
			}

			TestCategory ITestConfiguration.CurrentCategory {
				get { return TestCategory.All; }
			}
		}

		public static ITestConfiguration CreateEverythingEnabled ()
		{
			return new EverythingEnabled ();
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

