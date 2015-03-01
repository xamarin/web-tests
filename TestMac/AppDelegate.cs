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
using Xamarin.AsyncTests.Framework;
using Xamarin.AsyncTests.UI;

namespace TestMac
{
	public partial class AppDelegate : NSApplicationDelegate
	{
		MainWindowController mainWindowController;
		SettingsDialogController settingsDialogController;
		TestSessionModel currentSession;
		bool isStopped;
		MacUI ui;

		public MacUI MacUI {
			get { return ui; }
		}

		[Export ("MainController")]
		public MainWindowController MainController {
			get { return mainWindowController; }
		}

		public static AppDelegate Instance {
			get { return (AppDelegate)NSApplication.SharedApplication.Delegate; }
		}

		public static SettingsDialogController Settings {
			get { return Instance.settingsDialogController; }
		}

		public override void DidFinishLaunching (NSNotification notification)
		{
			ui = MacUI.Create ();

			isStopped = true;
			ui.TestRunner.Stop.NotifyStateChanged.StateChanged += (sender, e) => IsStopped = !e;

			mainWindowController = new MainWindowController ();
			mainWindowController.Window.MakeKeyAndOrderFront (this);

			settingsDialogController = new SettingsDialogController ();

			ui.ServerManager.TestSuite.PropertyChanged += (sender, e) => {
				Console.WriteLine ("AD SUITE CHANGED: {0}", e);
				if (e == null)
					CurrentSession = null;
				else {
					var session = new OldTestSession (ui, e);
					CurrentSession = new TestSessionModel (session);
				}
			};

			LoadLocalTestSuite ();
		}

		[Export ("ShowPreferences:")]
		public void ShowPreferences ()
		{
			settingsDialogController.Window.MakeKeyAndOrderFront (this);
		}

		[Export ("LoadLocalTestSuite:")]
		public async void LoadLocalTestSuite ()
		{
			await ui.ServerManager.Start.Execute (ServerParameters.CreatePipe ());
		}

		[Export ("UnloadTestSuite:")]
		public async void UnloadTestSuite ()
		{
			await ui.ServerManager.Stop.Execute ();
		}

		[Export ("ClearSession:")]
		public void ClearSession ()
		{
			if (CurrentSession != null)
				CurrentSession.RemoveAllChildren ();
		}

		[Export ("LoadSession:")]
		public void LoadSession ()
		{
			Console.WriteLine ("LOAD SESSION");
		}

		[Export ("SaveSession:")]
		public void SaveSession ()
		{
			Console.WriteLine ("SAVE SESSION");
		}

		public override string ToString ()
		{
			return string.Format ("[AppDelegate: {0:x}]", Handle.ToInt64 ());
		}

		public const string CurrentSessionKey = "CurrentSession";

		[Export (CurrentSessionKey)]
		public TestSessionModel CurrentSession {
			get { return currentSession; }
			set {
				WillChangeValue (CurrentSessionKey);
				currentSession = value;
				DidChangeValue (CurrentSessionKey);
			}
		}

		public const string IsStoppedKey = "IsStopped";

		[Export (IsStoppedKey)]
		public bool IsStopped {
			get { return isStopped; }
			set {
				WillChangeValue (IsStoppedKey);
				isStopped = value;
				DidChangeValue (IsStoppedKey);
			}
		}


	}
}

