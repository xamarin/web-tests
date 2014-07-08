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
	public class ServerManager : CommandProvider<TestServer>
	{
		readonly ConnectCommand connectCommand;

		public Command<TestServer> Connect {
			get { return connectCommand; }
		}

		public ServerManager (TestApp app)
			: base (app)
		{
			connectCommand = new ConnectCommand (this);

			CanStart = app.ServerHost != null;

			LoadSettings ();
		}

		#region Options

		string serverAddress;

		public string ServerAddress {
			get { return serverAddress; }
			set {
				serverAddress = value;
				SaveSettings ();
				OnPropertyChanged ("ServerAddress");
			}
		}

		bool autoStart;
		bool autoLoad;

		public bool AutoStart {
			get { return autoStart; }
			set {
				if (value == autoStart)
					return;
				autoStart = value;
				SaveSettings ();
				OnPropertyChanged ("AutoStart");
			}
		}

		public bool AutoLoad {
			get { return autoLoad; }
			set {
				if (value == autoLoad)
					return;
				autoLoad = value;
				SaveSettings ();
				OnPropertyChanged ("AutoLoad");
			}
		}

		void LoadSettings ()
		{
			if (App.SettingsHost == null)
				return;

			serverAddress = App.SettingsHost.GetValue ("ServerAddress") ?? string.Empty;

			var autoStartValue = App.SettingsHost.GetValue ("AutoStartServer");
			if (autoStartValue != null)
				autoStart = bool.Parse (autoStartValue);

			var autoLoadValue = App.SettingsHost.GetValue ("AutoLoadTestSuite");
			if (autoLoadValue != null)
				autoLoad = bool.Parse (autoLoadValue);

			OnPropertyChanged ("UseServer");
			OnPropertyChanged ("ServerAddress");
			OnPropertyChanged ("AutoStart");
			OnPropertyChanged ("AutoLoad");
		}

		void SaveSettings ()
		{
			if (App.SettingsHost == null)
				return;

			App.SettingsHost.SetValue ("ServerAddress", ServerAddress);
			App.SettingsHost.SetValue ("AutoStartServer", AutoStart.ToString ());
			App.SettingsHost.SetValue ("AutoLoadTestSuite", AutoLoad.ToString ());
		}

		#endregion

		#region Server

		TestServer server;
		IServerConnection connection;

		protected async Task<TestServer> OnConnect (CancellationToken cancellationToken)
		{
			await Task.Yield ();

			StatusMessage = "Starting server ...";

			connection = await App.ServerHost.Connect (
				ServerAddress, cancellationToken).ConfigureAwait (false);

			StatusMessage = "Got remote connection.";

			var stream = await connection.Open (cancellationToken);
			server = new TestServer (App, stream, connection);
			server.Run ();

			await server.Hello (CancellationToken.None);

			StatusMessage = "Server running.";

			return server;
		}

		protected Task OnDisconnect (CancellationToken cancellationToken)
		{
			return Task.Run (() => {
				if (server != null) {
					server.Stop ();
					server = null;
				}

				App.Context.Configuration.Clear ();

				StatusMessage = "Stopped server.";
			});
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

			internal override Task Stop (CancellationToken cancellationToken)
			{
				return Manager.OnDisconnect (cancellationToken);
			}
		}
	}
}

