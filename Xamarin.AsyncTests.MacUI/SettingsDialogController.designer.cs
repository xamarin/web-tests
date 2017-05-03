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
	[Register ("SettingsDialogController")]
	partial class SettingsDialogController
	{
		[Outlet]
		AppKit.NSArrayController FeaturesController { get; set; }
		
		void ReleaseDesignerOutlets ()
		{
			if (FeaturesController != null) {
				FeaturesController.Dispose ();
				FeaturesController = null;
			}
		}
	}
}
