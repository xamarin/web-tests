//
// TestServer.cs
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

namespace Xamarin.AsyncTests.UI
{
	using Framework;
	using Server;

	public class TestServer : ServerConnection
	{
		public TestApp App {
			get;
			private set;
		}

		public override TestContext Context {
			get { return App.Context; }
		}

		public IServerConnection Connection {
			get;
			private set;
		}

		public TestServer (TestApp app, Stream stream, IServerConnection connection)
			: base (stream)
		{
			App = app;
			Connection = connection;
		}

		public void Run ()
		{
			Context.Configuration.PropertyChanged += OnConfigChanged;

			Task.Factory.StartNew (async () => {
				try {
					await MainLoop ().ConfigureAwait (false);
				} catch {
					;
				} finally {
					Context.Configuration.Clear ();
					App.ServerControl.Disconnect ("Server terminated.");
				}
			});
		}

		public override void Stop ()
		{
			Context.Configuration.PropertyChanged -= OnConfigChanged;
			try {
				base.Stop ();
			} catch {
				;
			}
			try {
				Connection.Close ();
			} catch {
				;
			}
		}

		void Debug (string message, params object[] args)
		{
			System.Diagnostics.Debug.WriteLine (message, args);
		}

		#region implemented abstract members of ServerConnection

		protected override Task OnLoadResult (TestResult result)
		{
			return Task.Run (() => {
				App.RootTestResult.Result.AddChild (result);
			});
		}

		protected override void OnMessage (string message)
		{
			Debug ("MESSAGE: {0}", message);
		}

		protected override void OnDebug (int level, string message)
		{
			Debug ("DEBUG ({0}): {1}", level, message);
		}

		protected override Task OnHello (CancellationToken cancellationToken)
		{
			return Task.Run (() => {
				Debug ("HELLO WORLD!");
			});
		}

		bool suppressConfigChanged;

		async void OnConfigChanged (object sender, PropertyChangedEventArgs args)
		{
			if (suppressConfigChanged)
				return;
			await SyncConfiguration (Context.Configuration, false);
		}

		protected override void OnSyncConfiguration (TestConfiguration configuration, bool fullUpdate)
		{
			try {
				suppressConfigChanged = true;
				Context.Configuration.Merge (configuration, fullUpdate);
			} finally {
				suppressConfigChanged = false;
			}
		}

		#endregion
	}
}

