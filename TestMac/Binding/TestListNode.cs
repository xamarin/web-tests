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

namespace TestMac
{
	public abstract class TestListNode : NSObject
	{
		NSMutableArray children;

		protected abstract IEnumerable<TestListNode> ResolveChildren ();

		public void AddChild (TestListNode child)
		{
			WillChangeValue ("isLeaf");
			WillChangeValue ("childNodes");

			children.Add (child);

			DidChangeValue ("isLeaf");
			DidChangeValue ("childNodes");
		}

		void InitializeChildren ()
		{
			if (children != null)
				return;
			children = new NSMutableArray ();
			children.AddObjects (ResolveChildren ().ToArray ());
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

		public bool IsRoot {
			get; set;
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
			return string.Format ("[TestListNode: IsLeaf={0}, Name={1}, Status={2}]", IsLeaf, Name, TestStatus);
		}
	}
}

