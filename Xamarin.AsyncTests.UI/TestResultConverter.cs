//
// TestResultConverter.cs
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
using System.Reflection;
using System.Globalization;
using Xamarin.Forms;

namespace Xamarin.AsyncTests.UI
{
	using Framework;

	public class TestResultConverter : IValueConverter
	{
		#region IValueConverter implementation

		public object Convert (object value, Type targetType, object parameter, CultureInfo culture)
		{
			if (parameter != null) {
				switch ((string)parameter) {
				case "not-null":
					return value != null;
				default:
					throw new InvalidOperationException ();
				}
			}

			if (value is TestStatus) {
				if (targetType.Equals (typeof(Color))) {
					switch ((TestStatus)value) {
					case TestStatus.Warning:
						return Color.Yellow;
					case TestStatus.Error:
						return Color.Red;
					case TestStatus.Success:
						return Color.Green;
					default:
						return Color.Black;
					}
				} else if (targetType.Equals (typeof(string))) {
					return value.ToString ();
				}
			} else if (value is TestName) {
				var name = (TestName)value;
				if (string.IsNullOrEmpty (name.FullName))
					return "<null>";
				return name.FullName;
			} else if (value is Exception) {
				if (!targetType.Equals (typeof(string)))
					throw new InvalidOperationException ();
				return value.ToString ();
			} else if (targetType.Equals (typeof(string))) {
				return value != null ? value.ToString () : "<null>";
			}

			throw new InvalidOperationException ();
		}

		public object ConvertBack (object value, Type targetType, object parameter, CultureInfo culture)
		{
			throw new NotImplementedException ();
		}

		#endregion
	}
}

