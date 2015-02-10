//
// TestRunner.cs
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
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Xamarin.AsyncTests;
using Xamarin.AsyncTests.Framework;

namespace Xamarin.AsyncTests.UI
{
	public class TestRunner : CommandProvider<TestResult>
	{
		readonly RunSingleCommand runCommand;
		readonly RepeatCommand repeatCommand;
		readonly ClearCommand clearCommand;
		readonly RefreshCommand refreshCommand;

		public Command<TestResult> Run {
			get { return runCommand; }
		}

		public Command<TestResult> Repeat {
			get { return repeatCommand; }
		}

		public Command Clear {
			get { return clearCommand; }
		}

		public Command Refresh {
			get { return refreshCommand; }
		}

		public TestApp Context {
			get { return App.Context; }
		}

		public TestRunner (UITestApp app)
			: base (app)
		{
			runCommand = new RunSingleCommand (this);
			repeatCommand = new RepeatCommand (this);
			clearCommand = new ClearCommand (this);
			refreshCommand = new RefreshCommand (this);
			currentResult = app.RootTestResult;

			app.RootTestResult.PropertyChanged += (sender, e) => OnPropertyChanged ("CurrentTestResult");
		}

		protected override void OnStatusMessageChanged (string message)
		{
			base.OnStatusMessageChanged (message);
		}

		TestResultModel currentResult;
		public TestResultModel CurrentTestResult {
			get { return currentResult; }
			set {
				lock (this) {
					currentResult = value;
					OnPropertyChanged ("CurrentTestResult");
				}
			}
		}

		public static readonly BindableProperty CurrentTestProperty =
			BindableProperty.Create ("CurrentTest", typeof(string), typeof(TestRunner), string.Empty);

		public string CurrentTest {
			get { return (string)GetValue (CurrentTestProperty); }
			set { SetValue (CurrentTestProperty, value); }
		}

		static int countReruns;
		DateTime startTime;

		async Task<TestResult> OnRun (bool repeat, CancellationToken cancellationToken)
		{
			await Task.Yield ();

			var model = currentResult;

			App.Logger.ResetStatistics ();

			SetStatusMessage ("Running {0}.", model.Result.Test.Name);

			var test = model.Result.Test;
			var result = model.Result;

			if (!model.IsRoot) {
				var name = new TestNameBuilder ();
				name.PushName ("UI-Rerun");
				name.PushParameter ("$uiTriggeredRerun", ++countReruns);

				test = TestFramework.CreateProxy (test, name.GetName ());
				result = new TestResult (name.GetName ());
			} else {
				model.Result.Clear ();
			}

			var session = new TestSession (App, test, result);

			startTime = DateTime.Now;

			if (repeat)
				await session.Repeat (App.Options.RepeatCount, cancellationToken);
			else
				await session.Run (cancellationToken);

			var elapsed = DateTime.Now - startTime;

			CurrentTest = string.Empty;
			StatusMessage = GetStatusMessage (string.Format ("Finished in {0} seconds", (int)elapsed.TotalSeconds));

			App.Logger.LogMessage ("DONE: |{0}|{1}|", session.Name, StatusMessage);

			if (!model.IsRoot)
				model.Result.AddChild (result);

			OnRefresh ();

			return result;
		}

		void OnClear ()
		{
			var result = currentResult;
			if (result != null)
				result.Result.Clear ();
			message = null;
			App.Logger.ResetStatistics ();
			StatusMessage = GetStatusMessage ();
			OnRefresh ();
		}

		void OnRefresh ()
		{
			if (UpdateResultEvent != null)
				UpdateResultEvent (this, EventArgs.Empty);
		}

		public event EventHandler UpdateResultEvent;

		string message;

		int countTests;
		int countSuccess;
		int countErrors;
		int countIgnored;

		internal void OnStatisticsEvent (TestLoggerBackend.StatisticsEventArgs args)
		{
			StatusMessage = GetStatusMessage ();

			switch (args.Type) {
			case TestLoggerBackend.StatisticsEventType.Running:
				++countTests;
				CurrentTest = string.Format ("Running {0}", args.Name);
				break;
			case TestLoggerBackend.StatisticsEventType.Finished:
				switch (args.Status) {
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

				CurrentTest = string.Format ("Finished {0}: {1}", args.Name, args.Status);
				break;
			default:
				break;
			}
		}

		string GetStatusMessage (string prefix = null)
		{
			if (!App.ServerManager.HasTestSuite)
				return prefix ?? "No test loaded.";
			var sb = new StringBuilder ();
			if (prefix != null) {
				sb.Append (prefix);
				sb.Append (": ");
			}
			sb.AppendFormat ("{0} tests passed", countSuccess);
			if (countErrors > 0)
				sb.AppendFormat (", {0} errors", countErrors);
			if (countIgnored > 0)
				sb.AppendFormat (", {0} ignored", countIgnored);

			if (message != null)
				return string.Format ("{0} ({1})", message, sb);
			else
				return sb.ToString ();
		}

		abstract class RunCommand : Command<TestResult>
		{
			public readonly TestRunner Runner;

			public RunCommand (TestRunner runner)
				: base (runner, runner.App.ServerManager)
			{
				Runner = runner;

				Runner.PropertyChanged += (sender, e) => {
					switch (e.PropertyName) {
					case "CurrentTestResult":
						var result = runner.CurrentTestResult;
						if (result == null || result.Result.Test == null)
							CanExecute = false;
						else
							CanExecute = true;
						break;
					}
				};
			}

			internal sealed override Task<bool> Run (TestResult result, CancellationToken cancellationToken)
			{
				return Task.FromResult (false);
			}

			internal sealed override Task Stop (TestResult result, CancellationToken cancellationToken)
			{
				return Task.FromResult (false);
			}
		}

		class RunSingleCommand : RunCommand
		{
			public RunSingleCommand (TestRunner runner)
				: base (runner)
			{
			}

			internal override Task<TestResult> Start (CancellationToken cancellationToken)
			{
				return Runner.OnRun (false, cancellationToken);
			}
		}

		class RepeatCommand : RunCommand
		{
			public RepeatCommand (TestRunner runner)
				: base (runner)
			{
			}

			internal override Task<TestResult> Start (CancellationToken cancellationToken)
			{
				return Runner.OnRun (true, cancellationToken);
			}
		}

		class ClearCommand : RunCommand
		{
			public ClearCommand (TestRunner runner)
				: base (runner)
			{
			}

			#region implemented abstract members of Command

			internal override async Task<TestResult> Start (CancellationToken cancellationToken)
			{
				await Task.Yield ();
				Runner.OnClear ();
				return null;
			}

			#endregion
		}


		class RefreshCommand : RunCommand
		{
			public RefreshCommand (TestRunner runner)
				: base (runner)
			{
			}

			#region implemented abstract members of Command

			internal override async Task<TestResult> Start (CancellationToken cancellationToken)
			{
				await Task.Yield ();
				Runner.OnRefresh ();
				return null;
			}

			#endregion
		}

	}
}

