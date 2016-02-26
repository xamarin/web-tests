//
// Xamarin.AsyncTests.Mobile.cs
//
// Author:
//       Martin Baulig <martin.baulig@xamarin.com>
//
// Copyright (c) 2015 Xamarin, Inc.
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
using SD = System.Diagnostics;
using Xamarin.AsyncTests;
using Xamarin.AsyncTests.Framework;
using Xamarin.AsyncTests.Remoting;
using Xamarin.AsyncTests.Portable;
using Xamarin.Forms;

namespace Xamarin.AsyncTests.Mobile
{
	public class MobileTestApp : Application, TestApp
	{
		public TestFramework Framework {
			get;
			private set;
		}

		public IPortableEndPoint EndPoint {
			get;
			private set;
		}

		public TestLogger Logger {
			get;
			private set;
		}

		public SettingsBag Settings {
			get;
			private set;
		}

		public Label MainLabel {
			get;
			private set;
		}

		public Label StatusLabel {
			get;
			private set;
		}

		public Label StatisticsLabel {
			get;
			private set;
		}

		public StackLayout Content {
			get;
			private set;
		}

		public Button RunButton {
			get;
			private set;
		}

		public Button StopButton {
			get;
			private set;
		}

		public MobileTestApp (TestFramework framework)
		{
			Framework = framework;

			Settings = SettingsBag.CreateDefault ();
			Settings.LocalLogLevel = -1;

			Logger = new TestLogger (new MobileLogger (this));

			EndPoint = GetEndPoint ();

			MainLabel = new Label { HorizontalTextAlignment = TextAlignment.Start, Text = "Welcome to Xamarin AsyncTests!" };

			StatusLabel = new Label { HorizontalTextAlignment = TextAlignment.Start };

			StatisticsLabel = new Label { HorizontalTextAlignment = TextAlignment.Start };

			RunButton = new Button { Text = "Run" };

			StopButton = new Button { Text = "Stop", IsEnabled = false };

			var buttonLayout = new StackLayout {
				HorizontalOptions = LayoutOptions.Center,
				Children = { RunButton, StopButton }
			};

			Content = new StackLayout {
				VerticalOptions = LayoutOptions.Center,
				Children = { MainLabel, StatusLabel, buttonLayout, StatisticsLabel }
			};

			MainPage = new ContentPage { Content = Content };

			RunButton.Clicked += (s, e) => OnRun ();
			StopButton.Clicked += (sender, e) => OnStop ();
		}

		CancellationTokenSource cts;

		async void OnRun ()
		{
			if (Interlocked.CompareExchange (ref cts, new CancellationTokenSource (), null) != null)
				return;

			try {
				StopButton.IsEnabled = true;
				RunButton.IsEnabled = false;
	
				var cancellationToken = cts.Token;
				var local = await TestServer.StartLocal (this, Framework, cancellationToken);
				MainLabel.Text = "started local server.";

				cancellationToken.ThrowIfCancellationRequested ();
				var session = await local.GetTestSession (cancellationToken);
				MainLabel.Text = string.Format ("Got test session {0}.", session);
				Debug ("GOT SESSION: {0}", session);

				OnResetStatistics ();

				cancellationToken.ThrowIfCancellationRequested ();
				await session.Run (session.RootTestCase, cancellationToken);

				cancellationToken.ThrowIfCancellationRequested ();
				var running = await local.WaitForExit (cancellationToken);
				Debug ("WAIT FOR EXIT: {0}", running);

				await local.Stop (cancellationToken);
			} finally {
				cts.Dispose ();
				cts = null;
				MainLabel.Text = string.Format ("Done running.");
				StopButton.IsEnabled = false;
				RunButton.IsEnabled = true;
			}
		}

		void OnStop ()
		{
			var oldCts = Interlocked.Exchange (ref cts, null);
			if (oldCts == null)
				return;

			oldCts.Cancel ();
		}

