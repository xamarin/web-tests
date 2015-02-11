//
// TestBuilderDataSource.cs
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
using AppKit;
using Foundation;
using Xamarin.AsyncTests;
using Xamarin.AsyncTests.Framework;

namespace TestMac
{
	public class TestBuilderDataSource : NSOutlineViewDataSource
	{
		public MacUI App {
			get;
			private set;
		}

		public NSOutlineView View {
			get;
			private set;
		}

		TestBuilderModel root;

		public TestBuilderDataSource (NSOutlineView view, MacUI app)
		{
			App = app;
			View = view;

			app.ServerManager.TestSuite.PropertyChanged += (sender, e) => {
				view.InvokeOnMainThread (() => LoadTestSuite (e));
			};
		}

		void LoadTestSuite (TestSuite suite)
		{
			if (suite == null)
				root = null;
			else
				root = new TestBuilderModel (suite);

			View.ReloadData ();
		}

		public override NSObject GetChild (NSOutlineView outlineView, nint childIndex, NSObject item)
		{
			if (item == null) {
				if (childIndex != 0)
					throw new InvalidOperationException ();
				return root;
			}

			var model = (TestBuilderModel)item;
			return model.GetChild ((int)childIndex);
		}

		public override nint GetChildrenCount (NSOutlineView outlineView, NSObject item)
		{
			if (item == null)
				return App.ServerManager.TestSuite.HasValue ? 1 : 0;

			var model = (TestBuilderModel)item;
			return model.Builder.CountChildren;
		}

		public override bool ItemExpandable (NSOutlineView outlineView, NSObject item)
		{
			var model = (TestBuilderModel)item;
			return model.Builder.CountChildren > 0;
		}

		public override NSObject GetObjectValue (NSOutlineView outlineView, NSTableColumn tableColumn, NSObject item)
		{
			var model = (TestBuilderModel)item;
			return (NSString)model.Name.FullName;
		}

		public override NSObject ValueForKey (NSString key)
		{
			return base.ValueForKey (key);
		}
	}
}

