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
		Dictionary<string,TestFeature> features = new Dictionary<string,TestFeature> ();
		Dictionary<string,TestCategory> categories = new Dictionary<string,TestCategory> ();

		public TestConfiguration ()
		{
			categories.Add (TestCategory.All.Name, TestCategory.All);
		}

		public IEnumerable<TestFeature> Features {
			get { return features.Values; }
		}

		public IEnumerable<TestCategory> Categories {
			get { return categories.Values; }
		}

		public bool TryGetCategory (string name, out TestCategory category)
		{
			return categories.TryGetValue (name, out category);
		}

		public static TestConfiguration FromTestSuite (ITestConfiguration config)
		{
			var configuration = new TestConfiguration ();
			foreach (var feature in config.Features)
				configuration.features.Add (feature.Name, feature);
			foreach (var category in config.Categories)
				configuration.categories.Add (category.Name, category);
			return configuration;
		}

		public static TestConfiguration ReadFromXml (XElement node)
		{
			if (node == null)
				return null;
			if (!node.Name.LocalName.Equals ("TestConfiguration"))
				throw new InvalidOperationException ();

			var config = new TestConfiguration ();
			foreach (var item in node.Elements ("Category")) {
				var category = new TestCategory (item.Attribute ("Name").Value);
				config.categories.Add (category.Name, category);
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

				config.features.Add (feature.Name, feature);
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

		public bool CanModify (TestFeature feature)
		{
			return feature.Constant == null;
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

