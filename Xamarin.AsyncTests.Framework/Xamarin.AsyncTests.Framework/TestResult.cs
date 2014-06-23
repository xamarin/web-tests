//
// Xamarin.AsyncTests.Framework.TestResultItem
//
// Authors:
//      Martin Baulig (martin.baulig@gmail.com)
//
// Copyright 2012 Xamarin Inc. (http://www.xamarin.com)
//
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//
using System;
using System.ComponentModel;
using System.Collections.Generic;

namespace Xamarin.AsyncTests.Framework
{
	public class TestResult : INotifyPropertyChanged
	{
		TestName name;
		TestStatus status = TestStatus.Ignored;
		string message;
		Exception error;

		public TestName Name {
			get { return name; }
			protected set {
				name = value;
				OnPropertyChanged ("Name");
			}
		}

		public TestStatus Status {
			get { return status; }
			protected set {
				if (value == status)
					return;
				status = value;
				OnPropertyChanged ("Status");
			}
		}

		public string Message {
			get { return message; }
			set {
				message = value;
				OnPropertyChanged ("Message");
			}
		}

		public Exception Error {
			get { return error; }
			set {
				error = value;
				if (error != null)
					Status = TestStatus.Error;
				OnPropertyChanged ("Error");
			}
		}

		public TestResult (TestName name, TestStatus status, string message = null)
		{
			this.name = name;
			this.status = status;
			this.message = message;
		}

		public TestResult (TestName name, Exception error, string message = null)
		{
			this.name = name;
			this.status = TestStatus.Error;
			this.error = error;
			this.message = message;
		}

		public event PropertyChangedEventHandler PropertyChanged;

		protected virtual void OnPropertyChanged (string propertyName)
		{
			if (PropertyChanged != null)
				PropertyChanged (this, new PropertyChangedEventArgs (propertyName));
		}

		public override string ToString ()
		{
			return string.Format ("[TestResult: Name={0}, Status={1}, Message={2}, Error={3}]", Name, Status, Message, Error);
		}
	}
}
