//
// ServerManager.cs
//
// Author:
//       Martin Baulig <martin.baulig@xamarin.com>
//
// Copyright (c) 2014 Xamarin Inc. (http://www.xamarin.com)
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
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Xamarin.AsyncTests;
using Xamarin.AsyncTests.Framework;

namespace Xamarin.AsyncTests.UI
{
	using Framework;
	using Server;

	public class ServerManager : CommandProvider<TestServer>
	{
		readonly ServerCommand startCommand;

		public Command<TestServer,ServerParameters> Start {
			get { return startCommand; }
		}

		public SettingsBag Settings {
			get;
			private set;
		}

		public readonly InstanceProperty<TestSuite> TestSuite = new InstanceProperty<TestSuite> ("TestSuite", null);

		public ServerManager (UITestApp app)
			: base (app, null)
		{
			Settings = app.Settings;

			startCommand = new ServerCommand (this);

			serverAddress = string.Empty;
			Settings.PropertyChanged += (sender, e) => LoadSettings ();
			LoadSettings ();
		}

		#region Options

		string serverAddress;

		public string ServerAddress {
			get { return serverAddress; }
			set {
				serverAddress = value;
				Settings.SetValue ("ServerAddress", value);
			}
		}

		void LoadSettings ()
		{
			string value;
			if (Settings.TryGetValue ("ServerAddress", out value))
				serverAddress = value;

			if (Settings.TryGetValue ("StartupAction", out value))
				Enum.TryParse<StartupActionKind> (value, out startupAction);
		}

		#endregion

		#region Startup Action

		StartupActionKind startupAction;

		public StartupActionKind StartupAction {
			get { return startupAction; }
			set {
				if (value == startupAction)
					return;
				startupAction = value;
				Settings.SetValue ("StartupAction", value.ToString ());
			}
		}

		public enum StartupActionKind {
			Nothing,
			LoadLocal,
			Start,
			Connect,
		}

		#endregion

		protected async Task<bool> OnRun (TestServer instance, CancellationToken cancellationToken)
		{
			TestSuite.Value = instance.TestSuite;
			return await instance.Run (cancellationToken);
		}

		protected async Task OnStop (TestServer instance, CancellationToken cancellationToken)
		{
			TestSuite.Value = null;
			SetStatusMessage ("Server stopped.");
			await instance.Stop (cancellationToken);
		}

		class ServerCommand : Command<TestServer,ServerParameters>
		{
			public readonly ServerManager Manager;

			public ServerCommand (ServerManager manager)
				: base (manager, manager.NotifyCanExecute)
			{
				Manager = manager;
			}

			internal override Task<TestServer> Start (ServerParameters parameters, CancellationToken cancellationToken)
			{
				switch (parameters.Mode) {
				case ServerMode.Local:
					return TestServer.StartLocal (Manager.App, cancellationToken);
				case ServerMode.ConnectToServer:
					return TestServer.Connect (Manager.App, parameters.Address, cancellationToken);
				case ServerMode.StartServer:
					return TestServer.StartServer (Manager.App, cancellationToken);
				default:
					throw new InvalidOperationException ();
				}
			}

			internal sealed override Task<bool> Run (TestServer instance, CancellationToken cancellationToken)
			{
				return Manager.OnRun (instance, cancellationToken);
			}

			internal sealed override Task Stop (TestServer instance, CancellationToken cancellationToken)
			{
				return Manager.OnStop (instance, cancellationToken);
			}
		}
	}
}

