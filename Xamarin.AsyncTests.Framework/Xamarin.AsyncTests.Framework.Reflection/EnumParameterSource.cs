//
// EnumParameterSource.cs
//
// Author:
//       Martin Baulig <mabaul@microsoft.com>
//
// Copyright (c) 2018 Xamarin Inc. (http://www.xamarin.com)
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
using System.Reflection;
using System.Collections.Generic;

namespace Xamarin.AsyncTests.Framework.Reflection
{
	class EnumParameterSource<T> : ITestParameterSource<T>
		where T : struct
	{
		Dictionary<string, Tuple<T, EnumValueFilter>> values;

		public IEnumerable<T> GetParameters (TestContext ctx, string filter)
		{
			SetupValues ();
			foreach (var value in values.Values) {
				if (value.Item2?.Filter (ctx, value.Item1) ?? true)
					yield return value.Item1;
			}
		}

		void SetupValues ()
		{
			if (values != null)
				return;
			values = new Dictionary<string, Tuple<T, EnumValueFilter>> ();
			var typeInfo = typeof (T).GetTypeInfo ();
			foreach (var field in typeInfo.DeclaredFields) {
				if (field.IsSpecialName)
					continue;
				if (!Enum.TryParse (field.Name, out T value))
					continue;

				var filter = GetFilter (field);
				values.Add (field.Name, new Tuple<T, EnumValueFilter> (value, filter));
			}
		}

		EnumValueFilter GetFilter (FieldInfo field)
		{
			var filters = new List<EnumValueFilter> ();
			var cattr = field.GetCustomAttribute<EnumValueFilter> ();
			if (cattr != null)
				filters.Add (cattr);

			var category = field.GetCustomAttribute<TestCategoryAttribute> ();
			if (category != null)
				filters.Add (ReflectionHelper.GetEnumValueFilter (category.Category));

			foreach (var feature in field.GetCustomAttributes<TestFeatureAttribute> ())
				filters.Add (ReflectionHelper.GetEnumValueFilter (feature.Feature));

			if (filters.Count == 0)
				return null;
			if (filters.Count == 1)
				return filters[0];
			return ReflectionHelper.GetEnumValueFilter (filters);
		}
	}
}
