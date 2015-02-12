//
// AppDelegate.cs
//
// Author:
//       Martin Baulig <martin.baulig@xamarin.com>
//
// Copyright (c) 2015 Xamarin, Inc.
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
using System.Threading.Tasks;

using Foundation;
using AppKit;
using Xamarin.AsyncTests;

namespace TestMac
{
	public partial class AppDelegate : NSApplicationDelegate
	{
		MainWindowController mainWindowController;
		NSMutableArray testResultArray;
		MacUI ui;

		public MacUI MacUI {
			get { return ui; }
		}

		public MainWindowController MainController {
			get { return mainWindowController; }
		}

		public static AppDelegate Instance {
			get { return (AppDelegate)NSApplication.SharedApplication.Delegate; }
		}

		public override void DidFinishLaunching (NSNotification notification)
		{
			ui = MacUI.Create (this);

			mainWindowController = new MainWindowController ();
			mainWindowController.Window.MakeKeyAndOrderFront (this);

			UIBinding.Bind (ui.ServerManager.Local, LoadLocal);
			UIBinding.Bind (ui.ServerManager.Start, StartServer);
			UIBinding.Bind (ui.ServerManager.Connect, ConnectToServer);
			UIBinding.Bind (ui.ServerManager.Stop, Unload);

			ui.ServerManager.Local.Execute ();
		}

		public override void AwakeFromNib ()
		{
			base.AwakeFromNib ();

			testResultArray = new NSMutableArray ();
		}

		public override string ToString ()
		{
			return string.Format ("[AppDelegate: {0:x}]", Handle.ToInt64 ());
		}

		[Export ("TestResultArray")]
		public NSMutableArray TestResultArray {
			get { return testResultArray; }
			set { testResultArray = value; }
		}
	}
}