		static IPortableEndPoint GetEndPoint ()
		{
			var support = DependencyInjector.Get<IPortableEndPointSupport> ();
			return support.GetEndpoint (8888);
		}

		TestServer server;

		protected override async void OnStart ()
		{
			MainLabel.Text = string.Format ("Server address is {0}:{1}.", EndPoint.Address, EndPoint.Port);

			while (true) {
				server = await TestServer.StartServer (this, EndPoint, Framework, CancellationToken.None);

				var session = await server.GetTestSession (CancellationToken.None);
				MainLabel.Text = string.Format ("Got test session {0}.", session);
				Debug ("GOT SESSION: {0}", session);

				OnResetStatistics ();

				var running = await server.WaitForExit (CancellationToken.None);
				Debug ("WAIT FOR EXIT: {0}", running);

				await server.Stop (CancellationToken.None);

				MainLabel.Text = string.Format ("Done running.");
			}
		}

		protected override void OnSleep ()
		{
			// Handle when your app sleeps
		}

		protected override void OnResume ()
		{
			// Handle when your app resumes
		}

		void Debug (string format, params object[] args)
		{
			Debug (string.Format (format, args));
		}

		void Debug (string message)
		{
			SD.Debug.WriteLine (message);
		}

		void OnLogMessage (string message)
		{
			Debug (message);
		}

		void OnLogDebug (int level, string message)
		{
			if (Settings.LocalLogLevel >= 0 && level > Settings.LocalLogLevel)
				return;
			Debug (message);
		}

		void OnResetStatistics ()
		{
			StatusLabel.Text = string.Empty;
			StatisticsLabel.Text = string.Empty;
			countTests = countSuccess = countErrors = countIgnored = 0;
		}

		int countTests;
		int countSuccess;
		int countErrors;
		int countIgnored;

		void OnStatisticsEvent (TestLoggerBackend.StatisticsEventArgs args)
		{
			switch (args.Type) {
			case TestLoggerBackend.StatisticsEventType.Running:
				++countTests;
				Debug ("Running {0}", args.Name);
				Device.BeginInvokeOnMainThread (() => StatusLabel.Text = string.Format ("Running {0}", args.Name));
				break;
			case TestLoggerBackend.StatisticsEventType.Finished:
				switch (args.Status) {
				case TestStatus.Success:
					++countSuccess;
					break;
				case TestStatus.Ignored:
				case TestStatus.None:
					++countIgnored;
					break;
				default:
					++countErrors;
					break;
				}

				Debug ("Finished {0}: {1}", args.Name, args.Status);
				Device.BeginInvokeOnMainThread (() => {
					StatusLabel.Text = string.Format ("Finished {0}: {1}", args.Name, args.Status);
					StatisticsLabel.Text = string.Format ("{0} test run, {1} ignored, {2} passed, {3} errors.",
						countTests, countIgnored, countSuccess, countErrors);
				});
				break;
			case TestLoggerBackend.StatisticsEventType.Reset:
				Device.BeginInvokeOnMainThread (() => OnResetStatistics ());
				break;
			}
		}

		class MobileLogger : TestLoggerBackend
		{
			readonly MobileTestApp MobileApp;

			public MobileLogger (MobileTestApp app)
			{
				MobileApp = app;
			}

			protected override void OnLogEvent (LogEntry entry)
			{
				switch (entry.Kind) {
				case EntryKind.Debug:
					MobileApp.OnLogDebug (entry.LogLevel, entry.Text);
					break;

				case EntryKind.Error:
					if (entry.Error != null)
						MobileApp.OnLogMessage (string.Format ("ERROR: {0}", entry.Error));
					else
						MobileApp.OnLogMessage (entry.Text);
					break;

				default:
					MobileApp.OnLogMessage (entry.Text);
					break;
				}
			}

			protected override void OnStatisticsEvent (StatisticsEventArgs args)
			{
				MobileApp.OnStatisticsEvent (args);
			}
		}
	}
}

