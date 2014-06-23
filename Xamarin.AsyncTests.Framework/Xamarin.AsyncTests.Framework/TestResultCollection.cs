//
// Xamarin.AsyncTests.Framework.TestResultCollection
//
// Authors:
//      Martin Baulig (martin.baulig@gmail.com)
//
// Copyright 2012 Xamarin Inc. (http://www.xamarin.com)
//
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//
using System;
using System.Linq;
using System.ComponentModel;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Xamarin.AsyncTests.Framework {

	public class TestResultCollection : TestResult
	{
		public TestResultCollection ()
			: this (null)
		{
		}

		public TestResultCollection (TestName name)
			: base (name, TestStatus.Ignored)
		{
			messages = new ObservableCollection<string> ();
			((INotifyPropertyChanged)messages).PropertyChanged += (sender, e) => OnMessagesChanged ();
			children = new ObservableCollection<TestResult> ();
			((INotifyPropertyChanged)children).PropertyChanged += (sender, e) => OnChildrenChanged ();
		}

		ObservableCollection<TestResult> children;
		ObservableCollection<string> messages;

		public ObservableCollection<TestResult> Children {
			get { return children; }
		}

		public ObservableCollection<string> Messages {
			get { return messages; }
		}

		void OnMessagesChanged ()
		{
			OnPropertyChanged ("Messages");
		}

		void OnChildrenChanged ()
		{
			if (children.Count == 0)
				Status = TestStatus.Ignored;
			else {
				var hasErrors = children.Any (child => child.Status == TestStatus.Error);
				if (hasErrors)
					Status = TestStatus.Error;
				else if (Status == TestStatus.Ignored)
					Status = TestStatus.Success;
			}

			OnPropertyChanged ("Children");
		}

		public void AddMessage (string format, params object[] args)
		{
			messages.Add (string.Format (format, args));
		}

		public void AddWarnings (IEnumerable<TestResult> warnings)
		{
			foreach (var warning in warnings)
				children.Add (warning);
		}

		public void AddChild (TestResult result, bool flatten = false)
		{
			var collection = result as TestResultCollection;
			if (collection == null || !flatten || collection.Name != null) {
				result.PropertyChanged += (sender, e) => OnChildrenChanged ();
				children.Add (result);
				return;
			}

			foreach (var child in collection.Children) {
				AddChild (child, flatten);
			}
		}

		public void Clear ()
		{
			children.Clear ();
			messages.Clear ();
		}
	}
}
