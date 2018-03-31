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

		public IReadOnlyCollection<TestCategoryAttribute> Categories {
			get;
		}

		public IReadOnlyCollection<TestFeature> Features {
			get;
		}

		public TestFilter (TestBuilder builder, TestFilter parent,
				   IReadOnlyCollection<TestCategoryAttribute> categories,
				   IReadOnlyCollection<TestFeature> features)
		{
			Builder = builder;
			Parent = parent;
			Categories = categories;
			Features = features;
		}

		TestBuilder GetFixtureBuilder ()
		{
			TestBuilder builder = Builder;
			while (builder != null) {
				if (builder.PathType == TestPathType.Fixture)
					return builder;
				builder = builder.Parent;
			}
			return null;
		}

		bool HandleMartinAttr (TestContext ctx, TestInstance instance, MartinAttribute attr)
		{
			if (ctx.CurrentCategory != TestCategory.Martin)
				return false;
			if (attr.UseFixtureName)
				return HandleMartinFixtureFilter (ctx, instance, attr);
			if (attr.IsExplicit && !string.Equals (ctx.Settings.MartinTest, attr.Parameter, StringComparison.OrdinalIgnoreCase))
				return false;
			if (string.IsNullOrEmpty (ctx.Settings.MartinTest) ||
			   string.Equals (ctx.Settings.MartinTest, "all", StringComparison.OrdinalIgnoreCase) ||
			    string.Equals (ctx.Settings.MartinTest, attr.Parameter, StringComparison.OrdinalIgnoreCase))
				return true;
			return false;
		}

		bool HandleMartinFixtureFilter (TestContext ctx, TestInstance instance, MartinAttribute attr)
		{
			var fixture = GetFixtureBuilder ();
			if (fixture == null)
				return false;

			var searchString = ctx.Settings.MartinTest;
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

			ctx.LogDebug (10, $"MARTIN FILTER: {fixture.Name} {query}");
			TestInstance.LogDebug (ctx, instance, 10);

			for (int i = 1; i < query.Length; i++) {
				var split = query[i].Split ('=');
				if (split.Length != 2)
					throw ctx.AssertFail ($"Invalid query: '{query}'");

				var matches = FilterParameter (ctx, instance, split[0], split[1]);
				if (!matches)
					return false;
			}

			return true;
		}

		bool FilterParameter (TestContext ctx, TestInstance instance,
				      string key, string value)
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

		public bool Filter (TestContext ctx, TestInstance instance)
		{
			if (Filter (ctx, instance, out bool enabled))
				return enabled;

			if (Parent != null && Parent.Filter (ctx, instance, out enabled))
				return enabled;

			if (ctx.CurrentCategory == TestCategory.All)
				return true;

			return Parent == null;
		}

		bool Filter (TestContext ctx, TestInstance instance, out bool enabled)
		{
			if (Categories.Any (attr => attr.Category == TestCategory.Global)) {
				enabled = true;
				return true;
			}

			foreach (var feature in Features) {
				if (!ctx.IsEnabled (feature)) {
					enabled = false;
					return true;
				}
			}

			foreach (var attr in Categories) {
				if (attr is MartinAttribute martin) {
					enabled = HandleMartinAttr (ctx, instance, martin);
					return true;
				}
				if (ctx.CurrentCategory == attr.Category) {
					enabled = true;
					return true;
				}
				if (attr.Category.IsExplicit) {
					enabled = false;
					return true;
				}
			}

			if (ctx.CurrentCategory == TestCategory.All) {
				enabled = true;
				return true;
			}

			enabled = false;
			return false;
		}
	}
}

