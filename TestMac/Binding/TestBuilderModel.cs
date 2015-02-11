//
// TestCaseModel.cs
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
using System.Collections.Generic;
using Foundation;
using Xamarin.AsyncTests;
using Xamarin.AsyncTests.Framework;

namespace TestMac
{
	public class TestBuilderModel : NSObject
	{
		public ITestBuilder Builder {
			get;
			private set;
		}

		public TestName Name {
			get;
			private set;
		}

		public TestBuilderModel (TestSuite suite)
		{
			Builder = suite.TestBuilder;
			Name = suite.Name;
		}

		TestBuilderModel (ITestBuilder builder)
		{
			Builder = builder;
			Name = builder.Name;
		}

		List<TestBuilderModel> children;

		public int CountChildren {
			get { return Builder.CountChildren; }
		}

		public TestBuilderModel GetChild (int index)
		{
			InitializeChildren ();
			return children [index];
		}

		void InitializeChildren ()
		{
			if (children != null)
				return;

			children = new List<TestBuilderModel> (Builder.CountChildren);
			for (int i = 0; i < Builder.CountChildren; i++) {
				var child = Builder.GetChild (i);
				var childModel = new TestBuilderModel (child);
				children.Add (childModel);
			}
		}
	}
}

