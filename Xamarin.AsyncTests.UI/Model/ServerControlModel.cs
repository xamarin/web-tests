//
// ServerControlModel.cs
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
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace Xamarin.AsyncTests.UI
{
	using Framework;

	public class ServerControlModel : BindableObject
	{
		public TestApp App {
			get;
			private set;
		}

		string serverAddress;

		public string ServerAddress {
			get { return serverAddress; }
			set {
				serverAddress = value;
				SaveSettings ();
				OnPropertyChanged ("ServerAddress");
			}
		}

		bool canConnect;
		bool isConnected;
		string statusMessage;

		public bool CanConnect {
			get { return canConnect; }
			set {
				if (canConnect == value)
					return;
				canConnect = value;
				OnPropertyChanged ("CanConnect");
			}
		}

		public bool IsConnected {
			get { return isConnected; }
			set {
				if (isConnected == value)
					return;
				isConnected = value;
				OnPropertyChanged ("IsConnected");
			}
		}

		public string StatusMessage {
			get { return statusMessage; }
			set {
				statusMessage = value;
				OnPropertyChanged ("StatusMessage");
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

		public ServerControlModel (TestApp app)
		{
			App = app;

			CanConnect = app.ServerHost != null;
			CanLoad = true;

			LoadSettings ();
		}

		CancellationTokenSource serverCts;
		IServerConnection connection;
		TestServer server;

		public async Task<bool> Connect ()
		{
			CancellationToken token;
			lock (this) {
				if (!CanConnect || serverCts != null)
					return false;
				serverCts = new CancellationTokenSource ();
				token = serverCts.Token;
				CanLoad = false;
			}

			try {
				CanConnect = false;
				IsConnected = true;
				connection = await App.ServerHost.Connect (ServerAddress, token);
				OnPropertyChanged ("CanLoad");
				StatusMessage = "Started server!";

				var stream = await connection.Open (token).ConfigureAwait (false);
				server = new TestServer (App, stream, connection);
				server.Run ();
				StatusMessage = "Got remote connection!";
				IsConnected = true;

				if (AutoLoad)
					await LoadTestSuite ();
				else
					CanLoad = true;

				return true;
			} catch (OperationCanceledException) {
				Disconnect ("Server connection canceled!");
				return false;
			} catch (Exception ex) {
				App.Context.Log ("SERVER ERROR: {0}", ex);
				Disconnect (string.Format ("Server error: {0}", ex.Message));
				return false;
			}
		}

		public void Disconnect (string message = null)
		{
			lock (this) {
				if (serverCts != null) {
					serverCts.Cancel ();
					serverCts.Dispose ();
				}
				serverCts = null;
			}
			if (server != null) {
				server.Stop ();
				server = null;
			}
			if (connection != null) {
				connection.Close ();
				connection = null;
			}
			CanLoad = false;
			IsConnected = false;
			CanConnect = App.ServerHost != null;
			StatusMessage = message ?? "Disconnected.";
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

		public TestSuite TestSuite {
			get { return suite; }
		}

		public bool HasTestSuite {
			get { return suite != null; }
		}

		public bool CanLoad {
			get { return canLoad; }
			set {
				if (canLoad == value)
					return;
				canLoad = value;
				OnPropertyChanged ("CanLoad");
			}
		}

		bool canLoad;
		TestSuite suite;

		public async Task<TestSuite> LoadTestSuite ()
		{
			lock (this) {
				if (!canLoad)
					return null;
				CanLoad = false;
			}

			if (serverCts == null)
				suite = await TestSuite.LoadAssembly (App.Assembly);
			else
				suite = await server.LoadTestSuite (CancellationToken.None);

			if (suite == null)
				return null;

			if (suite.Configuration != null)
				App.Context.Configuration.AddTestSuite (suite.Configuration);
			App.RootTestResult.Result.Test = suite;
			StatusMessage = string.Format ("Successfully loaded {0}.", suite.Name);

			OnPropertyChanged ("HasTestSuite");
			return suite;
		}

		public void UnloadTestSuite ()
		{
			lock (this) {
				if (suite == null)
					return;
				suite = null;
			}

			App.RootTestResult.Result.Clear ();
			App.CurrentTestRunner = App.RootTestRunner;
			App.Context.Configuration.Clear ();
			OnPropertyChanged ("HasTestSuite");
			CanLoad = true;
		}

		public async Task Initialize ()
		{
			if (AutoStart) {
				if (!await Connect ())
					return;
			}

			if (AutoLoad)
				await LoadTestSuite ();
		}
	}
}

