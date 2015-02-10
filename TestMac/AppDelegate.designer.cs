// WARNING
//
// This file has been generated automatically by Xamarin Studio to store outlets and
// actions made in the UI designer. If it is removed, they will be lost.
// Manual changes to this file may not be handled correctly.
//
using Foundation;
using System.CodeDom.Compiler;

namespace TestMac
{
	[Register ("AppDelegate")]
	partial class AppDelegate
	{
		[Outlet]
		AppKit.NSMenuItem ConnectToServer { get; set; }

		[Outlet]
		AppKit.NSMenuItem LoadLocal { get; set; }

		[Outlet]
		AppKit.NSMenuItem StartServer { get; set; }

		[Outlet]
		AppKit.NSMenuItem Unload { get; set; }
		
		void ReleaseDesignerOutlets ()
		{
			if (LoadLocal != null) {
				LoadLocal.Dispose ();
				LoadLocal = null;
			}

			if (StartServer != null) {
				StartServer.Dispose ();
				StartServer = null;
			}

			if (ConnectToServer != null) {
				ConnectToServer.Dispose ();
				ConnectToServer = null;
			}

			if (Unload != null) {
				Unload.Dispose ();
				Unload = null;
			}
		}
	}
}
