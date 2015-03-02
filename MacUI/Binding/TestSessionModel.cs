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
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using AppKit;
using Foundation;
using Xamarin.AsyncTests;
using Xamarin.AsyncTests.Framework;

namespace Xamarin.AsyncTests.MacUI
{
	public class TestSessionModel : TestListNode
	{
		public TestSession Session {
			get;
			private set;
		}

		public TestCaseModel Test {
			get;
			private set;
		}

		public TestSessionModel (TestSession session)
		{
			Session = session;

			Initialize ();
		}

		async void Initialize ()
		{
			var test = await Session.GetRootTestCase (CancellationToken.None);
			Console.WriteLine ("GOT ROOT TEST: {0}", test);

			if (test != null) {
				var model = new TestCaseModel (Session, test);
				AddChild (model);
			}
		}

		#region implemented abstract members of TestListNode

		protected override IEnumerable<TestListNode> ResolveChildren ()
		{
			if (Test != null)
				yield return Test;
		}

		#endregion

		#region implemented abstract members of TestListItem

		public override string Name {
			get { return Session.Name; }
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

