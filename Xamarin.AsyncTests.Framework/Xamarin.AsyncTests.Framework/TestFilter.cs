//
// TestFilter.cs
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
using System.Collections.Generic;

namespace Xamarin.AsyncTests.Framework
{
	class TestFilter
	{
		public TestBuilder Builder {
			get;
		}

		public TestFilter Parent {
			get;
		}

		public bool MustMatch {
			get;
		}

		public ITestConfiguration Configuration {
			get;
		}

		public IReadOnlyCollection<TestCategoryAttribute> Categories {
			get;
		}

		public IReadOnlyCollection<TestFeature> Features {
			get;
		}

		public TestFilter (TestBuilder builder, TestFilter parent, bool mustMatch,
				   IReadOnlyCollection<TestCategoryAttribute> categories,
				   IReadOnlyCollection<TestFeature> features)
		{
			Builder = builder;
			Parent = parent;
			MustMatch = mustMatch;
			Categories = categories;
			Features = features;
		}

		bool HandleMartinAttr (ITestConfiguration config, string searchString, TestInstance instance, MartinAttribute attr)
		{
			if (config.CurrentCategory != TestCategory.Martin)
				return false;
			if (attr.UseFixtureName)
				return HandleMartinFixtureFilter (searchString, instance);
			if (attr.IsExplicit && !string.Equals (searchString, attr.Parameter, StringComparison.OrdinalIgnoreCase))
				return false;
			if (string.IsNullOrEmpty (searchString) ||
			   string.Equals (searchString, "all", StringComparison.OrdinalIgnoreCase) ||
			    string.Equals (searchString, attr.Parameter, StringComparison.OrdinalIgnoreCase))
				return true;
			return false;
		}

		bool HandleMartinFixtureFilter (string searchString, TestInstance instance)
		{
			var fixture = TestBuilder.GetFixtureBuilder (Builder);
			if (fixture == null)
				return false;

			var query = searchString.Split (':');
			string fixtureQuery;
			string parameterQuery;
			var pos = searchString.IndexOf (":", StringComparison.Ordinal);
			if (pos > 0) {
				fixtureQuery = searchString.Substring (0, pos);
				parameterQuery = searchString.Substring (pos + 1);
			} else {
				fixtureQuery = searchString;
				parameterQuery = null;
			}

			if (!string.Equals (query[0], fixture.Name, StringComparison.Ordinal))
				return false;

			if (query.Length == 1 || instance == null)
				return true;

			for (int i = 1; i < query.Length; i++) {
				var split = query[i].Split ('=');
				if (split.Length != 2)
					throw new InvalidOperationException ($"Invalid query: '{query}'");

				var matches = FilterParameter (instance, split[0], split[1]);
				if (!matches)
					return false;
			}

			return true;
		}

		bool FilterParameter (TestInstance instance, string key, string value)
		{
			for (; instance != null; instance = instance.Parent) {
				if (instance.Node.PathType != TestPathType.Parameter)
					continue;
				if (!string.Equals (instance.Node.Name, key, StringComparison.Ordinal))
					continue;
				var parameter = instance.GetCurrentParameter ();
				return string.Equals (
					parameter.Parameter.Value, value,
					StringComparison.OrdinalIgnoreCase);
			}

			return true;
		}

		public bool Filter (TestContext ctx, TestInstance instance, out bool enabled)
		{
			return Filter (ctx.Configuration, ctx.Settings, instance, out enabled);
		}

		bool Filter (ITestConfiguration config, SettingsBag settings, TestInstance instance, out bool enabled)
		{
			if (RunFilter (config, settings, instance, out enabled))
				return true;

			if (Parent != null && Parent.Filter (config, settings, instance, out enabled))
				return true;

			if (config.CurrentCategory == TestCategory.All) {
				enabled = true;
				return true;
			}

			if (MustMatch) {
				enabled = false;
				return true;
			}

			return false;
		}

		bool RunFilter (ITestConfiguration config, SettingsBag settings, TestInstance instance, out bool enabled)
		{
			if (Categories.Any (attr => attr.Category == TestCategory.Global)) {
				enabled = true;
				return true;
			}

			foreach (var feature in Features) {
				if (!config.IsEnabled (feature)) {
					enabled = false;
					return true;
				}
			}

			foreach (var attr in Categories) {
				if (attr is MartinAttribute martin) {
					enabled = HandleMartinAttr (config, settings.MartinTest, instance, martin);
					return true;
				}
				if (config.CurrentCategory == attr.Category) {
					enabled = true;
					return true;
				}
				if (attr.Category.IsExplicit) {
					enabled = false;
					return true;
				}
			}

			if (config.CurrentCategory == TestCategory.All) {
				enabled = true;
				return true;
			}

			enabled = false;
			return false;
		}
	}
}

