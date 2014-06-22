//
// TestResultItem.cs
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
using System.Collections.Generic;

namespace Xamarin.AsyncTests.Framework
{
	public abstract class TestResultItem
	{
		public string Name {
			get;
			protected set;
		}

		List<TestResultItem> children = new List<TestResultItem> ();

		public void AddChild (TestResultItem child)
		{
			children.Add (child);
		}

		public void AddMessage (string format, params object[] args)
		{
			AddChild (new TestResultText (string.Format (format, args)));
		}

		public void AddWarnings (IEnumerable<TestWarning> warnings)
		{
			children.AddRange (warnings);
		}

		public bool HasChildren {
			get { return children.Count > 0; }
		}

		public int Count {
			get { return children.Count; }
		}

		public TestResultItem this [int index] {
			get { return children [index]; }
		}

		public virtual bool HasErrors ()
		{
			foreach (var child in children) {
				if (child.HasErrors ())
					return true;
			}

			return false;
		}

		public abstract void Accept (ResultVisitor visitor);
	}
}

