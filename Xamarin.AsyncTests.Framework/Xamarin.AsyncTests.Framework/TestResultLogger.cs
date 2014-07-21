//
// MessageLogger.cs
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

namespace Xamarin.AsyncTests.Framework
{
	class TestResultLogger : ITestLogger
	{
		public TestResult Result {
			get;
			private set;
		}

		public ITestLogger Parent {
			get;
			private set;
		}

		public TestResultLogger (TestResult result, ITestLogger parent = null)
		{
			Result = result;
			Parent = parent;
		}

		#region ITestLogger implementation

		public void LogDebug (int level, string message)
		{
			if (Parent != null)
				Parent.LogDebug (level, message);
			LogMessage (message);
		}

		public void LogDebug (int level, string format, params object[] args)
		{
			LogDebug (level, string.Format (format, args));
		}

		public void LogMessage (string message)
		{
			if (Parent != null)
				Parent.LogMessage (message);

			Result.AddMessage (message);
		}

		public void LogMessage (string format, params object[] args)
		{
			LogMessage (string.Format (format, args));
		}

		public void LogError (Exception error)
		{
			Result.AddError (error);
		}

		#endregion
	}
}

