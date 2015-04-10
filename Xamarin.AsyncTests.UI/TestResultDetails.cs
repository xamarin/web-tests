using System;
using System.Collections.Generic;
using System.Linq;
using Foundation;
using AppKit;

namespace Xamarin.AsyncTests.MacUI
{
	public partial class TestResultDetails : AppKit.NSView
	{
		#region Constructors

		// Called when created from unmanaged code
		public TestResultDetails (IntPtr handle) : base (handle)
		{
			Initialize ();
		}

		// Called when created directly from a XIB file
		[Export ("initWithCoder:")]
		public TestResultDetails (NSCoder coder) : base (coder)
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
