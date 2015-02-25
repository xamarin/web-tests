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
using AppKit;
using Foundation;
using Xamarin.AsyncTests;
using Xamarin.AsyncTests.Framework;
using ObjCRuntime;

namespace TestMac
{
	public class TestResultModel : TestListNode
	{
		public TestContext Context {
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

		public TestResultModel (TestContext ctx, TestResult result, TestName name = null)
		{
			Context = ctx;
			Result = result;
			TestName = name ?? result.Name;
		}

		protected override IEnumerable<TestListNode> ResolveChildren ()
		{
			return Result.Children.Select (c => new TestResultModel (Context, c));
		}

		public override string Name {
			get { return TestName.IsNullOrEmpty (TestName) ? "ROOT" : TestName.FullName; }
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
			if (Result == null)
				return null;

			var sb = new StringBuilder ();
			if (Result.Path != null) {
				var serialized = Result.Path.Serialize ();
				sb.AppendLine (serialized.ToString ());
				sb.AppendLine ();
			}

			var aggregate = Result.Error as AggregateException;
			if (aggregate != null) {
				sb.AppendFormat ("{0}: {1}", aggregate.GetType ().FullName, aggregate.Message);
				sb.AppendLine ();
				sb.AppendLine ();
				sb.AppendLine (aggregate.InnerException.ToString ());
				sb.AppendLine ();
				for (int i = 0; i < aggregate.InnerExceptions.Count; i++) {
					sb.AppendFormat ("Inner exception #{0}):", i + 1);
					sb.AppendLine ();
					sb.AppendLine (aggregate.InnerExceptions [i].ToString ());
					sb.AppendLine ();
				}
			} else if (Result.Error != null) {
				sb.Append (Result.Error.ToString ());
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
			if (Result == null || Result.Test == null || Context == null)
				return null;

			testCase = new TestCaseModel (Context, Result.Test);
			return testCase;
		}

		TestCaseModel testCase;
		string testParameters;
		NSAttributedString error;
	}
}

