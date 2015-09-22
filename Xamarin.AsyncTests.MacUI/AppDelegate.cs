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
using System.IO;
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
	public partial class AppDelegate : NSApplicationDelegate, IAppDelegate
	{
		MainWindowController mainWindowController;
		SettingsDialogController settingsDialogController;
		TestSession currentSession;
		bool isStopped;
		bool hasServer;
		MacUI ui;

		public event EventHandler<TestSession> SessionChangedEvent;

		public TestSession CurrentSession {
			get { return currentSession; }
		}

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

		public bool HasBuiltinFramework {
			get { return DependencyInjector.IsAvailable (typeof (IBuiltinTestServer)); }
		}

		public override void DidFinishLaunching (NSNotification notification)
		{
			ui = new MacUI ();

			isStopped = true;
			ui.TestRunner.Stop.NotifyStateChanged.StateChanged += (sender, e) => IsStopped = !e;
			ui.ServerManager.Start.NotifyStateChanged.StateChanged += (sender, e) => HasServer = !e;
			IsStopped = true;

			settingsDialogController = new SettingsDialogController ();

			mainWindowController = new MainWindowController ();
			mainWindowController.Window.MakeKeyAndOrderFront (this);

			ui.ServerManager.TestSession.PropertyChanged += (sender, e) => {
				currentSession = e;
				if (SessionChangedEvent != null)
					SessionChangedEvent (this, e);
			};

			StartServer ();

			settingsDialogController.DidFinishLaunching ();
		}

		ServerParameters GetParameters ()
		{
			var serverMode = Settings.CurrentServerMode.Mode;

			switch (serverMode) {
			case ServerMode.Local:
				return GetLocalParameters ();
			case ServerMode.WaitForConnection:
				return GetListenParameters ();
			case ServerMode.Android:
				return GetAndroidParameters ();
			case ServerMode.iOS:
				return GetIOSParameters ();
			case ServerMode.Builtin:
				return new ServerParameters (ServerMode.Builtin);
			default:
				throw new AlertException ("Invalid server mode: {0}", serverMode);
			}
		}

		ServerParameters GetAndroidParameters ()
		{
			var address = Settings.AndroidEndpoint;
			if (string.IsNullOrEmpty (address))
				throw new AlertException ("Must set Android Endpoint in Settings Dialog.");

			var endpoint = ParseEndPoint (address);
			return ServerParameters.Android (endpoint);
		}

		ServerParameters GetIOSParameters ()
		{
			var address = Settings.IOSEndpoint;
			if (string.IsNullOrEmpty (address))
				throw new AlertException ("Must set iOS Endpoint in Settings Dialog.");

			var endpoint = ParseEndPoint (address);
			return ServerParameters.IOS (endpoint);
		}

		ServerParameters GetListenParameters ()
		{
			var address = Settings.ListenAddress;
			if (string.IsNullOrEmpty (address))
				throw new AlertException ("Must set Listen Address in Settings Dialog.");

			var endpoint = ParseEndPoint (address);
			return ServerParameters.WaitForConnection (endpoint);
		}

		ServerParameters GetLocalParameters ()
		{
			var pipeArgs = new PipeArguments ();
			pipeArgs.MonoPrefix = Settings.MonoRuntime;
			if (string.IsNullOrEmpty (pipeArgs.MonoPrefix))
				throw new AlertException ("Must set Mono Runtime in Settings Dialog.");

			var monoPath = Path.Combine (pipeArgs.MonoPrefix, "bin", "mono");
			if (!File.Exists (monoPath))
				throw new AlertException ("Invalid runtime prefix: {0}", pipeArgs.MonoPrefix);

			var launcherPath = Settings.LauncherPath;
			if (!string.IsNullOrEmpty (launcherPath))
				pipeArgs.ConsolePath = FindFile (launcherPath);

			var assembly = Settings.TestSuite;
			if (string.IsNullOrEmpty (assembly))
				throw new AlertException ("Must set Test Suite in Settings Dialog.");
			pipeArgs.Assembly = FindFile (assembly);
			pipeArgs.ExtraArguments = Settings.Arguments;

			var address = Settings.ListenAddress;
			if (string.IsNullOrEmpty (address))
				throw new AlertException ("Must set Listen Address in Settings Dialog.");

			var endpoint = ParseEndPoint (address);
			return ServerParameters.CreatePipe (endpoint, pipeArgs);
		}

		static string FindFile (string filename)
		{
			if (Path.IsPathRooted (filename))
				return filename;

			var appDir = Path.GetDirectoryName (Path.GetDirectoryName (Environment.CurrentDirectory));
			appDir = Path.GetDirectoryName (appDir);
			var projectDir = Path.GetDirectoryName (Path.GetDirectoryName (appDir));
			var solutionDir = Path.GetDirectoryName (projectDir);
			var outDir = Path.Combine (solutionDir, "out");

			var outFile = Path.Combine (outDir, filename);

			if (!File.Exists (outFile))
				throw new AlertException ("Cannot find file: {0}", filename);

			return outFile;
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

		static void ShowAlertForException (string message, Exception ex)
		{
			var alert = NSAlert.WithMessage (message, "Ok", string.Empty, string.Empty, ex.Message);
			alert.RunModal ();
		}

		[Export ("ShowPreferences")]
		public void ShowPreferences ()
		{
			settingsDialogController.Window.MakeKeyAndOrderFront (this);
		}

		[Export ("StartServer")]
		public async void StartServer ()
		{
			ServerParameters parameters;
			try {
				parameters = GetParameters ();
			} catch (AlertException ex) {
				var alert = NSAlert.WithMessage ("Failed to start server", "Ok", string.Empty, string.Empty, ex.Message);
				alert.RunModal ();
				return;
			}

			try {
				await ui.ServerManager.Start.Execute (parameters);
			} catch (TaskCanceledException) {
				;
			} catch (Exception ex) {
				ShowAlertForException ("Failed to start server", ex);
				return;
			}
		}

		[Export ("StopServer")]
		public async void StopServer ()
		{
			try {
				await ui.ServerManager.Stop.Execute ();
			} catch (TaskCanceledException) {
				;
			} catch (Exception ex) {
				var alert = NSAlert.WithMessage ("Failed to stop server", "Ok", string.Empty, string.Empty, ex.Message);
				alert.RunModal ();
				return;
			}
		}

		[Export ("ClearSession")]
		public void ClearSession ()
		{
			#if FIXME
			if (CurrentSessionModel != null)
				CurrentSessionModel.RemoveAllChildren ();
			#endif
		}

		[Export ("LoadSession")]
		public void LoadSession ()
		{
			var open = new NSOpenPanel {
				CanChooseDirectories = false, CanChooseFiles = true, AllowsMultipleSelection = false,
				AllowedFileTypes = new string[] { "xml" }
			};
			var ret = open.RunModal ();
			if (ret == 0 || open.Urls.Length != 1)
				return;

			try {
				MainController.LoadSession (open.Urls [0].Path);
			} catch (Exception ex) {
				ShowAlertForException ("Failed to load session", ex);
				return;
			}
		}

		[Export ("SaveSession")]
		public void SaveSession ()
		{
			var save = new NSSavePanel {
				CanCreateDirectories = false, CanSelectHiddenExtension = false,
				AllowedFileTypes = new string[] { "xml" }
			};
			var ret = save.RunModal ();
			if (ret == 0 || save.Url == null)
				return;

			try {
				MainController.SaveSession (save.Url.Path);
			} catch (Exception ex) {
				ShowAlertForException ("Failed to save session", ex);
				return;
			}
		}

		public override string ToString ()
		{
			return string.Format ("[AppDelegate: {0:x}]", Handle.ToInt64 ());
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

		public const string HasServerKey = "HasServer";

		[Export (HasServerKey)]
		public bool HasServer {
			get { return hasServer; }
			set {
				WillChangeValue (HasServerKey);
				hasServer = value;
				DidChangeValue (HasServerKey);
			}
		}

		NSApplicationDelegate IAppDelegate.Delegate {
			get { return this; }
		}

		SettingsDialogController IAppDelegate.Settings {
			get { return Settings; }
		}
	}
}

