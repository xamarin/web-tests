//
// App.cs
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
using System.Text;
using SD = System.Diagnostics;
using System.ComponentModel;
using System.Reflection;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace Xamarin.AsyncTests.UI
{
	using Framework;

	public class TestApp : TestContext
	{
		public Assembly Assembly {
			get;
			private set;
		}

		public TestContext Context {
			get { return this; }
		}

		public TabbedPage Root {
			get;
			private set;
		}

		public TestResultModel RootTestResult {
			get;
			private set;
		}

		string statusMessage;
		public string StatusMessage {
			get { return statusMessage; }
			set {
				statusMessage = value;
				OnPropertyChanged ("StatusMessage");
			}
		}

		public IServerHost ServerHost {
			get;
			private set;
		}

		public OptionsModel Options {
			get;
			private set;
		}

		public ServerManager ServerManager {
			get;
			private set;
		}

		public TestRunner TestRunner {
			get;
			private set;
		}

		public MainPage MainPage {
			get;
			private set;
		}

		public LogPage LogPage {
			get;
			private set;
		}

		public TestApp (SettingsBag settings, IServerHost server, Assembly assembly)
			: base (settings)
		{
			ServerHost = server;
			Assembly = assembly;

			Logger = new UILogger (this);

			var result = new TestResult (new TestName (null));
			RootTestResult = new TestResultModel (this, result, true);

			ServerManager = new ServerManager (this);

			TestRunner = new TestRunner (this);
			TestRunner.CurrentTestResult = RootTestResult;

			MainPage = new MainPage (ServerManager);

			Options = new OptionsModel (this);

			Root = new TabbedPage ();

			var resultPage = new TestResultPage (this, RootTestResult);
			var resultNav = new NavigationPage (resultPage) { Title = resultPage.Title };

			LogPage = new LogPage (this);

			Root.Children.Add (MainPage);
			Root.Children.Add (new OptionsPage (Options));
			Root.Children.Add (resultNav);
			Root.Children.Add (LogPage);

			Initialize ();
		}

		async void Initialize ()
		{
			await Task.Yield ();

			try {
				await ServerManager.Initialize ();
			} catch (Exception ex) {
				Logger.LogError (ex);
			}
		}

		class UILogger : TestLogger
		{
			readonly TestApp App;

			public UILogger (TestApp app)
			{
				App = app;
			}

			protected override void OnLogEvent (LogEntry entry)
			{
				switch (entry.Kind) {
				case LogEntry.EntryKind.Debug:
					App.LogDebug (entry.LogLevel, entry.Text);
					break;

				case LogEntry.EntryKind.Error:
					if (entry.Error != null)
						App.LogMessage (string.Format ("ERROR: {0}", entry.Error));
					else
						App.LogMessage (entry.Text);
					break;

				default:
					App.LogMessage (entry.Text);
					break;
				}
			}
		}

		protected void LogDebug (int level, string message)
		{
			if (level > DebugLevel)
				return;
			SD.Debug.WriteLine (message);
			LogPage.Log (message);
		}

		protected void LogMessage (string message)
		{
			SD.Debug.WriteLine (message);
			LogPage.Log (message);
		}
	}
}
