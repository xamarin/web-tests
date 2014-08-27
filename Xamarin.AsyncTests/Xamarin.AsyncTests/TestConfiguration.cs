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

		bool Load (bool sendEvent)
		{
			bool modified = false;
			foreach (var feature in provider.Features) {
				bool enabled;
				if (feature.Constant != null)
					enabled = feature.Constant.Value;
				else
					enabled = IsFeatureEnabled (feature.Name) ?? feature.DefaultValue ?? false;
				if (features [feature.Name] != enabled) {
					features [feature.Name] = enabled;
					modified = true;
					OnPropertyChanged ("Feature");
				}
			}

			var category = GetCurrentCategory ();
			if (category != null && category != currentCategory) {
				currentCategory = category;
				modified = true;
				OnPropertyChanged ("Category");
			}

			return modified;
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
			return features [feature.Name];
		}

		public void SetIsEnabled (TestFeature feature, bool enabled)
		{
			SetIsFeatureEnabled (feature.Name, enabled);
		}
			
		bool? IsFeatureEnabled (string name)
		{
			var key = "/Feature/" + name;
			string value;
			if (!settings.TryGetValue (key, out value))
				return null;
			return bool.Parse (value);
		}

		void SetIsFeatureEnabled (string name, bool enabled)
		{
			if (features [name] == enabled)
				return;
			features [name] = enabled;
			var key = "/Feature/" + name;
			settings.SetValue (key, enabled.ToString ());
			OnPropertyChanged ("Feature");
		}

		TestCategory GetCurrentCategory ()
		{
			string key;
			if (!settings.TryGetValue ("CurrentCategory", out key))
				return TestCategory.All;

			return categories.FirstOrDefault (c => c.Name.Equals (key)) ?? TestCategory.All;
		}

		void SetCurrentCategory (string value)
		{
			settings.SetValue ("CurrentCategory", value);
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

