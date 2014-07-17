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

		public ClientConnection (TestContext context, Stream stream, bool useMySettings, bool useMyTests)
			: base (context, stream, false)
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
				handshake.Settings = Context.Settings;

			if (UseMyTestSuite)
				handshake.TestSuite = await GetLocalTestSuite (cancellationToken);

			await Hello (handshake, cancellationToken);

			if (Context.DebugLevel >= 0)
				await SetLogLevel (Context.DebugLevel, cancellationToken);

			await base.Start (cancellationToken);
		}

		internal override Task<Handshake> OnHello (Handshake handshake, CancellationToken cancellationToken)
		{
			throw new InvalidOperationException ();
		}

		public async Task Hello (Handshake handshake, CancellationToken cancellationToken)
		{
			Debug ("Client Handshake: {0}", handshake);

			var hello = new HelloCommand { Argument = handshake };
			var retval = await hello.Send (this, cancellationToken);

			lock (this) {
				if (retval.Settings != null) {
					Context.Settings.Merge (retval.Settings);
					Context.Settings.PropertyChanged += OnSettingsChanged;
				}

				if (handshake.WantStatisticsEvents)
					Context.Statistics.StatisticsEvent += OnStatisticsEvent;

				if (retval.TestSuite != null)
					suite = retval.TestSuite;
				else if (handshake.TestSuite != null)
					suite = handshake.TestSuite;

				if (suite == null)
					throw new InvalidOperationException ();

				Context.CurrentTestSuite = suite;
				startTcs.SetResult (suite);
			}

			Debug ("Client Handshake done: {0}", retval);
		}

		protected internal override Task<TestResult> OnRunTestSuite (CancellationToken cancellationToken)
		{
			return OnRun (suite, cancellationToken);
		}

		async void OnSettingsChanged (object sender, PropertyChangedEventArgs e)
		{
			await SyncSettings ((SettingsBag)sender);
		}

		async void OnStatisticsEvent (object sender, TestStatistics.StatisticsEventArgs e)
		{
			if (e.IsRemote)
				return;
			await new NotifyStatisticsEventCommand { Argument = e }.Send (this);
		}

		public override void Stop ()
		{
			Context.Settings.PropertyChanged -= OnSettingsChanged;
			Context.Statistics.StatisticsEvent -= OnStatisticsEvent;
			base.Stop ();
		}

		protected internal override void OnShutdown ()
		{
			Context.CurrentTestSuite = null;
			Context.Settings.PropertyChanged -= OnSettingsChanged;
			Context.Statistics.StatisticsEvent -= OnStatisticsEvent;
			base.OnShutdown ();
		}
	}
}

