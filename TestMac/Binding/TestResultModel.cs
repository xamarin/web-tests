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
using System.Collections.Generic;
using AppKit;
using Foundation;
using Xamarin.AsyncTests;
using Xamarin.AsyncTests.Framework;
using ObjCRuntime;

namespace TestMac
{
	public class TestResultModel : NSObject
	{
		public TestResult Result {
			get;
			private set;
		}

		public TestName TestName {
			get;
			private set;
		}

		public TestResultModel (TestResult result, TestName name)
		{
			Result = result;
			TestName = name;
		}

		#if FIXME
		[Export("copyWithZone:")]
		public NSObject CopyWithZone (IntPtr zone)
		{
			var cloned = new TestResultModel ();
			cloned.DangerousRetain ();
			return cloned;
		}
		#endif

		public override NSObject ValueForUndefinedKey (NSString key)
		{
			return base.ValueForUndefinedKey (key);
		}

		[Export ("Name")]
		public string Name {
			get { return TestName.IsNullOrEmpty (TestName) ? "ROOT" : TestName.FullName; }
		}

		[Export ("Status")]
		public TestStatus TestStatus {
			get { return Result != null ? Result.Status : TestStatus.None; }
		}

		[Export ("TestParameters")]
		public string TestParameters {
			get { return GetTestParameters (); }
		}

		[Export ("HasParameters")]
		public bool HasParameters {
			get { return !TestName.IsNullOrEmpty (TestName) && TestName.HasParameters; }
		}

		string GetTestParameters ()
		{
			if (testParameters != null)
				return testParameters;
			if (TestName.IsNullOrEmpty (TestName) || !TestName.HasParameters) {
				testParameters = string.Empty;
				return testParameters;
			}

			Console.WriteLine ("GET PARAMETERS: {0}", TestName.Parameters.Length);
			var sb = new StringBuilder ();
			foreach (var parameter in TestName.Parameters) {
				if (sb.Length > 0)
					sb.AppendLine ();
				sb.AppendFormat ("  {0} = {1}", parameter.Name, parameter.Value);
			}

			testParameters = sb.ToString ();
			return testParameters;
		}

		List<TestResultModel> children;
		string testParameters;

		public int CountChildren {
			get {
				InitializeChildren ();
				return children.Count;
			}
		}

		public TestResultModel GetChild (int index)
		{
			InitializeChildren ();
			return children [index];
		}

		void InitializeChildren ()
		{
			if (children != null)
				return;

			children = new List<TestResultModel> ();
			foreach (var child in Result.Children) {
				var childModel = new TestResultModel (child, child.Name);
				children.Add (childModel);
			}
		}
	}
}

