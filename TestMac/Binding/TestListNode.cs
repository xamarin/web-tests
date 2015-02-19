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
using AppKit;
using Foundation;
using Xamarin.AsyncTests;

namespace TestMac
{
	public class TestListNode : NSObject
	{
		public TestListItem Model {
			get { return model; }
		}

		TestListItem model;
		TestListNode[] children;

		TestListNode (TestListItem model, TestListNode[] children)
		{
			this.model = model;
			this.children = children;
		}

		public static TestListNode CreateFromResult (TestResult result)
		{
			var children = new TestListNode [result.Children.Count];
			for (int i = 0; i < children.Length; i++)
				children [i] = TestListNode.CreateFromResult (result.Children [i]);
			var model = new TestResultModel (result, result.Name);
			return new TestListNode (model, children);
		}

		public static TestListNode CreateFromSession (TestSessionModel session)
		{
			return new TestListNode (session, new TestListNode [0]);
		}

		public void AddChild (TestListNode child)
		{
			WillChangeValue ("isLeaf");
			WillChangeValue ("childNodes");
			var newChildren = new TestListNode [children.Length + 1];
			children.CopyTo (newChildren, 0);
			newChildren [newChildren.Length - 1] = child;
			children = newChildren;
			DidChangeValue ("isLeaf");
			DidChangeValue ("childNodes");
		}

		[Export ("isLeaf")]
		public bool IsLeaf {
			get { return children.Length == 0; }
		}

		[Export ("childNodes")]
		public TestListNode[] Children {
			get { return children; }
			set { children = value; }
		}

		[Export("representedObject")]
		public NSObject RepresentedObject {
			get { return Model; }
		}

		public override string ToString ()
		{
			return string.Format ("[TestListNode: IsLeaf={0}, RepresentedObject={1}]", IsLeaf, RepresentedObject);
		}
	}
}

