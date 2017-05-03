//
// UITestApp.cs
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
using AppKit;

namespace Xamarin.AsyncTests.MacUI
{
	using Portable;
	using Framework;

	public class MacUI : TestApp
	{
		public TestApp Context {
			get { return this; }
		}

		public ServerManager ServerManager {
			get;
			private set;
		}

		public TestRunner TestRunner {
			get;
			private set;
		}

		public TestLogger Logger {
			get { return logger; }
		}

		public SettingsBag Settings {
			get { return settings; }
		}

		public string PackageName => null;

		readonly TestLogger logger;
		readonly SettingsBag settings;

		public MacUI ()
		{
			settings = new UISettings ();

			logger = new TestLogger (new UILogger (this));

			ServerManager = new ServerManager (this);

			TestRunner = new TestRunner (this);
		}

		class UILogger : TestLoggerBackend
		{
			readonly MacUI App;

			public UILogger (MacUI app)
			{
				App = app;
			}

			protected override void OnLogEvent (LogEntry entry)
			{
				switch (entry.Kind) {
				case EntryKind.Debug:
					App.LogDebug (entry.LogLevel, entry.Text);
					break;

				case EntryKind.Error:
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

			protected override void OnStatisticsEvent (StatisticsEventArgs args)
			{
				App.TestRunner.OnStatisticsEvent (args);
			}
		}

		internal static void Debug (string message, params object[] args)
		{
			System.Diagnostics.Debug.WriteLine (message, args); 
		}

		protected internal void LogDebug (int level, string message)
		{
			if (Settings.LogLevel >= 0 && level > Settings.LogLevel)
				return;
			SD.Debug.WriteLine (message);
		}

		protected internal void LogMessage (string format, params object[] args)
		{
			LogMessage (string.Format (format, args));
		}

		protected internal void LogMessage (string message)
		{
			SD.Debug.WriteLine (message);
		}

		public static IAppDelegate AppDelegate {
			get { return (IAppDelegate)NSApplication.SharedApplication.Delegate; }
		}

		public static MacUI Instance {
			get { return AppDelegate.MacUI; }
		}
	}
}
