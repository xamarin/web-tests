using System;
using System.Threading;
using System.Threading.Tasks;

using Foundation;
using AppKit;

using Xamarin.AsyncTests.UI;
using Xamarin.AsyncTests;

namespace TestMac
{
	public partial class MainWindowController : NSWindowController
	{
		public MainWindowController (IntPtr handle) : base (handle)
		{
		}

		[Export ("initWithCoder:")]
		public MainWindowController (NSCoder coder) : base (coder)
		{
		}

		public MainWindowController () : base ("MainWindow")
		{
		}

		public override void AwakeFromNib ()
		{
			base.AwakeFromNib ();

			Initialize ();
		}

		void Initialize ()
		{
			var app = (AppDelegate)NSApplication.SharedApplication.Delegate;

			// UIBinding.Bind (app.MacUI.TestRunner.Run, Run);
			UIBinding.Bind (app.MacUI.TestRunner.Repeat, Repeat);
			UIBinding.Bind (app.MacUI.TestRunner.Stop, Stop);
			UIBinding.Bind (app.MacUI.TestRunner.Clear, Clear);

			UIBinding.Bind (app.MacUI.ServerManager.StatusMessage, ServerStatusMessage);
			UIBinding.Bind (app.MacUI.TestRunner.StatusMessage, ServerStatusMessage);

			SplitView.AddSubview (TestResultList);
			SplitView.AddSubview (TestResultDetails);

			app.MacUI.ServerManager.TestSuite.PropertyChanged += (sender, e) => OnTestSuiteLoaded (e);
		}

		void OnTestSuiteLoaded (TestSuite suite)
		{
			var result = new TestResult (suite.Name);
			result.Test = suite.Test;

			var node = new TestResultNode (result);
			TestResultController.AddObject (node);
		}

		public new MainWindow Window {
			get { return (MainWindow)base.Window; }
		}

		[Export ("run:testCase:indexPath:")]
		public async void RunCommand (TestCaseModel test, NSIndexPath index)
		{
			Console.WriteLine ("RUN: {0} {1}", test, index);
			var ui = AppDelegate.Instance.MacUI;
			var newResult = await ui.TestRunner.RunCommand.Run (test.Test, CancellationToken.None);
			Console.WriteLine ("RESULT: {0}", newResult);
			var model = new TestResultNode (newResult);
			TestResultController.InsertObject (model, index);
		}
	}
}
