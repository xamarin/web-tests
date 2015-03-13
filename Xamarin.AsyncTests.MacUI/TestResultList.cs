using System;
using System.Collections.Generic;
using System.Linq;
using Foundation;
using AppKit;
using CoreGraphics;

namespace Xamarin.AsyncTests.MacUI
{
	public partial class TestResultList : AppKit.NSView
	{
		#region Constructors

		// Called when created from unmanaged code
		public TestResultList (IntPtr handle) : base (handle)
		{
			Initialize ();
		}

		// Called when created directly from a XIB file
		[Export ("initWithCoder:")]
		public TestResultList (NSCoder coder) : base (coder)
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
