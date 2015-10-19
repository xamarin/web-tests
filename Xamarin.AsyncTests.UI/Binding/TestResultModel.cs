//
// TestResultModel.cs
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
using System.Text;
using System.Linq;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AppKit;
using Foundation;
using Xamarin.AsyncTests;
using Xamarin.AsyncTests.Framework;
using ObjCRuntime;

namespace Xamarin.AsyncTests.MacUI
{
	public class TestResultModel : TestListNode
	{
		public TestSession Session {
			get;
			private set;
		}

		public TestResult Result {
			get;
			private set;
		}

		public TestName TestName {
			get;
			private set;
		}

		readonly string name;

		public TestResultModel (TestSession session, TestResult result, TestName testName = null)
		{
			Session = session;
			Result = result;
			TestName = testName ?? result.Name;

			name = TestName.IsNullOrEmpty (TestName) ? string.Empty : TestName.LocalName;
		}

		protected override IEnumerable<TestListNode> ResolveChildren ()
		{
			return Result.Children.Select (c => new TestResultModel (Session, c));
		}

		public override string Name {
			get { return name; }
		}

		public override TestStatus TestStatus {
			get { return Result != null ? Result.Status : TestStatus.None; }
		}

		public override string TestParameters {
			get { return GetTestParameters (); }
		}

		string GetTestParameters ()
		{
			if (testParameters != null)
				return testParameters;
			if (TestName.IsNullOrEmpty (TestName) || !TestName.HasParameters)
				return null;

			var sb = new StringBuilder ();
			foreach (var parameter in TestName.Parameters) {
				if (sb.Length > 0)
					sb.AppendLine ();
				sb.AppendFormat ("  {0} = {1}", parameter.Name, parameter.Value);
			}

			testParameters = sb.ToString ();
			return testParameters;
		}

		public override NSAttributedString Error {
			get {
				return GetError ();
			}
		}

		NSAttributedString GetError ()
		{
			if (error != null)
				return error;
			if (Result == null || Session == null)
				return null;

			var sb = new StringBuilder ();
			if (Session.App.Settings.Debug_DumpTestPath && Result.Path != null) {
				var serialized = Result.Path.SerializePath ();
				sb.AppendLine (serialized.ToString ());
				sb.AppendLine ();
				sb.AppendLine ();
			}

			if (Result.HasErrors) {
				sb.AppendLine ("One or more errors have occurred:");
				sb.AppendLine ();

				var errors = Result.Errors.ToArray ();
				for (int i = 0; i < errors.Length; i++) {
					sb.AppendFormat ("#{0}): ", i);

					var exception = errors [i];
					var savedException = exception as SavedException;
					if (savedException != null) {
						sb.AppendLine (savedException.Type);
						sb.AppendLine (savedException.Message);
						if (savedException.StackTrace != null)
							sb.AppendLine (savedException.StackTrace);
						else
							sb.AppendLine ();
					} else {
						sb.AppendLine (exception.ToString ());
					}
					sb.AppendLine ();
				}
			}

			if (Result.Messages != null) {
				foreach (var message in Result.Messages) {
					sb.AppendLine (message);
				}
			}

			if (Result.LogEntries != null) {
				foreach (var entry in Result.LogEntries) {
					sb.AppendLine (entry.Text);
				}
			}

			var font = NSFont.FromFontName ("Courier New", 18.0f);
			error = new NSAttributedString (sb.ToString (), font);

			return error;
		}

		public override TestCaseModel TestCase {
			get { return GetTestCase (); }
		}

		TestCaseModel GetTestCase ()
		{
			if (testCase != null)
				return testCase;
			if (Result == null || Result.Path == null || Session == null)
				return null;

			ResolveTestCase ();
			return null;
		}

		async void ResolveTestCase ()
		{
			var serialized = Result.Path.SerializePath ();
			var result = await Session.ResolveFromPath (serialized, CancellationToken.None);

			WillChangeValue ("TestCase");
			testCase = new TestCaseModel (Session, result);
			DidChangeValue ("TestCase");
		}

		TestCaseModel testCase;
		string testParameters;
		NSAttributedString error;
	}
}

