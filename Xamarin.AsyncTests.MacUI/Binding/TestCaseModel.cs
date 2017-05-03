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

		IReadOnlyCollection<TestCaseModel> children;

		TestCaseModel (TestSession session, TestCase test)
		{
			Session = session;
			Test = test;
			fullName = test.Path.FullName;
			serialized = Test.Path.SerializePath ().ToString ();

			RunInitialize ();
		}

		public static async Task<TestCaseModel> Create (TestSession session, TestCase test)
		{
			var model = new TestCaseModel (session, test);
			await model.initTcs.Task.ConfigureAwait (false);
			return model;
		}

		async void RunInitialize ()
		{
			MacUI.Debug ("INIT: {0} {1} {2}\n{3}", Test.Path.FullName, Test.HasChildren, Test.HasParameters, Test.Path.SerializePath ());

			initTcs = new TaskCompletionSource<bool> ();
			if (!Test.HasChildren && !Test.HasParameters) {
				children = new TestCaseModel [0];
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

			var childModels = new List<TestCaseModel> ();

			foreach (var child in list) {
				var model = await Create (Session, child);
				childModels.Add (model);
			}

			lock (this) {
				children = childModels;
			}

			DidChangeValue ("isLeaf");
			DidChangeValue ("childNodes");
		}

		#region implemented abstract members of TestListNode

		protected override IEnumerable<TestListNode> ResolveChildren ()
		{
			return children;
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

