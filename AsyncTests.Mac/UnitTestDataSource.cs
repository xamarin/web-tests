//
// MonoMac.CFNetwork.Test.UnitTests.UnitTestDataSource
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
using MonoMac.Foundation;
using MonoMac.CoreText;
using MonoMac.CoreGraphics;
using MonoMac.AppKit;
using Xamarin.AsyncTests.Framework;

namespace Xamarin.AsyncTests.Mac {

	[Register ("UnitTestDataSource")]
	public class UnitTestDataSource : NSOutlineViewDataSource {
		ResultWrapper root;

		public UnitTestDataSource (UnitTestRunnerController controller)
		{
			this.Controller = controller;
		}

		public UnitTestRunnerController Controller {
			get;
			private set;
		}

		internal void SetResult (TestResultCollection result)
		{
			if (result != null)
				root = new ResultWrapper (result);
			else
				root = null;
		}

		#region NSOutlineViewDataSource implementation

		public override int GetChildrenCount (NSOutlineView outlineView, NSObject item)
		{
			ResultWrapper result = item != null ? (ResultWrapper)item : root;
			return result != null ? result.Count : 0;
		}

		public override NSObject GetChild (NSOutlineView outlineView, int childIndex, NSObject ofItem)
		{
			var result = ofItem != null ? (ResultWrapper)ofItem : root;
			return result.GetChild (childIndex);
		}

		public override bool ItemExpandable (NSOutlineView outlineView, NSObject item)
		{
			var result = item as ResultWrapper;
			if (result != null)
				return result.HasChildren;
			else
				return false;
		}

		public override NSObject GetObjectValue (NSOutlineView outlineView, NSTableColumn forTableColumn, NSObject byItem)
		{
			return byItem;
		}

		#endregion
	}
}
