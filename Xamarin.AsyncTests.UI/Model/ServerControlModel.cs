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
	public class ServerControlModel : BindableObject
	{
		public TestApp App {
			get;
			private set;
		}

		bool canRun;
		bool isRunning;
		string statusMessage;

		public bool CanRun {
			get { return canRun; }
			set {
				if (canRun == value)
					return;
				canRun = value;
				OnPropertyChanged ("CanRun");
			}
		}

		public bool IsRunning {
			get { return isRunning; }
			set {
				if (isRunning == value)
					return;
				isRunning = value;
				OnPropertyChanged ("IsRunning");
			}
		}

		public string StatusMessage {
			get { return statusMessage; }
			set {
				statusMessage = value;
				OnPropertyChanged ("StatusMessage");
			}
		}

		public ServerControlModel (TestApp app)
		{
			App = app;

			CanRun = app.ServerHost != null;
		}

		CancellationTokenSource serverCts;
		IServerConnection connection;
		TestServer server;

		public async Task Connect ()
		{
			CancellationToken token;
			lock (this) {
				if (!CanRun || serverCts != null)
					return;
				serverCts = new CancellationTokenSource ();
				token = serverCts.Token;
			}

			try {
				CanRun = false;
				IsRunning = true;
				connection = await App.ServerHost.Connect (App.Options.ServerAddress, token);
				OnPropertyChanged ("CanLoad");
				StatusMessage = "Started server!";

				var stream = await connection.Open (token).ConfigureAwait (false);
				server = new TestServer (App, stream, connection);
				StatusMessage = "Got remote connection!";
				IsRunning = true;
			} catch (OperationCanceledException) {
				Disconnect ("Server connection canceled!");
				return;
			} catch (Exception ex) {
				App.Context.Log ("SERVER ERROR: {0}", ex);
				Disconnect (string.Format ("Server error: {0}", ex.Message));
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
			IsRunning = false;
			CanRun = App.ServerHost != null;
			StatusMessage = message ?? "Disconnected.";
		}
	}
}

