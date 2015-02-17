using System;
using System.Threading;
using System.Threading.Tasks;

using Foundation;
using AppKit;

using Xamarin.AsyncTests.UI;
using Xamarin.AsyncTests;
using Xamarin.AsyncTests.Framework;

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

			app.MacUI.TestRunner.RunCommand.NotifyStateChanged.StateChanged += (sender, e) => CanRun = e;

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

		[Export ("Run:testCase:indexPath:")]
		public async void Run (TestCaseModel test, NSIndexPath index)
		{
			Console.WriteLine ("RUN: {0} {1}", test, index);
			var ui = AppDelegate.Instance.MacUI;

			var result = await ui.TestRunner.RunCommand.Run (test.Test, test.Test.Name, CancellationToken.None);
			Console.WriteLine ("RESULT: {0}", result);

			var model = new TestResultNode (result);
			TestResultController.InsertObject (model, index);
		}

		bool canRun;

		[Export ("CanRun")]
		public bool CanRun {
			get { return canRun; }
			set {
				WillChangeValue ("CanRun");
				canRun = value;
				DidChangeValue ("CanRun");
			}
		}
	}
}
