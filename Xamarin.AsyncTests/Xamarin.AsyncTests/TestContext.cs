//
// TestContext.cs
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
using System.Text;
using System.Collections.Generic;
using System.Threading;

namespace Xamarin.AsyncTests
{
	using Portable;
	using Constraints;

	public sealed class TestContext : ITestConfiguration
	{
		readonly TestContext parent;
		readonly IPortableSupport support;
		readonly TestSuite suite;
		readonly TestResult result;
		readonly TestLogger logger;
		readonly SynchronizationContext syncContext;
		readonly ITestConfiguration config;

		public TestName Name {
			get;
			private set;
		}

		public TestResult Result {
			get { return result ?? parent.Result; }
		}

		public TestSuite Suite {
			get { return parent != null ? parent.Suite : suite; }
		}

		public IPortableSupport PortableSupport {
			get { return support; }
		}

		internal TestContext (IPortableSupport support, TestLogger logger, TestSuite suite, TestName name, TestResult result,
			SynchronizationContext syncContext = null)
		{
			Name = name;
			this.support = support;
			this.config = suite.Framework.Configuration.AsReadOnly ();
			this.logger = logger;
			this.suite = suite;
			this.result = result;
			this.syncContext = syncContext;
		}

		TestContext (TestContext parent, TestName name, TestResult result, SynchronizationContext syncContext = null)
		{
			Name = name;
			this.parent = parent;
			this.support = parent.support;
			this.result = result;
			this.syncContext = syncContext ?? parent.syncContext;

			if (result != null)
				logger = new TestLogger (TestLoggerBackend.CreateForResult (result, parent.logger));
			else
				logger = parent.logger;

			config = parent.config;
		}

		void Invoke (Action action)
		{
			if (syncContext == null)
				action ();
			else
				syncContext.Post (d => action (), null);
		}

		internal TestContext CreateChild (TestName name, TestResult result = null, SynchronizationContext syncContext = null)
		{
			return new TestContext (this, name, result, syncContext);
		}

		#region Statistics

		public void OnTestRunning ()
		{
			Invoke (() => logger.OnTestRunning (Name));
		}

		public void OnTestFinished (TestStatus status)
		{
			Result.Status = status;
			Invoke (() => logger.OnTestFinished (Name, status));
		}

		public void OnTestCanceled ()
		{
			OnTestFinished (TestStatus.Canceled);
		}

		#endregion

		#region Logging

		public void OnError (Exception error)
		{
			if (error is SkipRestOfThisTestException)
				return;

			Invoke (() => {
				logger.OnException (Name, error);
				logger.LogError (error);
			});
		}

		public void LogDebug (int level, string message)
		{
			if (level > logger.LogLevel)
				return;
			Invoke (() => logger.LogDebug (level, message));
		}

		public void LogDebug (int level, string format, params object[] args)
		{
			LogDebug (level, string.Format (format, args));
		}

		public void LogMessage (string message)
		{
			Invoke (() => logger.LogMessage (message));
		}

		public void LogMessage (string message, params object[] args)
		{
			LogMessage (string.Format (message, args));
		}

		public void LogError (Exception error)
		{
			Invoke (() => logger.LogError (error));
		}

		#endregion

		#region Assertions

		internal static string Print (object value)
		{
			if (value == null)
				return "<null>";
			var svalue = value as string;
			if (svalue != null && string.IsNullOrEmpty (svalue))
				return "<empty>";
			if (svalue != null)
				return '"' + svalue + '"';
			else
				return value.ToString ();
		}

		[HideStackFrame]
		public bool Expect (object actual, Constraint constraint)
		{
			return Expect (actual, constraint, false, null);
		}

		[HideStackFrame]
		public bool Expect (object actual, Constraint constraint, string format = null, params object[] args)
		{
			return Expect (actual, constraint, false, format, args);
		}

		[HideStackFrameAttribute]
		public bool Expect (bool value, string format = null)
		{
			return Expect (value, Is.True, false, format);
		}

		[HideStackFrame]
		public bool Expect (bool value, string format = null, params object[] args)
		{
			return Expect (value, Is.True, false, format, args);
		}

		[HideStackFrame]
		public bool Expect (object actual, Constraint constraint, bool fatal = false, string format = null, params object[] args)
		{
			var sb = new StringBuilder ();

			string error;
			if (constraint.Evaluate (actual, out error))
				return true;
			sb.AppendFormat ("AssertionFailed ({0})", constraint.Print ());
			if (format != null) {
				sb.Append (": ");
				if (args != null)
					sb.AppendFormat (format, args);
				else
					sb.Append (format);
			}
			if (error != null) {
				sb.AppendLine ();
				sb.Append (error);
			} else {
				sb.AppendLine ();
				sb.AppendFormat ("Actual value: {0}", Print (actual));
			}

			var exception = new AssertionException (sb.ToString (), GetStackTrace ());
			OnError (exception);
			if (fatal)
				throw new SkipRestOfThisTestException ();
			return false;
		}

		class SkipRestOfThisTestException : Exception
		{
		}

		[HideStackFrame]
		public void Assert (object actual, Constraint constraint)
		{
			Assert (actual, constraint, null);
		}

		[HideStackFrame]
		public void Assert (bool value, string format = null, params object[] args)
		{
			Assert (value, Is.True, format, args);
		}

		[HideStackFrame]
		public void Assert (object actual, Constraint constraint, string format = null, params object[] args)
		{
			Expect (actual, constraint, true, format, args);
		}

		public bool HasPendingException {
			get { return Result.HasErrors; }
		}

		#endregion

		#region Config Snapshot

		public TestCategory CurrentCategory {
			get { return config.CurrentCategory; }
		}

		public bool IsEnabled (TestFeature feature)
		{
			return config.IsEnabled (feature);
		}

		#endregion

		[HideStackFrame]
		public string GetStackTrace ()
		{
			return support.GetStackTrace (false);
		}
	}
}

