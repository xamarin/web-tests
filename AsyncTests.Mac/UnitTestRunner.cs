
using System;
using System.Collections.Generic;
using System.Linq;
using MonoMac.Foundation;
using MonoMac.AppKit;

namespace AsyncTests.Mac {

	public partial class UnitTestRunner : MonoMac.AppKit.NSWindow {
		#region Constructors
		
		// Called when created from unmanaged code
		public UnitTestRunner (IntPtr handle) : base (handle)
		{
			Initialize ();
		}
		
		// Called when created directly from a XIB file
		[Export ("initWithCoder:")]
		public UnitTestRunner (NSCoder coder) : base (coder)
		{
			Initialize ();
		}
		
		// Shared initialization code
		void Initialize ()
		{
		}
		
		#endregion
	}
}

