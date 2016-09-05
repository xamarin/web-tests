// WARNING
//
// This file has been generated automatically by Xamarin Studio from the outlets and
// actions declared in your storyboard file.
// Manual changes to this file will not be maintained.
//
using Foundation;
using System;
using System.CodeDom.Compiler;
using UIKit;

namespace Xamarin.WebTests.tvOS
{
    [Register ("ViewController")]
    partial class ViewController
    {
        [Outlet]
        [GeneratedCode ("iOS Designer", "1.0")]
        UIKit.UILabel MainLabel { get; set; }

        [Outlet]
        [GeneratedCode ("iOS Designer", "1.0")]
        UIKit.UIButton RunButton { get; set; }

        [Outlet]
        [GeneratedCode ("iOS Designer", "1.0")]
        UIKit.UILabel StatisticsLabel { get; set; }

        [Outlet]
        [GeneratedCode ("iOS Designer", "1.0")]
        UIKit.UILabel StatusLabel { get; set; }

        [Outlet]
        [GeneratedCode ("iOS Designer", "1.0")]
        UIKit.UILabel CategoryLabel { get; set; }

	[Outlet]
        [GeneratedCode ("iOS Designer", "1.0")]
        UIKit.UIButton StopButton { get; set; }

        [Action ("RunButton_PrimaryActionTriggered:")]
        [GeneratedCode ("iOS Designer", "1.0")]
        partial void OnRun (UIKit.UIButton sender);

        [Action ("StopButton_PrimaryActionTriggered:")]
        [GeneratedCode ("iOS Designer", "1.0")]
        partial void OnStop (UIKit.UIButton sender);

        void ReleaseDesignerOutlets ()
        {
            if (MainLabel != null) {
                MainLabel.Dispose ();
                MainLabel = null;
            }

            if (RunButton != null) {
                RunButton.Dispose ();
                RunButton = null;
            }

            if (StatisticsLabel != null) {
                StatisticsLabel.Dispose ();
                StatisticsLabel = null;
            }

            if (StatusLabel != null) {
                StatusLabel.Dispose ();
                StatusLabel = null;
            }

            if (CategoryLabel != null) {
                CategoryLabel.Dispose ();
                CategoryLabel = null;
            }

            if (StopButton != null) {
                StopButton.Dispose ();
                StopButton = null;
            }
        }
    }
}