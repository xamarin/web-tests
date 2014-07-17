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
using Xamarin.Forms;

namespace Xamarin.AsyncTests.UI
{
	public class TestRunner : CommandProvider<TestResult>
	{
		readonly RunSingleCommand runCommand;
		readonly RepeatCommand repeatCommand;
		readonly ClearCommand clearCommand;

		public Command<TestResult> Run {
			get { return runCommand; }
		}

		public Command<TestResult> Repeat {
			get { return repeatCommand; }
		}

		public Command Clear {
			get { return clearCommand; }
		}

		public TestContext Context {
			get { return App.Context; }
		}

		public TestRunner (TestApp app)
			: base (app)
		{
			runCommand = new RunSingleCommand (this);
			repeatCommand = new RepeatCommand (this);
			clearCommand = new ClearCommand (this);
			currentResult = app.RootTestResult;

			Context.Statistics.StatisticsEvent += (sender, e) => OnStatisticsEvent (e);
			app.RootTestResult.PropertyChanged += (sender, e) => OnPropertyChanged ("CurrentTestResult");
		}

		protected override void OnStatusMessageChanged (string message)
		{
			App.TestSuiteManager.StatusMessage = message;
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

		static int countReruns;
		DateTime startTime;

		async Task<TestResult> OnRun (bool repeat, CancellationToken cancellationToken)
		{
			await Task.Yield ();

			var model = currentResult;

			SetStatusMessage ("Running {0}.", model.Result.Test.Name);

			var test = model.Result.Test;
			var result = model.Result;

			if (!model.IsRoot) {
				var name = new TestNameBuilder ();
				name.PushName ("UI-Rerun");
				name.PushParameter ("$uiTriggeredRerun", ++countReruns);

				test = TestSuite.CreateProxy (test, name.GetName ());
				result = new TestResult (name.GetName ());
				model.Result.AddChild (result);
			} else {
				model.Result.Clear ();
			}

			if (repeat)
				test = TestSuite.CreateRepeatedTest (test, App.Options.RepeatCount);

			startTime = DateTime.Now;

			await test.Run (App.Context, result, cancellationToken);

			var elapsed = DateTime.Now - startTime;

			StatusMessage = GetStatusMessage (string.Format ("Finished in {0} seconds", (int)elapsed.TotalSeconds));

			return result;
		}

		void OnClear ()
		{
			var result = currentResult;
			if (result != null)
				result.Result.Clear ();
			message = null;
			Context.Statistics.Reset ();
			StatusMessage = GetStatusMessage ();
		}

		string message;

		void OnStatisticsEvent (TestStatistics.StatisticsEventArgs args)
		{
			StatusMessage = GetStatusMessage ();
		}

		string GetStatusMessage (string prefix = null)
		{
			if (!App.TestSuiteManager.HasInstance)
				return prefix ?? "No test loaded.";
			var sb = new StringBuilder ();
			if (prefix != null) {
				sb.Append (prefix);
				sb.Append (": ");
			}
			sb.AppendFormat ("{0} tests passed", Context.Statistics.CountSuccess);
			if (Context.Statistics.CountErrors > 0)
				sb.AppendFormat (", {0} errors", Context.Statistics.CountErrors);
			if (Context.Statistics.CountIgnored > 0)
				sb.AppendFormat (", {0} ignored", Context.Statistics.CountIgnored);

			if (message != null)
				return string.Format ("{0} ({1})", message, sb);
			else
				return sb.ToString ();
		}

		abstract class RunCommand : Command<TestResult>
		{
			public readonly TestRunner Runner;

			public RunCommand (TestRunner runner)
				: base (runner, runner.App.TestSuiteManager)
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


	}
}

