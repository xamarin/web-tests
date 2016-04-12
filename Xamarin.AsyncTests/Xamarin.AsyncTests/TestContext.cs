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
		readonly TestResult result;
		readonly TestLogger logger;
		readonly SettingsBag settings;
		readonly ITestConfiguration config;
		readonly ITestPathInternal path;
		bool isCanceled;

		public TestName Name {
			get;
			private set;
		}

		public TestResult Result {
			get { return result ?? parent.Result; }
		}

		public SettingsBag Settings {
			get { return settings ?? parent.Settings; }
		}

		internal ITestPathInternal Path {
			get { return path ?? parent.Path; }
		}

		internal TestContext (SettingsBag settings, TestLogger logger, ITestConfiguration config, TestName name)
		{
			Name = name;
			this.settings = settings;
			this.config = config;
			this.logger = logger;
		}

		TestContext (TestContext parent, TestName name, ITestPathInternal path, TestResult result)
		{
			Name = name;
			this.parent = parent;
			this.path = path;
			this.result = result;

			if (result != null)
				logger = new TestLogger (TestLoggerBackend.CreateForResult (result, parent.logger));
			else
				logger = parent.logger;

			config = parent.config;
		}

		internal TestContext CreateChild (TestName name, ITestPathInternal path, TestResult result = null)
		{
			return new TestContext (this, name, path, result);
		}

		#region Statistics

		public void OnTestRunning ()
		{
			logger.OnTestRunning (Name);
		}

		public void OnTestFinished (TestStatus status)
		{
			Result.Status = status;
			logger.OnTestFinished (Name, status);
		}

		public void OnTestCanceled ()
		{
			isCanceled = true;
			OnTestFinished (TestStatus.Canceled);
		}

		public void OnTestIgnored ()
		{
			if (Result.Status == TestStatus.None)
				Result.Status = TestStatus.Ignored;
		}

		#endregion

		#region Logging

		public void OnError (Exception error)
		{
			if (error is SkipRestOfThisTestException)
				return;

			logger.OnException (Name, error);
		}

		public void LogDebug (int level, string message)
		{
			var logLevel = Settings.LogLevel;
			if (logLevel >= 0 && level > logLevel)
				return;
			logger.LogDebug (level, message);
		}

		public void LogDebug (int level, string format, params object[] args)
		{
			LogDebug (level, string.Format (format, args));
		}

		public void LogMessage (string message)
		{
			logger.LogMessage (message);
		}

		public void LogMessage (string message, params object[] args)
		{
			LogMessage (string.Format (message, args));
		}

		public void LogError (string message, Exception error)
		{
			logger.LogError (message, error);
		}

		public void LogError (Exception error)
		{
			logger.LogError (error);
		}

		public void LogBuffer (string message, byte[] buffer)
		{
			LogBuffer (message, buffer, 0, buffer.Length);
		}

		public void LogBuffer (string message, byte[] buffer, int index, int length)
		{
			LogMessage ("{0} (0x{1:x4} bytes)", message, length);

			for (int i = index; i < index + length; i += 16) {
				int count = (index + length - i) >= 16 ? 16 : (index + length - i);
				string buf = string.Empty;
				string text = string.Empty;
				for (int j = 0; j < count; j++) {
					if (j == 8)
						buf += " - ";
					else if (j > 0)
						buf += " ";
					byte ch = buffer[i + j];
					buf += ch.ToString ("x2");
					text += ch >= 32 && ch < 127 ? (char)ch : '.';
				}
				LogMessage ("    {0:x4}  {1}  {2}", i, buf, text);
			}
		}

		public void LogBufferAsCSharp (string name, string indent, byte[] buffer)
		{
			LogBufferAsCSharp (name, indent, buffer, 0, buffer.Length);
		}

		public void LogBufferAsCSharp (string name, string indent, byte[] buffer, int offset, int size)
		{
			var sb = new StringBuilder ();
			sb.AppendFormat ("{0}internal static readonly byte[] {1} = new byte[] {{\n", indent, name);
			for (int i = 0; i < size; i++) {
				if ((i % 16) == 0)
					sb.Append (indent + "\t");
				sb.AppendFormat ("0x{0:x2}", buffer[offset + i]);
				if (i + 1 >= size)
					break;
				sb.Append (",");
				if (((i + 1) % 16) == 0)
					sb.AppendLine ();
				else
					sb.Append (" ");
			}
			sb.AppendLine ();
			sb.AppendFormat (indent + "}};\n");
			LogMessage (sb.ToString ());
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

		[HideStackFrame]
		public void IgnoreThisTest ()
		{
			OnTestIgnored ();
			throw new SkipRestOfThisTestException ();
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

		[HideStackFrame]
		public void AssertFail (string message)
		{
			var exception = new AssertionException (message, GetStackTrace ());
			OnError (exception);
			throw new SkipRestOfThisTestException ();
		}

		[HideStackFrame]
		public void AssertFail (string format, params object[] args)
		{
			AssertFail (string.Format (format, args));
		}

		public bool HasPendingException {
			get { return Result != null && Result.HasErrors; }
		}

		public bool IsCanceled {
			get {
				if (isCanceled)
					return true;
				else if (parent != null)
					return parent.IsCanceled;
				else
					return false;
			}
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
			var support = DependencyInjector.Get<IPortableSupport> ();
			return support.GetStackTrace (false);
		}

		public T GetParameter<T> (string name = null)
		{
			T value;
			if (TryGetParameter<T> (out value, name))
				return value;

			if (name != null)
				AssertFail (string.Format ("No such parameter '{0}'.", name));
			else
				AssertFail (string.Format ("No parameter of type '{0}'.", typeof (T)));
			throw new SkipRestOfThisTestException ();
		}

		public object GetFixtureInstance ()
		{
			object value;
			if (TryGetParameter<object> (out value, "instance"))
				return value;

			AssertFail ("Unable to get fixture instance.");
			throw new SkipRestOfThisTestException ();
		}

		public bool TryGetParameter<T> (out T value, string name = null)
		{
			var path = Path;
			if (path == null) {
				AssertFail ("Should never happen!");
				throw new SkipRestOfThisTestException ();
			}

			while (path != null) {
				if (path.ParameterMatches<T> (name)) {
					value = path.GetParameter<T> ();
					return true;
				}

				path = path.Parent;
			}

			value = default(T);
			return false;
		}

		static int nextPort;

		public int GetUniquePort ()
		{
			if (parent != null)
				return parent.GetUniquePort ();
			return 9000 + (++nextPort);
		}
	}
}

