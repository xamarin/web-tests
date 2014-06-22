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
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Xamarin.AsyncTests.Framework {

	public class TestResultCollection : TestResult
	{
		public TestResultCollection ()
		{
		}

		public TestResultCollection (string name)
		{
			Name = name;
		}

		public override TestStatus Status {
			get { return HasErrors () ? TestStatus.Error : TestStatus.Success; }
		}

		ObservableCollection<TestResult> children = new ObservableCollection<TestResult> ();
		ObservableCollection<string> messages = new ObservableCollection<string> ();

		bool HasErrors ()
		{
			foreach (var child in children) {
				if (child.Status == TestStatus.Error)
					return true;
			}

			return false;
		}

		public bool HasChildren {
			get { return children.Count > 0; }
		}

		public int Count {
			get { return children.Count; }
		}

		public ObservableCollection<TestResult> Children {
			get { return children; }
		}

		public ObservableCollection<string> Messages {
			get { return messages; }
		}

		public void AddMessage (string format, params object[] args)
		{
			messages.Add (string.Format (format, args));
		}

		public void AddWarnings (IEnumerable<TestWarning> warnings)
		{
			foreach (var warning in warnings)
				children.Add (warning);
		}

		public void AddChild (TestResult result, bool flatten = false)
		{
			var collection = result as TestResultCollection;
			if (collection == null || !flatten || collection.Name != null) {
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

		public override void Accept (ResultVisitor visitor)
		{
			visitor.Visit (this);
		}
	}
}
