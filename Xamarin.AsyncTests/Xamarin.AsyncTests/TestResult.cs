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
using System.Linq;
using System.ComponentModel;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Xamarin.AsyncTests
{
	public class TestResult : INotifyPropertyChanged
	{
		TestStatus status = TestStatus.None;
		readonly TestPath path;
		TestResult parent;
		TimeSpan? elapsedTime;

		public TestStatus Status {
			get { return status; }
			set {
				lock (this) {
					if (value == status)
						return;
					WantToModify ();
					status = value;
				}
				OnPropertyChanged ("Status");
			}
		}

		public TimeSpan? ElapsedTime {
			get { return elapsedTime; }
			set {
				lock (this) {
					WantToModify ();
					elapsedTime = value;
				}
				OnPropertyChanged ("ElapsedTime");
			}
		}

		public TestPath Path {
			get { return path; }
		}

		internal TestResult (TestPath path, Exception error)
			: this (path, TestStatus.Error)
		{
			AddError (error);
		}

		internal TestResult (TestPath path, TestStatus status = TestStatus.None)
		{
			this.path = path;
			this.status = status;

			messages = new ObservableCollection<string> ();
			((INotifyPropertyChanged)messages).PropertyChanged += (sender, e) => OnMessagesChanged ();
			children = new ObservableCollection<TestResult> ();
			((INotifyPropertyChanged)children).PropertyChanged += (sender, e) => OnChildrenChanged ();
			errors = new ObservableCollection<Exception> ();
			((INotifyPropertyChanged)errors).PropertyChanged += (sender, e) => OnErrorsChanged ();
			log = new ObservableCollection<TestLoggerBackend.LogEntry> ();
			((INotifyPropertyChanged)log).PropertyChanged += (sender, e) => OnLogEntriesChanged ();
		}

		ObservableCollection<TestResult> children;
		ObservableCollection<string> messages;
		ObservableCollection<TestLoggerBackend.LogEntry> log;
		ObservableCollection<Exception> errors;

		public bool HasChildren {
			get { return children.Count > 0; }
		}

		public IReadOnlyList<TestResult> Children {
			get { return children; }
		}

		public bool HasMessages {
			get { return messages != null && messages.Count > 0; }
		}

		public IReadOnlyCollection<string> Messages {
			get { return messages; }
		}

		public bool HasLogEntries {
			get { return log != null && log.Count > 0; }
		}

		public IReadOnlyCollection<TestLoggerBackend.LogEntry> LogEntries {
			get { return log; }
		}

		public bool HasErrors {
			get { return errors.Count > 0; }
		}

		public IReadOnlyCollection<Exception> Errors {
			get { return errors; }
		}

		void OnMessagesChanged ()
		{
			OnPropertyChanged ("Messages");
		}

		void OnErrorsChanged ()
		{
			OnPropertyChanged ("Errors");
		}

		void OnLogEntriesChanged ()
		{
			OnPropertyChanged ("LogEntries");
		}

		void OnChildrenChanged ()
		{
			OnPropertyChanged ("Children");
			OnPropertyChanged ("HasChildren");
		}

		public void AddMessage (string message)
		{
			lock (this) {
				WantToModify ();
				messages.Add (message);
			}
		}

		public void AddMessage (string format, params object[] args)
		{
			lock (this) {
				WantToModify ();
				messages.Add (string.Format (format, args));
			}
		}

		public void AddLogMessage (TestLoggerBackend.LogEntry entry)
		{
			lock (this) {
				WantToModify ();
				log.Add (entry);
			}
		}

		public void AddChild (TestResult child)
		{
			lock (this) {
				WantToModify ();
				child.parent = this;
				children.Add (child);
				MergeStatus (child.Status);
			}
		}

		public void Clear ()
		{
			lock (this) {
				children.Clear ();
				messages.Clear ();
				log.Clear ();
				errors.Clear ();
				status = TestStatus.None;
			}
			OnPropertyChanged ("Children");
			OnPropertyChanged ("Messages");
			OnPropertyChanged ("LogEntries");
			OnPropertyChanged ("Errors");
			OnPropertyChanged ("Status");
		}

		void MergeStatus (TestStatus childStatus)
		{
			if (childStatus == TestStatus.None)
				return;

			switch (status) {
			case TestStatus.Canceled:
			case TestStatus.Error:
				break;

			case TestStatus.Ignored:
			case TestStatus.None:
				Status = childStatus;
				break;

			case TestStatus.Unstable:
			case TestStatus.Success:
				if (childStatus == TestStatus.Unstable)
					Status = childStatus;
				else if (childStatus == TestStatus.Error || childStatus == TestStatus.Canceled)
					Status = childStatus;
				break;

			default:
				throw new InvalidOperationException ();
			}
		}

		public void AddError (Exception exception)
		{
			lock (this) {
				WantToModify ();
				errors.Add (exception);
				Status = TestStatus.Error;
			}
		}

		public event PropertyChangedEventHandler PropertyChanged;

		protected virtual void OnPropertyChanged (string propertyName)
		{
			if (PropertyChanged != null)
				PropertyChanged (this, new PropertyChangedEventArgs (propertyName));
		}

		protected void WantToModify ()
		{
			if (parent != null)
				throw new InvalidOperationException ("Cannot modify TestResult after it's been added to a parent.");
		}

		public override string ToString ()
		{
			return string.Format ("[TestResult: Name={0}, Status={1}]", path.FullName, Status);
		}
	}
}
