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
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using System.ComponentModel;
using System.Collections.Generic;

namespace Xamarin.AsyncTests
{
	public class TestConfiguration : INotifyPropertyChanged
	{
		Dictionary<TestFeature,bool> features = new Dictionary<TestFeature, bool> ();
		List<TestCategory> categories = new List<TestCategory> ();
		TestCategory currentCategory;
		SettingsBag settings;

		TestConfiguration (SettingsBag settings)
		{
			this.settings = settings;
			if (settings == null)
				throw new InvalidOperationException ();
			categories.Add (TestCategory.All);
			currentCategory = TestCategory.All;
		}

		public IEnumerable<TestFeature> Features {
			get { return features.Keys; }
		}

		public IEnumerable<TestCategory> Categories {
			get { return categories; }
		}

		public static TestConfiguration FromTestSuite (SettingsBag settings, ITestConfiguration config)
		{
			var configuration = new TestConfiguration (settings);
			foreach (var feature in config.Features) {
				var defaultValue = settings.IsFeatureEnabled (feature.Name);
				configuration.features.Add (feature, feature.Constant ?? defaultValue ?? false);
			}

			configuration.currentCategory = null;
			foreach (var category in config.Categories) {
				configuration.categories.Add (category);
				if (category.Name.Equals (settings.CurrentCategory))
					configuration.currentCategory = category;
			}
			if (configuration.currentCategory == null)
				configuration.currentCategory = config.DefaultCategory ?? TestCategory.All;
			return configuration;
		}

		internal void Update (SettingsBag newSettings)
		{
			this.settings = newSettings;

			foreach (var feature in features.Keys.ToArray ()) {
				if (!feature.CanModify)
					continue;
				var value = settings.IsFeatureEnabled (feature.Name);
				if (value == null)
					continue;
				SetIsEnabled (feature, value.Value);
			}

			TestCategory current = TestCategory.All;
			foreach (var category in categories) {
				if (category.Name.Equals (settings.CurrentCategory))
					current = category;
			}
			CurrentCategory = current;
		}

		public static TestConfiguration ReadFromXml (SettingsBag settings, XElement node)
		{
			if (node == null)
				return null;
			if (!node.Name.LocalName.Equals ("TestConfiguration"))
				throw new InvalidOperationException ();

			var config = new TestConfiguration (settings);
			foreach (var item in node.Elements ("Category")) {
				var category = new TestCategory (item.Attribute ("Name").Value);
				config.categories.Add (category);
			}
			foreach (var item in node.Elements ("Feature")) {
				var name = item.Attribute ("Name").Value;
				var description = item.Attribute ("Description").Value;
				var constant = item.Attribute ("Constant");
				var defaultValue = item.Attribute ("DefaultValue");

				TestFeature feature;
				if (constant != null) {
					var constantValue = bool.Parse (constant.Value);
					feature = new TestFeature (name, description, () => constantValue);
				} else if (defaultValue != null)
					feature = new TestFeature (name, description, bool.Parse (defaultValue.Value));
				else
					feature = new TestFeature (name, description);

				var value = settings.IsFeatureEnabled (feature.Name);
				config.features.Add (feature, feature.Constant ?? value ?? false);
			}
			return config;
		}

		public XElement WriteToXml ()
		{
			var element = new XElement ("TestConfiguration");

			foreach (var category in Categories) {
				if (category.IsBuiltin)
					continue;

				var node = new XElement ("Category");
				node.SetAttributeValue ("Name", category.Name);
				if (category == CurrentCategory)
					node.SetAttributeValue ("IsCurrent", "true");
				element.Add (node);
			}

			foreach (var feature in Features) {
				var node = new XElement ("Feature");
				node.SetAttributeValue ("Name", feature.Name);
				node.SetAttributeValue ("Description", feature.Description);

				if (feature.Constant != null)
					node.SetAttributeValue ("Constant", feature.Constant.Value.ToString ());
				if (feature.DefaultValue != null)
					node.SetAttributeValue ("DefaultValue", feature.DefaultValue.Value.ToString ());
				element.Add (node);
			}

			return element;
		}

		public TestCategory CurrentCategory {
			get { return currentCategory; }
			set {
				if (currentCategory == value)
					return;
				currentCategory = value;
				settings.CurrentCategory = value.Name;
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

		public void SetIsEnabled (TestFeature feature, bool enabled)
		{
			if (features [feature] == enabled)
				return;
			if (!CanModify (feature))
				throw new InvalidOperationException ();
			features [feature] = enabled;
			settings.SetIsFeatureEnabled (feature.Name, enabled);
			OnPropertyChanged ("Feature");
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

