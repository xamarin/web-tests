//
// ITestResultModel.cs
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
using System.Linq;
using System.Text;
using System.ComponentModel;
using Xamarin.Forms;

namespace Xamarin.AsyncTests.UI
{
	using Framework;

	public class TestResultModel : INotifyPropertyChanged
	{
		public TestApp App {
			get;
			private set;
		}

		public TestResult Result {
			get;
			private set;
		}

		public TestResultModel (TestApp app, TestResult result)
		{
			App = app;
			Result = result;

			result.PropertyChanged += (sender, e) => OnResultChanged ();
		}

		string summary;
		public string Summary {
			get {
				if (summary == null)
					summary = GetTestSummary (Result);
				return summary;
			}
		}

		void OnResultChanged ()
		{
			summary = null;
			OnPropertyChanged ("Result");
			OnPropertyChanged ("Summary");
		}

		static string GetTestSummary (TestResult result)
		{
			if (!result.HasChildren || !string.IsNullOrEmpty (result.Message))
				return result.Message;

			var numSuccess = result.Children.Count (t => t.Status == TestStatus.Success);
			var numWarnings = result.Children.Count (t => t.Status == TestStatus.Warning);
			var numErrors = result.Children.Count (t => t.Status == TestStatus.Error);

			var sb = new StringBuilder ();
			sb.AppendFormat ("{0} tests passed", numSuccess);
			if (numWarnings > 0)
				sb.AppendFormat (", {0} warnings", numWarnings);
			if (numErrors > 0)
				sb.AppendFormat (", {0} errors", numErrors);
			return sb.ToString ();
		}

		#region INotifyPropertyChanged implementation

		public event PropertyChangedEventHandler PropertyChanged;

		protected virtual void OnPropertyChanged (string propertyName)
		{
			if (PropertyChanged != null)
				PropertyChanged (this, new PropertyChangedEventArgs (propertyName));
		}

		#endregion
	}
}

