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

		public MobileTestApp (TestFramework framework)
		{
			Framework = framework;

			Settings = SettingsBag.CreateDefault ();
			Logger = new TestLogger (new MobileLogger (this));

			EndPoint = GetEndPoint ();

			// The root page of your application
			MainPage = new ContentPage {
				Content = new StackLayout {
					VerticalOptions = LayoutOptions.Center,
					Children = {
						new Label {
							XAlign = TextAlignment.Center,
							Text = "Welcome to Xamarin Forms!"
						}
					}
				}
			};
		}

		static IPortableEndPoint GetEndPoint ()
		{
			var support = DependencyInjector.Get<IPortableEndPointSupport> ();
			return support.GetEndpoint (8888);
		}

		protected override async void OnStart ()
		{
			var server = await TestServer.StartServer (this, EndPoint, Framework, CancellationToken.None);
			var session = server.GetTestSession (CancellationToken.None);
			Debug ("GOT SESSION: {0}", session);

			var running = await server.WaitForExit (CancellationToken.None);
			Debug ("WAIT FOR EXIT: {0}", running);
		}

		protected override void OnSleep ()
		{
			// Handle when your app sleeps
		}

		protected override void OnResume ()
		{
			// Handle when your app resumes
		}

		static void Debug (string message, params object[] args)
		{
			SD.Debug.WriteLine (message, args);
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
				break;
			case TestLoggerBackend.StatisticsEventType.Reset:
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

