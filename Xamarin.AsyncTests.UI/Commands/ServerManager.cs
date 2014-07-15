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

	public class ServerManager : CommandProvider<TestServer>
	{
		readonly ConnectCommand connectCommand;
		readonly StartCommand startCommand;

		public Command<TestServer> Connect {
			get { return connectCommand; }
		}

		public Command<TestServer> Start {
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

		#region Server

		TestServer server;
		IServerConnection connection;

		protected async Task<TestServer> OnConnect (CancellationToken cancellationToken)
		{
			await Task.Yield ();

			StatusMessage = "Connecting to server ...";

			connection = await App.ServerHost.Connect (ServerAddress, cancellationToken);

			SetStatusMessage ("Got remote connection ({0}).", connection.Name);

			var stream = await connection.Open (cancellationToken);
			server = new TestServer (App, stream, connection);

			SetStatusMessage ("Connected to server ({0}).", connection.Name);

			return server;
		}

		protected Task OnDisconnect (CancellationToken cancellationToken)
		{
			if (server != null) {
				server.Stop ();
				server = null;
				connection = null;
			}

			if (connection != null) {
				connection.Close ();
				connection = null;
			}

			StatusMessage = "Stopped server.";
			return Task.FromResult<object> (null);
		}

		protected async Task<TestServer> OnStart (CancellationToken cancellationToken)
		{
			await Task.Yield ();

			StatusMessage = "Starting server ...";

			connection = await App.ServerHost.Start (cancellationToken);

			SetStatusMessage ("Server running ({0})", connection.Name);

			var stream = await connection.Open (cancellationToken);
			StatusMessage = "Got remote connection!";

			server = new TestServer (App, stream, connection);
			return server;
		}

		protected async Task<bool> OnRun (CancellationToken cancellationToken)
		{
			if (server != null) {
				await server.Run (cancellationToken);
				StatusMessage = "Server finished";
			}

			return false;
		}

		#endregion

		class ConnectCommand : Command<TestServer>
		{
			public readonly ServerManager Manager;

			public ConnectCommand (ServerManager manager)
				: base (manager)
			{
				Manager = manager;
			}

			internal override Task<TestServer> Start (CancellationToken cancellationToken)
			{
				return Manager.OnConnect (cancellationToken);
			}

			internal override Task<bool> Run (CancellationToken cancellationToken)
			{
				return Manager.OnRun (cancellationToken);
			}

			internal override Task Stop (CancellationToken cancellationToken)
			{
				return Manager.OnDisconnect (cancellationToken);
			}
		}

		class StartCommand : Command<TestServer>
		{
			public readonly ServerManager Manager;

			public StartCommand (ServerManager manager)
				: base (manager)
			{
				Manager = manager;
			}

			internal override Task<TestServer> Start (CancellationToken cancellationToken)
			{
				return Manager.OnStart (cancellationToken);
			}

			internal override Task<bool> Run (CancellationToken cancellationToken)
			{
				return Manager.OnRun (cancellationToken);
			}

			internal override Task Stop (CancellationToken cancellationToken)
			{
				return Manager.OnDisconnect (cancellationToken);
			}
		}
	}
}

