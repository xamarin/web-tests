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
using System.Xml.Linq;
using System.Collections.Generic;
using AppKit;
using Foundation;
using Xamarin.AsyncTests;

namespace TestMac
{
	public class TestCaseModel : TestListNode
	{
		public TestContext Context {
			get;
			private set;
		}

		public TestCase Test {
			get;
			private set;
		}

		string fullName;
		string serialized;

		public TestCaseModel (TestContext ctx, TestCase test)
		{
			Context = ctx;
			Test = test;
			fullName = string.Format ("TEST:{0}:{1}", test, test.Name.FullName);
			serialized = Test.Serialize ().ToString ();
		}

		#region implemented abstract members of TestListNode

		protected override IEnumerable<TestListNode> ResolveChildren ()
		{
			var children = Test.GetChildren (Context);
			foreach (var child in children) {
				yield return new TestCaseModel (Context, child);
			}
			yield break;
		}

		public override string Name {
			get { return fullName; }
		}

		public override TestStatus TestStatus {
			get { return TestStatus.None; }
		}

		public override string TestParameters {
			get { return null; }
		}

		public override NSAttributedString Error {
			get { return new NSAttributedString (serialized); }
		}

		public override TestCaseModel TestCase {
			get { return this; }
		}

		#endregion

		public override string ToString ()
		{
			return string.Format ("[TestCaseModel: Test={0}]", Test);
		}
	}
}

