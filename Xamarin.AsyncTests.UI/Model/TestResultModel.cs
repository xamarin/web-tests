//
// ITestResultModel.cs
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
using System.Text;
using System.ComponentModel;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using Xamarin.Forms;

namespace Xamarin.AsyncTests.UI
{
	using Framework;

	public class TestResultModel : BindableObject
	{
		public TestApp App {
			get;
			private set;
		}

		public TestResult Result {
			get;
			private set;
		}

		public bool IsRoot {
			get;
			private set;
		}

		public Color StatusColor {
			get { return TestResultConverter.GetColorForStatus (Result.Status); }
		}

		public ObservableCollection<TestResultModel> Children {
			get;
			private set;
		}

		public bool HasChildren {
			get { return Children.Count > 0; }
		}

		public TestResultModel (TestApp app, TestResult result, bool isRoot)
		{
			App = app;
			Result = result;
			IsRoot = isRoot;

			Children = new ObservableCollection<TestResultModel> ();

			result.Children.CollectionChanged += HandleCollectionChanged;

			result.PropertyChanged += (sender, e) => {
				OnPropertyChanged ("StatusColor");
			};

			Children.CollectionChanged += (sender, e) => {
				;
				OnPropertyChanged ("HasChildren");
			};
		}

		void HandleCollectionChanged (object sender, NotifyCollectionChangedEventArgs e)
		{
			switch (e.Action) {
			case NotifyCollectionChangedAction.Add:
				foreach (var item in e.NewItems)
					Children.Add (new TestResultModel (App, (TestResult)item, false));
				break;

			default:
				Children.Clear ();
				foreach (var child in Result.Children)
					Children.Add (new TestResultModel (App, child, false));
				break;
			}
		}
	}
}

