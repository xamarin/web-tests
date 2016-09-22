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

namespace Xamarin.AsyncTests.MacUI
{
	public class TestRunner : CommandProvider<TestResult>
	{
		readonly RunCommand runCommand;

		public Command<TestResult,RunParameters> Run {
			get { return runCommand; }
		}

		public TestApp Context {
			get { return App.Context; }
		}

		public TestRunner (MacUI app)
			: base (app, app.ServerManager)
		{
			runCommand = new RunCommand (this);
		}

		public readonly Property<string> CurrentTestName = new InstanceProperty<string> ("CurrentTest", null);

		DateTime startTime;

		async Task<TestResult> OnRun (RunParameters parameters, CancellationToken cancellationToken)
		{
			await Task.Yield ();

			App.Logger.ResetStatistics ();

			SetStatusMessage ("Updating settings.");

			await parameters.Session.UpdateSettings (cancellationToken);

			SetStatusMessage ("Running {0}.", parameters.Test.Name);

			startTime = DateTime.Now;

			var result = await parameters.Session.Run (parameters.Test, cancellationToken);

			var elapsed = DateTime.Now - startTime;

			CurrentTestName.Value = string.Empty;
			StatusMessage.Value = GetStatusMessage (string.Format ("Finished in {0} seconds", (int)elapsed.TotalSeconds));

			App.Logger.LogMessage (StatusMessage.Value);

			OnRefresh ();

			return result;
		}

		void OnClear ()
		{
			message = null;
			App.Logger.ResetStatistics ();
			StatusMessage.Value = GetStatusMessage ();
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
			switch (args.Type) {
			case TestLoggerBackend.StatisticsEventType.Running:
				++countTests;
				CurrentTestName.Value = string.Format ("Running {0}", args.Name);
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

				CurrentTestName.Value = string.Format ("Finished {0}: {1}", args.Name, args.Status);
				break;
			case TestLoggerBackend.StatisticsEventType.Reset:
				countTests = countSuccess = countErrors = countIgnored = 0;
				CurrentTestName.Value = string.Empty;
				break;
			default:
				break;
			}

			StatusMessage.Value = GetStatusMessage (CurrentTestName.Value);
		}

		string GetStatusMessage (string prefix = null)
		{
			if (!App.ServerManager.TestSession.HasValue)
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

		class RunCommand : Command<TestResult,RunParameters>
		{
			public readonly TestRunner Runner;

			public RunCommand (TestRunner runner)
				: base (runner, runner.NotifyCanExecute)
			{
				Runner = runner;
			}

			internal override Task Stop (TestResult instance, CancellationToken cancellationToken)
			{
				return Task.FromResult (false);
			}

			internal override Task<bool> Run (TestResult instance, CancellationToken cancellationToken)
			{
				return Task.FromResult (false);
			}

			internal override Task<TestResult> Start (RunParameters parameters, CancellationToken cancellationToken)
			{
				return Runner.OnRun (parameters, cancellationToken);
			}
		}
	}
}

