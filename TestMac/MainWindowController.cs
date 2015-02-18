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
			var app = AppDelegate.Instance;

			app.MacUI.TestRunner.Run.NotifyStateChanged.StateChanged += (sender, e) => CanRun = e;
			app.MacUI.TestRunner.Stop.NotifyStateChanged.StateChanged += (sender, e) => CanStop = e;

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
			node.Model.IsRoot = true;
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

			var parameters = new RunParameters (test.Test);
			var result = await ui.TestRunner.Run.Execute (parameters);
			Console.WriteLine ("RESULT: {0}", result);

			var model = new TestResultNode (result);
			TestResultController.InsertObject (model, index);
		}

		bool canRun;
		bool canStop;

		[Export ("CanRun")]
		public bool CanRun {
			get { return canRun; }
			set {
				WillChangeValue ("CanRun");
				canRun = value;
				DidChangeValue ("CanRun");
			}
		}

		[Export ("Clear:testResult:indexPath:")]
		public void Clear (TestResultModel result, NSIndexPath indexPath)
		{
			if (!result.IsRoot)
				TestResultController.RemoveObjectAtArrangedObjectIndexPath (indexPath);
		}

		[Export ("CanStop")]
		public bool CanStop {
			get { return canStop; }
			set {
				WillChangeValue ("CanStop");
				canStop = value;
				DidChangeValue ("CanStop");
			}
		}

		[Export ("Stop:")]
		public async void Stop ()
		{
			var ui = AppDelegate.Instance.MacUI;
			await ui.TestRunner.Stop.Execute ();
		}
	}
}
