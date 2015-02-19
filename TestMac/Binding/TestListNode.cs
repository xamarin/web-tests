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
	public abstract class TestListNode : NSObject
	{
		TestListNode[] children;

		public static TestListNode CreateFromResult (TestResult result)
		{
			return new TestResultModel (result, result.Name);
		}

		public static TestListNode CreateFromSession (TestSessionModel session)
		{
			return session;
		}

		protected abstract TestListNode[] ResolveChildren ();

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

		TestListNode[] GetChildren ()
		{
			if (children == null)
				children = ResolveChildren ();
			return children;
		}

		[Export ("isLeaf")]
		public bool IsLeaf {
			get { return GetChildren ().Length == 0; }
		}

		[Export ("childNodes")]
		public TestListNode[] Children {
			get { return GetChildren (); }
			set { children = value; }
		}

		[Export("representedObject")]
		public NSObject RepresentedObject {
			get { return this; }
		}

		public override string ToString ()
		{
			return string.Format ("[TestListNode: IsLeaf={0}, RepresentedObject={1}]", IsLeaf, RepresentedObject);
		}
	}
}

