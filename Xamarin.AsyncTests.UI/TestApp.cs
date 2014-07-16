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
using System.ComponentModel;
using System.Reflection;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace Xamarin.AsyncTests.UI
{
	using Framework;

	public class TestApp : TestContext, INotifyPropertyChanged
	{
		public Assembly Assembly {
			get;
			private set;
		}

		public TestContext Context {
			get { return this; }
		}

		public TabbedPage MainPage {
			get;
			private set;
		}

		public Page Root {
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

		public TestSuiteManager TestSuiteManager {
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

		public ServerControlPage ServerControlPage {
			get;
			private set;
		}

		public TestApp (SettingsBag settings, IServerHost server, Assembly assembly)
			: base (settings)
		{
			ServerHost = server;
			Assembly = assembly;

			var result = new TestResult (new TestName (null));
			RootTestResult = new TestResultModel (this, result, true);

			ServerManager = new ServerManager (this);
			TestSuiteManager = new TestSuiteManager (this);

			TestRunner = new TestRunner (this);
			TestRunner.CurrentTestResult = RootTestResult;

			ServerControlPage = new ServerControlPage (ServerManager);

			Options = new OptionsModel (this);

			MainPage = new TabbedPage ();

			var resultPage = new TestResultPage (this, RootTestResult);
			var resultNav = new NavigationPage (resultPage) { Title = resultPage.Title };

			MainPage.Children.Add (ServerControlPage);
			MainPage.Children.Add (new OptionsPage (Options));
			MainPage.Children.Add (resultNav);

			Root = MainPage;

			Initialize ();
		}

		async void Initialize ()
		{
			await Task.Yield ();

			try {
				await ServerManager.Initialize ();
			} catch (Exception ex) {
				Context.Log ("INITIALIZE EX: {0}", ex);
			}
		}

		public event PropertyChangedEventHandler PropertyChanged;

		protected virtual void OnPropertyChanged (string propertyName)
		{
			if (PropertyChanged != null)
				PropertyChanged (this, new PropertyChangedEventArgs (propertyName));
		}
	}
}
