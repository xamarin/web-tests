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

		public ICommand Run {
			get { return runCommand; }
		}

		public ICommand Repeat {
			get { return repeatCommand; }
		}

		public ICommand Clear {
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
			StatusMessage = "No test loaded.";

			Context.TestFinishedEvent += (sender, e) => OnTestFinished (e);
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

			await test.Run (App.Context, result, cancellationToken);
			return result;
		}

		void OnClear ()
		{
			var result = currentResult;
			if (result != null)
				result.Result.Clear ();
			message = null;
			Context.ResetStatistics ();
			StatusMessage = GetStatusMessage ();
		}

		string message;

		void OnTestFinished (TestResult result)
		{
			StatusMessage = GetStatusMessage ();
		}

		string GetStatusMessage ()
		{
			if (!App.TestSuiteManager.HasInstance)
				return "No test loaded.";
			var sb = new StringBuilder ();
			sb.AppendFormat ("{0} tests passed", Context.CountSuccess);
			if (Context.CountErrors > 0)
				sb.AppendFormat (", {0} errors", Context.CountErrors);
			if (Context.CountIgnored > 0)
				sb.AppendFormat (", {0} ignored", Context.CountIgnored);

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
				AutoStop = true;

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

			internal sealed override Task Stop (CancellationToken cancellationToken)
			{
				throw new NotImplementedException ();
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

		class ClearCommand : Command<TestResult>
		{
			public readonly TestRunner Runner;

			public ClearCommand (TestRunner runner)
				: base (runner, runner.App.TestSuiteManager)
			{
				Runner = runner;
				AutoStop = true;
			}

			#region implemented abstract members of Command

			internal override async Task<TestResult> Start (CancellationToken cancellationToken)
			{
				await Task.Yield ();
				Runner.OnClear ();
				return null;
			}

			internal override Task Stop (CancellationToken cancellationToken)
			{
				throw new NotImplementedException ();
			}

			#endregion
		}


	}
}

