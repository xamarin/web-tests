//
// Xamarin.AsyncTests.Framework.TestContext
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
using System.Text;
using System.Linq;
using SD = System.Diagnostics;
using System.Reflection;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Xamarin.AsyncTests
{
	public class TestContext : IDisposable {
		int debugLevel = DefaultDebugLevel;
		List<IDisposable> disposables;
		TestNameBuilder currentTestName = new TestNameBuilder ();
		TestConfiguration configuration = new TestConfiguration ();

		const int DefaultDebugLevel = 10;

		public int DebugLevel {
			get { return debugLevel; }
			set { debugLevel = value; }
		}

		public void Debug (int level, string format, params object[] args)
		{
			Debug (level, string.Format (format, args));
		}

		public void Debug (int level, string message)
		{
			if (level <= DebugLevel)
				SD.Debug.WriteLine (message);
		}

		public void Log (string format, params object[] args)
		{
			Log (string.Format (format, args));
		}

		public void Log (string message)
		{
			SD.Debug.WriteLine (message);
			if (CurrentTestResult != null)
				CurrentTestResult.AddMessage (message);
		}

		public string Print (object obj)
		{
			return obj != null ? obj.ToString () : "<null>";
		}

		public TestResult CurrentTestResult {
			get;
			internal set;
		}

		internal TestNameBuilder CurrentTestName {
			get { return currentTestName; }
			set { currentTestName = value; }
		}

		public TestName GetCurrentTestName ()
		{
			return CurrentTestName.GetName ();
		}

		public TestConfiguration Configuration {
			get { return configuration; }
		}

		#region Statistics

		int countTests;
		int countSuccess;
		int countErrors;

		public int CountTests {
			get { return countTests; }
		}

		public int CountSuccess {
			get { return countSuccess; }
		}

		public int CountErrors {
			get { return countErrors; }
		}

		public virtual void ResetStatistics ()
		{
			countTests = countSuccess = countErrors = 0;
		}

		public virtual void OnTestRunning (TestName name)
		{
			countTests++;
		}

		public virtual void OnTestPassed (TestResult result)
		{
			countSuccess++;
			OnTestFinished (result);
		}

		public virtual void OnTestError (TestResult result)
		{
			countErrors++;
			OnTestFinished (result);
		}

		protected virtual void OnTestFinished (TestResult result)
		{
			if (TestFinishedEvent != null)
				TestFinishedEvent (this, result);
		}

		public event EventHandler<TestResult> TestFinishedEvent;

		#endregion

		#region Disposing

		public void AutoDispose (IDisposable disposable)
		{
			if (disposable == null)
				return;
			if (disposables == null)
				disposables = new List<IDisposable> ();
			disposables.Add (disposable);
		}

		public void AutoDispose ()
		{
			if (disposables == null)
				return;
			foreach (var disposable in disposables) {
				try {
					disposable.Dispose ();
				} catch (Exception ex) {
					Log ("Auto-dispose failed: {0}", ex);
				}
			}
			disposables = null;
		}

		~TestContext ()
		{
			Dispose (false);
		}
		
		public void Dispose ()
		{
			Dispose (true);
			GC.SuppressFinalize (this);
		}

		void Dispose (bool disposing)
		{
			AutoDispose ();
		}

		#endregion
	}
}
