//
// TestLoggerBackend.cs
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
	public abstract class TestLoggerBackend
	{
		protected internal abstract void OnLogEvent (LogEntry entry);

		protected internal abstract void OnStatisticsEvent (StatisticsEventArgs args);

		public enum EntryKind {
			Debug,
			Message,
			Warning,
			Error
		}

		public sealed class LogEntry
		{
			public EntryKind Kind {
				get;
			}

			public int LogLevel {
				get;
			}

			public string Text {
				get;
			}

			public Exception Error {
				get;
			}

			internal bool AddedToResult;

			public LogEntry (EntryKind kind, int level, string text, Exception error = null)
			{
				Kind = kind;
				LogLevel = level;
				Text = text;
				Error = error;
			}

			public override string ToString ()
			{
				return string.Format ("[LogEntry: Kind={0}, LogLevel={1}, Text={2}]", Kind, LogLevel, Text);
			}
		}

		public enum StatisticsEventType {
			Reset,
			Running,
			Finished
		}

		public class StatisticsEventArgs : EventArgs
		{
			public StatisticsEventType Type {
				get; set;
			}

			public string Name {
				get; set;
			}

			public TestStatus Status {
				get; set;
			}

			public bool IsRemote {
				get;
				internal set;
			}

			public override string ToString ()
			{
				return string.Format ("[StatisticsEventArgs: Type={0}, Name={1}, Status={2}]", Type, Name, Status);
			}
		}

		public static TestLoggerBackend CreateForResult (TestResult result, TestLogger parent)
		{
			return new TestResultBackend (result, parent != null ? parent.Backend : null);
		}

		public TestLoggerBackend CreateSynchronized ()
		{
			var synchronized = this as SynchronizedBackend;
			if (synchronized != null)
				return synchronized;
			return new SynchronizedBackend (this);
		}

		class TestResultBackend : TestLoggerBackend
		{
			public TestResult Result {
				get;
			}

			public TestLoggerBackend Parent {
				get;
			}

			public TestResultBackend (TestResult result, TestLoggerBackend parent = null)
			{
				Result = result;
				Parent = parent;
			}

			protected internal override void OnLogEvent (LogEntry entry)
			{
				if (!entry.AddedToResult) {
					if (entry.Kind == EntryKind.Error) {
						if (entry.Error != null)
							Result.AddError (entry.Error);
						else
							Result.AddError (new AssertionException (entry.Text, null));
					} else if (entry.Kind == EntryKind.Message) {
						Result.AddMessage (entry.Text);
					} else {
						Result.AddLogMessage (entry);
					}

					entry.AddedToResult = true;
				}

				if (Parent != null)
					Parent.OnLogEvent (entry);
			}

			protected internal override void OnStatisticsEvent (StatisticsEventArgs args)
			{
				if (Parent != null)
					Parent.OnStatisticsEvent (args);
			}
		}

		class SynchronizedBackend : TestLoggerBackend
		{
			public TestLoggerBackend Backend {
				get;
			}

			readonly object locker = new object ();

			public SynchronizedBackend (TestLoggerBackend backend)
			{
				Backend = backend;
			}

			protected internal override void OnLogEvent (LogEntry entry)
			{
				lock (locker) {
					Backend.OnLogEvent (entry);
				}
			}

			protected internal override void OnStatisticsEvent (StatisticsEventArgs args)
			{
				lock (locker) {
					Backend.OnStatisticsEvent (args);
				}
			}
		}
	}
}

