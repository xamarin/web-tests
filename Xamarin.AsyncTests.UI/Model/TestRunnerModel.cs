//
// TestRunnerModel.cs
//
// Author:
//       Martin Baulig <martin.baulig@xamarin.com>
//
// Copyright (c) 2014 Xamarin Inc. (http://www.xamarin.com)
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
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace Xamarin.AsyncTests.UI
{
	using Framework;

	public class TestRunnerModel : BindableObject
	{
		public TestApp App {
			get;
			private set;
		}

		public TestResultModel ResultModel {
			get;
			private set;
		}

		public TestCase Test {
			get { return ResultModel.Result.Test; }
		}

		public bool IsRoot {
			get;
			private set;
		}

		public TestRunnerModel (TestApp app, TestResultModel model, bool isRoot)
		{
			App = app;
			ResultModel = model;
			IsRoot = isRoot;
		}

		static int countReruns;

		public Task Run (CancellationToken cancellationToken)
		{
			var test = Test;
			var result = ResultModel.Result;

			if (!IsRoot) {
				var name = new TestNameBuilder ();
				name.PushName ("UI-Rerun");
				name.PushParameter ("$uiTriggeredRerun", ++countReruns);

				test = TestSuite.CreateProxy (test, name.GetName ());
				result = new TestResult (name.GetName ());
				ResultModel.Result.AddChild (result);
			} else {
				ResultModel.Result.Clear ();
			}

			return test.Run (App.Context, result, cancellationToken);
		}
	}
}

