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
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;

using Foundation;
using AppKit;
using Xamarin.AsyncTests;
using Xamarin.AsyncTests.Portable;
using Xamarin.AsyncTests.Framework;

namespace Xamarin.AsyncTests.MacUI
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
			ui = new MacUI ();

			isStopped = true;
			ui.TestRunner.Stop.NotifyStateChanged.StateChanged += (sender, e) => IsStopped = !e;

			mainWindowController = new MainWindowController ();
			mainWindowController.Window.MakeKeyAndOrderFront (this);

			settingsDialogController = new SettingsDialogController ();

			ui.ServerManager.TestSession.PropertyChanged += (sender, e) => {
				Console.WriteLine ("AD SESSION CHANGED: {0}", e);
				if (e == null)
					CurrentSession = null;
				else
					CurrentSession = new TestSessionModel (e);
			};

			ServerParameters parameters;
			try {
				parameters = GetParameters ();
			} catch (AlertException ex) {
				var alert = NSAlert.WithMessage ("Failed to parse command-line arguments", "Ok", string.Empty, string.Empty, ex.Message);
				alert.RunModal ();
				return;
			}

			Start (parameters);
		}

		async void Start (ServerParameters parameters)
		{
			await ui.ServerManager.Start.Execute (parameters);
		}

		ServerParameters GetParameters ()
		{
			var endpointSupport = DependencyInjector.Get<IPortableEndPointSupport> ();

			var args = Settings.Arguments;
			if (args == null)
				return ServerParameters.WaitForConnection (endpointSupport.GetLoopbackEndpoint (8888));

			var parts = args.Split (' ');
			switch (parts [0]) {
			case "listen":
				if (parts.Length == 1)
					return ServerParameters.WaitForConnection (endpointSupport.GetLoopbackEndpoint (8888));
				else if (parts.Length == 2)
					return ServerParameters.WaitForConnection (ParseEndPoint (parts [1]));
				else
					throw new AlertException ("Usage: listen [<optional address>]");

			case "connect":
				if (parts.Length != 2)
					throw new AlertException ("Usage: connect <address>");

				return ServerParameters.ConnectToServer (ParseEndPoint (parts [1]));

			default:
				throw new AlertException ("Unknown command: '{0}'", parts [0]);
			}
		}

		static IPortableEndPoint ParseEndPoint (string address)
		{
			var endpointSupport = DependencyInjector.Get<IPortableEndPointSupport> ();
			try {
				return endpointSupport.ParseEndpoint (address);
			} catch {
				throw new AlertException ("Failed to parse endpoint: '{0}'", address);
			}
		}

		[Export ("ShowPreferences:")]
		public void ShowPreferences ()
		{
			settingsDialogController.Window.MakeKeyAndOrderFront (this);
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

