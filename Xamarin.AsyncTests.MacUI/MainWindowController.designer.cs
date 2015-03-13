// WARNING
//
// This file has been generated automatically by Xamarin Studio to store outlets and
// actions made in the UI designer. If it is removed, they will be lost.
// Manual changes to this file may not be handled correctly.
//
using Foundation;
using System.CodeDom.Compiler;

namespace Xamarin.AsyncTests.MacUI
{
	[Register ("MainWindowController")]
	partial class MainWindowController
	{
		[Outlet]
		AppKit.NSButton Repeat { get; set; }

		[Outlet]
		AppKit.NSTextField ServerStatusMessage { get; set; }

		[Outlet]
		AppKit.NSSplitView SplitView { get; set; }

		[Outlet]
		AppKit.NSTreeController TestResultController { get; set; }

		[Outlet]
		Xamarin.AsyncTests.MacUI.TestResultDetails TestResultDetails { get; set; }

		[Outlet]
		Xamarin.AsyncTests.MacUI.TestResultList TestResultList { get; set; }

		[Outlet]
		AppKit.NSOutlineView TestResultView { get; set; }
		
		void ReleaseDesignerOutlets ()
		{
			if (Repeat != null) {
				Repeat.Dispose ();
				Repeat = null;
			}

			if (ServerStatusMessage != null) {
				ServerStatusMessage.Dispose ();
				ServerStatusMessage = null;
			}

			if (SplitView != null) {
				SplitView.Dispose ();
				SplitView = null;
			}

			if (TestResultController != null) {
				TestResultController.Dispose ();
				TestResultController = null;
			}

			if (TestResultDetails != null) {
				TestResultDetails.Dispose ();
				TestResultDetails = null;
			}

			if (TestResultList != null) {
				TestResultList.Dispose ();
				TestResultList = null;
			}

			if (TestResultView != null) {
				TestResultView.Dispose ();
				TestResultView = null;
			}
		}
	}
}
