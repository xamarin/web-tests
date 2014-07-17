//
// TestStatistics.cs
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

namespace Xamarin.AsyncTests
{
	public class TestStatistics
	{
		int countTests;
		int countSuccess;
		int countErrors;
		int countIgnored;

		public int CountTests {
			get { return countTests; }
		}

		public int CountSuccess {
			get { return countSuccess; }
		}

		public int CountErrors {
			get { return countErrors; }
		}

		public int CountIgnored {
			get { return countIgnored; }
		}

		public void Reset ()
		{
			countTests = countSuccess = countErrors = countIgnored = 0;
			OnStatisticsEvent (EventType.Reset);
		}

		public void OnTestRunning (TestName name)
		{
			++countTests;
			OnStatisticsEvent (EventType.Running, name);
		}

		public void OnTestFinished (TestName name, TestStatus status)
		{
			switch (status) {
			case TestStatus.Success:
				++countSuccess;
				break;
			case TestStatus.Ignored:
			case TestStatus.None:
				++countIgnored;
				break;
			default:
				++countErrors;
				break;
			}

			OnStatisticsEvent (EventType.Finished, name, status);
		}

		protected virtual void OnStatisticsEvent (EventType type, TestName name = null, TestStatus status = TestStatus.None)
		{
			if (StatisticsEvent == null)
				return;
			StatisticsEvent (this, new StatisticsEventArgs { Type = type, Name = name, Status = status });
		}

		public event EventHandler<StatisticsEventArgs> StatisticsEvent;

		public enum EventType {
			Reset,
			Running,
			Finished
		}

		public class StatisticsEventArgs : EventArgs
		{
			public EventType Type {
				get; set;
			}

			public TestName Name {
				get; set;
			}

			public TestStatus Status {
				get; set;
			}
		}
	}
}

