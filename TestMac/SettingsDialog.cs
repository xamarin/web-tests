using System;

using Foundation;
using AppKit;

namespace Xamarin.AsyncTests.MacUI
{
	public partial class SettingsDialog : NSWindow
	{
		public SettingsDialog (IntPtr handle) : base (handle)
		{
		}

		[Export ("initWithCoder:")]
		public SettingsDialog (NSCoder coder) : base (coder)
		{
		}

		public override void AwakeFromNib ()
		{
			base.AwakeFromNib ();
		}
	}
}
