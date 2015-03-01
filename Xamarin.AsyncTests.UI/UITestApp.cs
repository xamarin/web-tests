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

namespace Xamarin.AsyncTests.UI
{
	using Portable;
	using Framework;

	public class UITestApp : TestApp
	{
		public Assembly Assembly {
			get;
			private set;
		}

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

		public IPortableSupport PortableSupport {
			get { return support; }
		}

		public TestFramework Framework {
			get { return framework; }
		}

		public TestLogger Logger {
			get { return logger; }
		}

		public SettingsBag Settings {
			get { return settings; }
		}

		readonly TestLogger logger;
		readonly SettingsBag settings;
		readonly TestFramework framework;
		readonly IPortableSupport support;

		public UITestApp (IPortableSupport support, SettingsBag settings, Assembly assembly)
		{
			this.support = support;
			this.settings = settings;

			Assembly = assembly;

			logger = new TestLogger (new UILogger (this));

			framework = TestFramework.GetLocalFramework (Assembly, Settings);

			ServerManager = new ServerManager (this);

			TestRunner = new TestRunner (this);
		}

		class UILogger : TestLoggerBackend
		{
			readonly UITestApp App;

			public UILogger (UITestApp app)
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

		protected internal void LogDebug (int level, string message)
		{
			if (level > Logger.LogLevel)
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
	}
}
