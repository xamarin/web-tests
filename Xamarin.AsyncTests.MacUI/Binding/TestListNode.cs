//
// TestListNode.cs
//
// Author:
//       Martin Baulig <martin.baulig@xamarin.com>
//
// Copyright (c) 2015 Xamarin, Inc.
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
using AppKit;
using Foundation;
using Xamarin.AsyncTests;

namespace Xamarin.AsyncTests.MacUI
{
	public abstract class TestListNode : NSObject
	{
		TestListNode parent;
		NSMutableArray children;

		static int nextID;
		public readonly int ID = ++nextID;

		protected abstract IEnumerable<TestListNode> ResolveChildren ();

		public TestListNode Parent {
			get { return parent; }
		}

		public void AddChild (TestListNode child)
		{
			WillChangeValue ("isLeaf");
			WillChangeValue ("childNodes");

			InitializeChildren ();

			children.Add (child);
			child.parent = this;

			DidChangeValue ("isLeaf");
			DidChangeValue ("childNodes");
		}

		public void RemoveChild (TestListNode child)
		{
			WillChangeValue ("isLeaf");
			WillChangeValue ("childNodes");

			InitializeChildren ();

			var length = children.Count;
			for (nuint i = 0; i < length; i++) {
				if (children.GetItem<TestListNode> (i) == child) {
					children.RemoveObject ((nint)i);
					break;
				}
			}

			child.parent = null;

			DidChangeValue ("isLeaf");
			DidChangeValue ("childNodes");
		}

		public void RemoveAllChildren ()
		{
			WillChangeValue ("isLeaf");
			WillChangeValue ("childNodes");

			InitializeChildren ();

			children.RemoveAllObjects ();

			DidChangeValue ("isLeaf");
			DidChangeValue ("childNodes");
		}

		void InitializeChildren ()
		{
			if (children != null)
				return;
			children = new NSMutableArray ();
			foreach (var child in ResolveChildren ()) {
				child.parent = this;
				children.Add (child);
			}
		}

		[Export ("isLeaf")]
		public bool IsLeaf {
			get {
				InitializeChildren ();
				return children.Count == 0;
			}
		}

		[Export ("childNodes")]
		public NSMutableArray Children {
			get {
				InitializeChildren ();
				return children;
			}
			set {
				children = value;
			}
		}

		[Export ("Name")]
		public abstract string Name {
			get;
		}

		[Export ("Status")]
		public abstract TestStatus TestStatus {
			get;
		}

		[Export ("TestParameters")]
		public abstract string TestParameters {
			get;
		}

		[Export ("Error")]
		public abstract NSAttributedString Error {
			get;
		}

		[Export ("TestCase")]
		public abstract TestCaseModel TestCase {
			get;
		}

		public override string ToString ()
		{
			return string.Format ("[TestListNode({0}): IsLeaf={1}, Name={2}, Status={3}]", ID, IsLeaf, Name, TestStatus);
		}
	}
}

