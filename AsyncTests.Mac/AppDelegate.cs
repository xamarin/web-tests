using System;
using System.Drawing;
using MonoMac.Foundation;
using MonoMac.AppKit;
using MonoMac.ObjCRuntime;

namespace Xamarin.AsyncTests.Mac
{
	public partial class AppDelegate : NSApplicationDelegate
	{
		UnitTestRunnerController unitTestController;

		public AppDelegate ()
		{
		}

		public override bool ApplicationShouldTerminateAfterLastWindowClosed (NSApplication sender)
		{
			return true;
		}

		public override void FinishedLaunching (NSObject notification)
		{
			unitTestController = new UnitTestRunnerController ();
			unitTestController.Window.MakeKeyAndOrderFront (this);
		}
	}
}

