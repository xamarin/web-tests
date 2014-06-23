//
// TestResultCollectionModel.cs
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
using System.Collections.ObjectModel;

namespace Xamarin.AsyncTests.UI
{
	using Framework;

	public class TestResultCollectionModel : TestResultModel
	{
		readonly ObservableCollection<TestResultModel> children = new ObservableCollection<TestResultModel> ();

		new public TestResultCollection Result {
			get { return (TestResultCollection)base.Result; }
		}

		public TestResultCollectionModel ()
			: base (new TestResultCollection ())
		{
		}

		public TestResultCollectionModel (TestResultCollection collection)
			: base (collection)
		{
			foreach (var child in collection.Children)
				AddTestResult (child);
		}

		public override string DetailedStatus {
			get {
				var numErrors = Result.Children.Count (t => t.Status == TestStatus.Error);
				var numWarnings = Result.Children.Count (t => t.Status == TestStatus.Warning);
				var numSuccess = Result.Children.Count (t => t.Status == TestStatus.Success);

				return string.Format ("{0} passed, {1} warnings, {2} errors", numSuccess, numWarnings, numErrors);
			}
		}

		public ObservableCollection<TestResultModel> Children {
			get { return children; }
		}

		TestResultModel CreateModel (TestResult result)
		{
			var collection = result as TestResultCollection;
			if (collection != null)
				return new TestResultCollectionModel (collection);
			return new TestResultModel (result);
		}

		public void AddTestResult (TestResult result)
		{
			var collection = result as TestResultCollection;
			if (collection == null || collection.Name != null) {
				children.Add (CreateModel (result));
				return;
			}

			if (collection.Children.Count == 1) {
				AddTestResult (collection.Children [0]);
				return;
			}

			foreach (var child in collection.Children) {
				AddTestResult (child);
			}
		}

		public void Clear ()
		{
			children.Clear ();
		}
	}
}

