using System;

using Foundation;
using AppKit;

namespace TestMac
{
	public partial class MainWindowController : NSWindowController
	{
		MacUI ui;

		public UICommand LoadLocalCommand {
			get;
			private set;
		}

		public UICommand RunCommand {
			get;
			private set;
		}

		public UICommand RepeatCommand {
			get;
			private set;
		}

		public UICommand StopCommand {
			get;
			private set;
		}

		public UICommand ClearCommand {
			get;
			private set;
		}

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
			ui = MacUI.Create (this);

			LoadLocalCommand = new UICommand (ui.ServerManager.Local, Load);
			RunCommand = new UICommand (ui.TestRunner.Run, Run);
			RepeatCommand = new UICommand (ui.TestRunner.Repeat, Repeat);
			StopCommand = new UICommand (ui.TestRunner.Stop, Stop);
			ClearCommand = new UICommand (ui.ServerManager.Stop, Clear);

			ui.ServerManager.PropertyChanged += (sender, e) => {
				Console.WriteLine ("PROPERTY CHANGED: {0} {1}", e.PropertyName, ui.ServerManager.StatusMessage);
				InvokeOnMainThread (() => ServerStatusMessage.StringValue = ui.ServerManager.StatusMessage);
			};

			ui.TestRunner.PropertyChanged += (sender, e) => {
				Console.WriteLine ("PROPERTY CHANGED #1: {0} {1}", e.PropertyName, ui.TestRunner.StatusMessage);
				InvokeOnMainThread (() => TestRunnerStatusMessage.StringValue = ui.TestRunner.StatusMessage);
			};
		}

		public new MainWindow Window {
			get { return (MainWindow)base.Window; }
		}
	}
}
