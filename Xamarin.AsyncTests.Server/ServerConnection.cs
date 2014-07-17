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
	using Framework;

	public abstract class ServerConnection : Connection
	{
		TaskCompletionSource<TestSuite> helloTcs;
		TestSuite suite;

		public ServerConnection (TestContext context, Stream stream)
			: base (context, stream, true)
		{
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
				if (handshake.WantStatisticsEvents)
					Context.Statistics.StatisticsEvent += OnStatisticsEvent;

				if (handshake.Settings == null) {
					Context.Settings.PropertyChanged += OnSettingsChanged;
					handshake.Settings = Context.Settings;
				} else {
					Context.Settings.Merge (handshake.Settings);
					handshake.Settings = null;
				}

				if (handshake.TestSuite != null) {
					suite = handshake.TestSuite;
					handshake.TestSuite = null;
				} else {
					suite = localSuite;
					handshake.TestSuite = suite;
				}

				Context.CurrentTestSuite = suite;
				helloTcs.SetResult (suite);
			}

			Debug ("Server Handshake done: {0}", handshake);

			return handshake;
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
			Context.CurrentTestSuite = null;
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

