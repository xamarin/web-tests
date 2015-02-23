//
// TestSessionModel.cs
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
using AppKit;
using Foundation;
using Xamarin.AsyncTests;

namespace TestMac
{
	public class TestSessionModel : TestListNode
	{
		public TestSuite Suite {
			get;
			private set;
		}

		public TestCaseModel Test {
			get;
			private set;
		}

		public TestSessionModel (TestSuite suite)
		{
			Suite = suite;
			Test = new TestCaseModel (suite.Test);
		}

		#region implemented abstract members of TestListNode

		protected override IEnumerable<TestListNode> ResolveChildren ()
		{
			yield return Test;
		}

		#endregion

		#region implemented abstract members of TestListItem

		public override string Name {
			get { return Suite.Name.FullName; }
		}

		public override TestStatus TestStatus {
			get { return TestStatus.None; }
		}

		public override string TestParameters {
			get { return null; }
		}

		public override NSAttributedString Error {
			get { return null; }
		}

		public override TestCaseModel TestCase {
			get { return Test; }
		}

		#endregion
	}
}

