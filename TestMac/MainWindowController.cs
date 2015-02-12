using System;

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

			UIBinding.Bind (app.MacUI.TestRunner.Run, Run);
			UIBinding.Bind (app.MacUI.TestRunner.Repeat, Repeat);
			UIBinding.Bind (app.MacUI.TestRunner.Stop, Stop);
			UIBinding.Bind (app.MacUI.TestRunner.Clear, Clear);

			UIBinding.Bind (app.MacUI.ServerManager.StatusMessage, ServerStatusMessage);
			UIBinding.Bind (app.MacUI.TestRunner.StatusMessage, ServerStatusMessage);

			SplitView.AddSubview (TestResultList);
			SplitView.AddSubview (TestResultDetails);

			app.MacUI.ServerManager.TestSuite.PropertyChanged += (sender, e) => OnTestSuiteLoaded (e);
			app.MacUI.TestRunner.UpdateResultEvent += (sender, e) => OnTestFinished ();
		}

		void OnTestFinished ()
		{
			var result = AppDelegate.Instance.MacUI.TestRunner.TestResult.Value;
			var model = new TestResultModel (result, result.Name);
			var node = new TestResultNode (model);
			TestResultController.AddObject (node);
		}

		void OnTestSuiteLoaded (TestSuite suite)
		{
			var result = AppDelegate.Instance.MacUI.RootTestResult;
			var model = new TestResultModel (result, suite.Name);
			var node = new TestResultNode (model);
			TestResultController.AddObject (node);
		}

		public new MainWindow Window {
			get { return (MainWindow)base.Window; }
		}
	}
}
