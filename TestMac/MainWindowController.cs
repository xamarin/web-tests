using System;

using Foundation;
using AppKit;

using Xamarin.AsyncTests.UI;

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
		}

		public new MainWindow Window {
			get { return (MainWindow)base.Window; }
		}
	}
}
