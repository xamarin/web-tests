//
// ServerConnection.cs
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
using System.ComponentModel;

namespace Xamarin.AsyncTests.Server
{
	using Portable;
	using Framework;

	public class ServerConnection : Connection
	{
		IServerConnection connection;
		TaskCompletionSource<TestSuite> helloTcs;
		TestSuite suite;

		public ServerConnection (TestApp context, Stream stream, IServerConnection connection)
			: base (context, stream, true)
		{
			this.connection = connection;
			helloTcs = new TaskCompletionSource<TestSuite> ();
		}

		public async Task<TestSuite> StartServer (CancellationToken cancellationToken)
		{
			lock (this) {
				if (suite != null)
					return suite;
			}

			await Start (cancellationToken);
			return await helloTcs.Task;
		}

		internal override async Task<Handshake> OnHello (Handshake handshake, CancellationToken cancellationToken)
		{
			Debug ("Server Handshake: {0}", handshake);

			TestSuite localSuite = null;
			if (handshake.TestSuite == null)
				localSuite = await GetLocalTestSuite (cancellationToken);

			lock (this) {
				if (handshake.Settings == null) {
					App.Settings.PropertyChanged += OnSettingsChanged;
					handshake.Settings = App.Settings;
				} else {
					App.Settings.Merge (handshake.Settings);
					handshake.Settings = null;
				}

				if (handshake.TestSuite != null) {
					suite = handshake.TestSuite;
					handshake.TestSuite = null;
				} else {
					suite = localSuite;
					handshake.TestSuite = suite;
				}

				App.CurrentTestSuite = suite;
				helloTcs.SetResult (suite);
			}

			Debug ("Server Handshake done: {0}", handshake);

			return handshake;
		}

		async void OnSettingsChanged (object sender, PropertyChangedEventArgs e)
		{
			await SyncSettings ((SettingsBag)sender);
		}

		public override void Stop ()
		{
			App.CurrentTestSuite = null;
			App.Settings.PropertyChanged -= OnSettingsChanged;

			try {
				base.Stop ();
			} catch {
				;
			}

			try {
				if (connection != null) {
					connection.Close ();
					connection = null;
				}
			} catch {
				;
			}
		}

		protected internal override void OnShutdown ()
		{
			App.CurrentTestSuite = null;
			App.Settings.PropertyChanged -= OnSettingsChanged;
			base.OnShutdown ();
		}

		#region implemented abstract members of Connection

		protected internal override void OnLogMessage (string message)
		{
			throw new NotImplementedException ();
		}

		protected override void OnDebug (int level, string message)
		{
			throw new NotImplementedException ();
		}

		protected internal override Task<TestSuite> GetLocalTestSuite (CancellationToken cancellationToken)
		{
			throw new NotImplementedException ();
		}

		protected internal override Task<TestResult> OnRunTestSuite (CancellationToken cancellationToken)
		{
			throw new NotImplementedException ();
		}

		#endregion
	}
}

