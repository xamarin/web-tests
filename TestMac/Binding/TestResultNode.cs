//
// TestResultNode.cs
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
	public class TestResultNode : NSObject
	{
		public TestResult Result {
			get;
			private set;
		}

		public TestResultModel Model {
			get;
			private set;
		}

		TestResultNode[] children;

		public TestResultNode (TestResult result)
		{
			Result = result;
			Model = new TestResultModel (result, result.Name);
		}

		[Export ("isLeaf")]
		public bool IsLeaf {
			get { return !Result.HasChildren; }
		}

		void InitializeChildren ()
		{
			if (children != null)
				return;
			children = new TestResultNode [Result.Children.Count];
			for (int i = 0; i < children.Length; i++)
				children [i] = new TestResultNode (Result.Children [i]);
		}

		[Export ("childNodes")]
		public TestResultNode[] Children {
			get {
				InitializeChildren ();
				return children;
			}
			set {
				Console.WriteLine ("TRN ATTEMPT TO SET CHILD NODES: {0}", value);
				children = value;
			}
		}

		#if FIXME
		[Export("copyWithZone:")]
		public NSObject CopyWithZone (IntPtr zone)
		{
			var cloned = new TestResultNode (Result);
			cloned.DangerousRetain ();
			return cloned;
		}
		#endif

		public override NSObject ValueForKey (NSString key)
		{
			return base.ValueForKey (key);
		}

		public override NSObject ValueForKeyPath (NSString keyPath)
		{
			return base.ValueForKeyPath (keyPath);
		}

		public override NSObject ValueForUndefinedKey (NSString key)
		{
			return base.ValueForUndefinedKey (key);
		}

		[Export("representedObject")]
		public NSObject RepresentedObject {
			get { return Model; }
		}

		public override string ToString ()
		{
			return string.Format ("[TestResultNode: IsLeaf={0}, RepresentedObject={1}]", IsLeaf, RepresentedObject);
		}
	}
}

