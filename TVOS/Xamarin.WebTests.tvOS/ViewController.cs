//
// ViewController.cs
//
// Author:
//       Martin Baulig <martin.baulig@xamarin.com>
//
// Copyright (c) 2016 Xamarin, Inc.
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
using System;
using Foundation;
using UIKit;

namespace Xamarin.WebTests.tvOS
{
	using AsyncTests;
	using AsyncTests.Mobile;
	using AsyncTests.Framework;
	using System.Collections.Generic;

	public partial class ViewController : UIViewController, ISimpleUIController
	{
		public ViewController (IntPtr handle) : base (handle)
		{
			var appDelegate = (AppDelegate)UIApplication.SharedApplication.Delegate;
			var options = Environment.GetEnvironmentVariable ("XAMARIN_ASYNCTESTS_OPTIONS");
			App = new MobileTestApp (this, appDelegate.Framework, options);
		}

		public MobileTestApp App {
			get;
			private set;
		}

		bool isRunning;
		bool canRun;
		bool isRemote;
		int selectedCategory;

		public bool IsRunning {
			get {
				return isRunning;
			}
			set {
				InvokeOnMainThread (() => {
					isRunning = value;
					if (value) {
						RunButton.Enabled = false;
						StopButton.Enabled = true;
					} else {
						RunButton.Enabled = canRun;
						StopButton.Enabled = false;
					}
				});
			}
		}

		public bool CanRun {
			get {
				return canRun;
			}
			set {
				InvokeOnMainThread (() => {
					canRun = value;
					RunButton.Enabled = value && !isRunning;
				});
			}
		}

		public bool IsRemote {
			get {
				return isRemote;
			}
			set {
				InvokeOnMainThread (() => {
					isRemote = value;
					CategoryLabel.Enabled = !value;
				});
			}
		}

		public IList<string> Categories {
			get;
			private set;
		}

		public int SelectedCategory {
			get {
				return selectedCategory;
			}
			set {
				InvokeOnMainThread (() => {
					selectedCategory = value;
					if (selectedCategory >= 0 && selectedCategory < Categories.Count)
						CategoryLabel.Text = string.Format ("Test category: {0}", Categories [selectedCategory]);
					else
						CategoryLabel.Text = string.Empty;
				});
			}
		}

		public event EventHandler SessionChangedEvent;

		event EventHandler<int> ISimpleUIController.CategoryChangedEvent {
			add { }
			remove { }
		}

		public override void ViewDidLoad ()
		{
			base.ViewDidLoad ();

			OnSessionChanged ();

			App.Start ();
		}

		void OnSessionChanged ()
		{
			if (SessionChangedEvent != null)
				SessionChangedEvent (this, EventArgs.Empty);
		}

		partial void OnRun (UIButton sender)
		{
			App.Run ();
		}

		partial void OnStop (UIButton sender)
		{
			App.Stop ();
		}

		public override void DidReceiveMemoryWarning ()
		{
			base.DidReceiveMemoryWarning ();
			// Release any cached data, images, etc that aren't in use.
		}

		public void DebugMessage (string message)
		{
			Console.Error.WriteLine (message);
		}

		public void Message (string message)
		{
			InvokeOnMainThread (() => MainLabel.Text = message);
		}

		public void Message (string format, params object [] args)
		{
			Message (string.Format (format, args));
		}

		public void StatusMessage (string message)
		{
			InvokeOnMainThread (() => StatusLabel.Text = message);
		}

		public void StatusMessage (string format, params object [] args)
		{
			StatusMessage (string.Format (format, args));
		}

		public void StatisticsMessage (string message)
		{
			InvokeOnMainThread (() => StatisticsLabel.Text = message);
		}

		public void StatisticsMessage (string format, params object [] args)
		{
			StatisticsMessage (string.Format (format, args));
		}

		public void SetCategories (IList<string> categories, int selected)
		{
			Categories = categories;
			SelectedCategory = selected;
		}
	}
}


