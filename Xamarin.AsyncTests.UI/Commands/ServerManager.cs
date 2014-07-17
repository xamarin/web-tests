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
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Xamarin.AsyncTests;
using Xamarin.AsyncTests.Framework;
using Xamarin.Forms;

namespace Xamarin.AsyncTests.UI
{
	using Framework;
	using Server;

	public class ServerManager : CommandProvider<TestProvider>
	{
		readonly LocalCommand localCommand;
		readonly ConnectCommand connectCommand;
		readonly StartCommand startCommand;

		public Command<TestProvider> Local {
			get { return localCommand; }
		}

		public Command<TestProvider> Connect {
			get { return connectCommand; }
		}

		public Command<TestProvider> Start {
			get { return startCommand; }
		}

		public SettingsBag Settings {
			get;
			private set;
		}

		public ServerManager (TestApp app)
			: base (app)
		{
			Settings = app.Settings;

			localCommand = new LocalCommand (this);
			connectCommand = new ConnectCommand (this);
			startCommand = new StartCommand (this);

			CanStart = app.ServerHost != null;

			serverAddress = string.Empty;
			Settings.PropertyChanged += (sender, e) => LoadSettings ();
			LoadSettings ();
		}

		internal async Task Initialize ()
		{
			if (UseServer) {
				if (AutoLoad)
					await App.TestSuiteManager.LoadLocal.Execute ();
				if (AutoStart)
					await Start.Execute ();
				return;
			} else if (AutoStart) {
				await Connect.Execute ();
				if (HasInstance && AutoLoad)
					await App.TestSuiteManager.LoadFromServer.Execute ();
			} else if (AutoLoad) {
				await App.TestSuiteManager.LoadLocal.Execute ();
			}
		}

		#region Options

		string serverAddress;

		public string ServerAddress {
			get { return serverAddress; }
			set {
				serverAddress = value;
				Settings.SetValue ("ServerAddress", value);
				OnPropertyChanged ("ServerAddress");
			}
		}

		bool autoStart;
		bool autoLoad;
		bool useServer;

		public bool AutoStart {
			get { return autoStart; }
			set {
				if (value == autoStart)
					return;
				autoStart = value;
				Settings.SetValue ("AutoStartServer", value.ToString ());
				OnPropertyChanged ("AutoStart");
			}
		}

		public bool AutoLoad {
			get { return autoLoad; }
			set {
				if (value == autoLoad)
					return;
				autoLoad = value;
				Settings.SetValue ("AutoLoadTestSuite", value.ToString ());
				OnPropertyChanged ("AutoLoad");
			}
		}

		public bool UseServer {
			get { return useServer; }
			set {
				if (value == useServer)
					return;
				useServer = value;
				Settings.SetValue ("UseServer", value.ToString ());
				OnPropertyChanged ("UseServer");
			}
		}

		void LoadSettings ()
		{
			string value;
			if (Settings.TryGetValue ("ServerAddress", out value))
				serverAddress = value;

			if (Settings.TryGetValue ("UseServer", out value))
				useServer = bool.Parse (value);

			if (Settings.TryGetValue ("AutoStartServer", out value))
				autoStart = bool.Parse (value);

			if (Settings.TryGetValue ("AutoLoadTestSuite", out value))
				autoLoad = bool.Parse (value);

			OnPropertyChanged ("UseServer");
			OnPropertyChanged ("ServerAddress");
			OnPropertyChanged ("AutoStart");
			OnPropertyChanged ("AutoLoad");
		}

		#endregion

		abstract class ServerCommand : Command<TestProvider>
		{
			public readonly ServerManager Manager;

			public ServerCommand (ServerManager manager)
				: base (manager)
			{
				Manager = manager;
			}

			internal sealed override Task<bool> Run (TestProvider instance, CancellationToken cancellationToken)
			{
				return instance.Run (cancellationToken);
			}

			internal sealed override async Task Stop (TestProvider instance, CancellationToken cancellationToken)
			{
				Manager.StatusMessage = string.Empty;
				await instance.Stop (cancellationToken);
			}
		}

		class LocalCommand : ServerCommand
		{
			public LocalCommand (ServerManager manager)
				: base (manager)
			{
			}

			internal override Task<TestProvider> Start (CancellationToken cancellationToken)
			{
				return Task.FromResult (TestProvider.StartLocal (Manager.App));
			}
		}

		class ConnectCommand : ServerCommand
		{
			public ConnectCommand (ServerManager manager)
				: base (manager)
			{
			}

			internal override Task<TestProvider> Start (CancellationToken cancellationToken)
			{
				return TestProvider.Connect (Manager.App, Manager.ServerAddress, cancellationToken);
			}
		}

		class StartCommand : ServerCommand
		{
			public StartCommand (ServerManager manager)
				: base (manager)
			{
			}

			internal override Task<TestProvider> Start (CancellationToken cancellationToken)
			{
				return TestProvider.StartServer (Manager.App, cancellationToken);
			}
		}
	}
}

