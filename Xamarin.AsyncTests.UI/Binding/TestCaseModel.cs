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
using System.Threading;
using System.Threading.Tasks;
using AppKit;
using Foundation;
using Xamarin.AsyncTests;
using Xamarin.AsyncTests.Framework;

namespace Xamarin.AsyncTests.MacUI
{
	public class TestCaseModel : TestListNode
	{
		public TestSession Session {
			get;
			private set;
		}

		public TestCase Test {
			get;
			private set;
		}

		string fullName;
		string serialized;
		TaskCompletionSource<bool> initTcs;

		IReadOnlyCollection<TestCase> children;

		public TestCaseModel (TestSession session, TestCase test)
		{
			Session = session;
			Test = test;
			fullName = test.Name.FullName;
			serialized = Test.Path.SerializePath ().ToString ();

			RunInitialize ();
		}

		public Task<bool> Initialize ()
		{
			return initTcs.Task;
		}

		async void RunInitialize ()
		{
			initTcs = new TaskCompletionSource<bool> ();
			if (!Test.HasChildren && !Test.HasParameters) {
				initTcs.SetResult (false);
				return;
			}

			try {
				await DoInitialize ();
				initTcs.SetResult (children.Count > 0);
			} catch (Exception ex) {
				initTcs.SetException (ex);
			}
		}

		async Task DoInitialize ()
		{
			WillChangeValue ("isLeaf");
			WillChangeValue ("childNodes");

			List<TestCase> list = new List<TestCase> ();

			if (Test.HasParameters) {
				var parameterResult = await Session.GetTestParameters (Test, CancellationToken.None);
				list.AddRange (parameterResult);
			}

			if (Test.HasChildren) {
				var childrenResult = await Test.GetChildren (CancellationToken.None);
				list.AddRange (childrenResult);
			}

			lock (this) {
				children = list;
			}

			DidChangeValue ("isLeaf");
			DidChangeValue ("childNodes");
		}

		#region implemented abstract members of TestListNode

		protected override IEnumerable<TestListNode> ResolveChildren ()
		{
			lock (this) {
				if (children == null)
					yield break;

				foreach (var child in children) {
					yield return new TestCaseModel (Session, child);
				}
			}
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
			get {
				var font = NSFont.FromFontName ("Courier New", 18.0f);
				return new NSAttributedString (serialized, font);
			}
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

