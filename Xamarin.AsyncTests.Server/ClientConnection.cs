//
// ClientConnection.cs
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
	using Framework;

	public abstract class ClientConnection : Connection
	{
		TestSuite suite;
		TaskCompletionSource<TestSuite> startTcs;

		public bool UseMySettings {
			get;
			private set;
		}

		public bool UseMyTestSuite {
			get;
			private set;
		}

		public ClientConnection (TestApp app, Stream stream, bool useMySettings, bool useMyTests)
			: base (app, stream, false)
		{
			UseMySettings = UseMySettings;
			UseMyTestSuite = useMyTests;
			startTcs = new TaskCompletionSource<TestSuite> ();
		}

		public async Task<TestSuite> StartClient (CancellationToken cancellationToken)
		{
			lock (this) {
				if (suite != null)
					return suite;
			}

			await Start (cancellationToken);
			return await startTcs.Task;
		}

		internal override async Task OnStart (CancellationToken cancellationToken)
		{
			var handshake = new Handshake { WantStatisticsEvents = true };
			if (UseMySettings)
				handshake.Settings = App.Settings;

			if (UseMyTestSuite)
				handshake.TestSuite = await GetLocalTestSuite (cancellationToken);

			await Hello (handshake, cancellationToken);

			await MartinTest ();

			await base.Start (cancellationToken);
		}

		async Task MartinTest ()
		{
			var logger = await RemoteObjectManager.GetRemoteTestLogger (this, CancellationToken.None);
			Debug ("GOT REMOTE LOGGER: {0}", logger);
			logger.LogMessage ("Hello World!");
			Debug ("LOGGER DONE!");
		}

		internal override Task<Handshake> OnHello (Handshake handshake, CancellationToken cancellationToken)
		{
			throw new ServerErrorException ();
		}

		public async Task Hello (Handshake handshake, CancellationToken cancellationToken)
		{
			Debug ("Client Handshake: {0}", handshake);

			var hello = new HelloCommand { Argument = handshake };
			var retval = await hello.Send (this, cancellationToken);

			lock (this) {
				if (retval.Settings != null) {
					App.Settings.Merge (retval.Settings);
					App.Settings.PropertyChanged += OnSettingsChanged;
				}

				if (retval.TestSuite != null)
					suite = retval.TestSuite;
				else if (handshake.TestSuite != null)
					suite = handshake.TestSuite;

				if (suite == null)
					throw new ServerErrorException ();

				App.CurrentTestSuite = suite;
				startTcs.SetResult (suite);
			}

			Debug ("Client Handshake done: {0}", retval);
		}

		protected internal override Task<TestResult> OnRunTestSuite (CancellationToken cancellationToken)
		{
			return OnRun (suite.Test, cancellationToken);
		}

		async void OnSettingsChanged (object sender, PropertyChangedEventArgs e)
		{
			await SyncSettings ((SettingsBag)sender);
		}

		public override void Stop ()
		{
			App.Settings.PropertyChanged -= OnSettingsChanged;
			base.Stop ();
		}

		protected internal override void OnShutdown ()
		{
			App.CurrentTestSuite = null;
			App.Settings.PropertyChanged -= OnSettingsChanged;
			base.OnShutdown ();
		}
	}
}

