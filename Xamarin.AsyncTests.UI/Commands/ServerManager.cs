//
// ServerManager.cs
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
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Xamarin.AsyncTests;
using Xamarin.AsyncTests.Framework;
using Xamarin.Forms;

namespace Xamarin.AsyncTests.UI
{
	using Framework;
	using Server;

	public class ServerManager : CommandProvider<TestProvider>
	{
		readonly LocalCommand localCommand;
		readonly ConnectCommand connectCommand;
		readonly StartCommand startCommand;

		public Command<TestProvider> Local {
			get { return localCommand; }
		}

		public Command<TestProvider> Connect {
			get { return connectCommand; }
		}

		public Command<TestProvider> Start {
			get { return startCommand; }
		}

		public SettingsBag Settings {
			get;
			private set;
		}

		public static readonly BindableProperty TestSuiteProperty =
			BindableProperty.Create ("TestSuite", typeof(TestSuite), typeof(ServerManager), null,
				propertyChanged: (bo, o, n) => ((ServerManager)bo).OnTestSuiteChanged ((TestSuite)n));

		public ITestSuite TestSuite {
			get { return (TestSuite)GetValue(TestSuiteProperty); }
			set { SetValue (TestSuiteProperty, value); }
		}

		public static readonly BindableProperty HasTestSuiteProperty =
			BindableProperty.Create ("HasTestSuite", typeof(bool), typeof(ServerManager), false);

		public bool HasTestSuite {
			get { return (bool)GetValue (HasTestSuiteProperty); }
			set { SetValue (HasTestSuiteProperty, value); }
		}

		public TestFeaturesModel Features {
			get;
			private set;
		}

		public TestCategoriesModel Categories {
			get;
			private set;
		}

		public ServerManager (TestApp app)
			: base (app)
		{
			Settings = app.Settings;

			localCommand = new LocalCommand (this);
			connectCommand = new ConnectCommand (this);
			startCommand = new StartCommand (this);

			Features = new TestFeaturesModel (App);
			Categories = new TestCategoriesModel (App);

			connectCommand.CanExecute = app.ServerHost != null;
			startCommand.CanExecute = app.ServerHost != null;

			serverAddress = string.Empty;
			Settings.PropertyChanged += (sender, e) => LoadSettings ();
			LoadSettings ();
		}

		internal async Task Initialize ()
		{
			if (AutoStart) {
				if (useServer)
					await Start.Execute ();
				else
					await Connect.Execute ();
			}
		}

		#region Options

		string serverAddress;

		public string ServerAddress {
			get { return serverAddress; }
			set {
				serverAddress = value;
				Settings.SetValue ("ServerAddress", value);
				OnPropertyChanged ("ServerAddress");
			}
		}

		bool autoStart;
		bool useServer;

		public bool AutoStart {
			get { return autoStart; }
			set {
				if (value == autoStart)
					return;
				autoStart = value;
				Settings.SetValue ("AutoStartServer", value.ToString ());
				OnPropertyChanged ("AutoStart");
			}
		}

		public bool UseServer {
			get { return useServer; }
			set {
				if (value == useServer)
					return;
				useServer = value;
				Settings.SetValue ("UseServer", value.ToString ());
				OnPropertyChanged ("UseServer");
			}
		}

		void LoadSettings ()
		{
			string value;
			if (Settings.TryGetValue ("ServerAddress", out value))
				serverAddress = value;

			if (Settings.TryGetValue ("UseServer", out value))
				useServer = bool.Parse (value);

			if (Settings.TryGetValue ("AutoStartServer", out value))
				autoStart = bool.Parse (value);

			OnPropertyChanged ("UseServer");
			OnPropertyChanged ("ServerAddress");
			OnPropertyChanged ("AutoStart");
		}

		#endregion

		protected void OnTestSuiteChanged (TestSuite suite)
		{
			if (suite == null) {
				Features.Configuration = null;
				Categories.Configuration = null;

				App.RootTestResult.Result.Clear ();
				App.TestRunner.CurrentTestResult = App.RootTestResult;
			} else {
				Features.Configuration = suite.Configuration;
				Categories.Configuration = suite.Configuration;

				App.RootTestResult.Result.Test = suite;
			}

			HasTestSuite = suite != null;
		}

		protected async Task<bool> OnRun (TestProvider instance, CancellationToken cancellationToken)
		{
			var suite = await instance.LoadTestSuite (cancellationToken);
			TestSuite = suite;
			return await instance.Run (cancellationToken);
		}

		protected async Task OnStop (TestProvider instance, CancellationToken cancellationToken)
		{
			TestSuite = null;
			SetStatusMessage ("Server stopped.");
			await instance.Stop (cancellationToken);
		}

		abstract class ServerCommand : Command<TestProvider>
		{
			public readonly ServerManager Manager;

			public ServerCommand (ServerManager manager)
				: base (manager)
			{
				Manager = manager;
			}

			internal sealed override Task<bool> Run (TestProvider instance, CancellationToken cancellationToken)
			{
				return Manager.OnRun (instance, cancellationToken);
			}

			internal sealed override Task Stop (TestProvider instance, CancellationToken cancellationToken)
			{
				return Manager.OnStop (instance, cancellationToken);
			}
		}

		class LocalCommand : ServerCommand
		{
			public LocalCommand (ServerManager manager)
				: base (manager)
			{
			}

			internal override Task<TestProvider> Start (CancellationToken cancellationToken)
			{
				return Task.FromResult (TestProvider.StartLocal (Manager.App));
			}
		}

		class ConnectCommand : ServerCommand
		{
			public ConnectCommand (ServerManager manager)
				: base (manager)
			{
			}

			internal override Task<TestProvider> Start (CancellationToken cancellationToken)
			{
				return TestProvider.Connect (Manager.App, Manager.ServerAddress, cancellationToken);
			}
		}

		class StartCommand : ServerCommand
		{
			public StartCommand (ServerManager manager)
				: base (manager)
			{
			}

			internal override Task<TestProvider> Start (CancellationToken cancellationToken)
			{
				return TestProvider.StartServer (Manager.App, cancellationToken);
			}
		}
	}
}

