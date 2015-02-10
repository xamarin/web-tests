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
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Xamarin.AsyncTests;
using Xamarin.AsyncTests.Framework;

namespace Xamarin.AsyncTests.UI
{
	using Framework;
	using Server;

	public class ServerManager : CommandProvider<TestServer>
	{
		readonly LocalCommand localCommand;
		readonly ConnectCommand connectCommand;
		readonly StartCommand startCommand;

		public Command<TestServer> Local {
			get { return localCommand; }
		}

		public Command<TestServer> Connect {
			get { return connectCommand; }
		}

		public Command<TestServer> Start {
			get { return startCommand; }
		}

		public SettingsBag Settings {
			get;
			private set;
		}

		public static readonly BindableProperty TestSuiteProperty =
			BindableProperty.Create ("TestSuite", typeof(TestSuite), typeof(ServerManager), null,
				propertyChanged: (bo, o, n) => ((ServerManager)bo).OnTestSuiteChanged ((TestSuite)n));

		public TestSuite TestSuite {
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

		public ServerManager (UITestApp app)
			: base (app)
		{
			Settings = app.Settings;

			localCommand = new LocalCommand (this);
			connectCommand = new ConnectCommand (this);
			startCommand = new StartCommand (this);

			Features = new TestFeaturesModel (App);
			Categories = new TestCategoriesModel (App);

			connectCommand.CanExecute = app.PortableSupport.ServerHost != null;
			startCommand.CanExecute = app.PortableSupport.ServerHost != null;

			serverAddress = string.Empty;
			Settings.PropertyChanged += (sender, e) => LoadSettings ();
			LoadSettings ();
		}

		internal async Task Initialize ()
		{
			switch (StartupAction) {
			case StartupActionKind.LoadLocal:
				await Local.Execute ();
				break;
			case StartupActionKind.Start:
				await Start.Execute ();
				break;
			case StartupActionKind.Connect:
				await Connect.Execute ();
				break;
			default:
				break;
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

		void LoadSettings ()
		{
			string value;
			if (Settings.TryGetValue ("ServerAddress", out value))
				serverAddress = value;

			if (Settings.TryGetValue ("StartupAction", out value))
				Enum.TryParse<StartupActionKind> (value, out startupAction);

			OnPropertyChanged ("UseServer");
			OnPropertyChanged ("ServerAddress");
			OnPropertyChanged ("AutoStart");
		}

		#endregion

		#region Startup Action

		StartupActionKind startupAction;

		public StartupActionKind StartupAction {
			get { return startupAction; }
			set {
				if (value == startupAction)
					return;
				startupAction = value;
				Settings.SetValue ("StartupAction", value.ToString ());
				OnPropertyChanged ("StartupAction");
			}
		}

		public enum StartupActionKind {
			Nothing,
			LoadLocal,
			Start,
			Connect,
		}

		#endregion

		protected void OnTestSuiteChanged (TestSuite suite)
		{
			if (suite == null) {
				App.RootTestResult.Result.Clear ();
				App.TestRunner.CurrentTestResult = App.RootTestResult;
			} else {
				App.RootTestResult.Result.Test = suite.Test;
			}

			HasTestSuite = suite != null;
		}

		protected async Task<bool> OnRun (TestServer instance, CancellationToken cancellationToken)
		{
			var suite = await instance.LoadTestSuite (cancellationToken);
			TestSuite = suite;
			return await instance.Run (cancellationToken);
		}

		protected async Task OnStop (TestServer instance, CancellationToken cancellationToken)
		{
			TestSuite = null;
			SetStatusMessage ("Server stopped.");
			await instance.Stop (cancellationToken);
		}

		abstract class ServerCommand : Command<TestServer>
		{
			public readonly ServerManager Manager;

			public ServerCommand (ServerManager manager)
				: base (manager)
			{
				Manager = manager;
			}

			internal sealed override Task<bool> Run (TestServer instance, CancellationToken cancellationToken)
			{
				return Manager.OnRun (instance, cancellationToken);
			}

			internal sealed override Task Stop (TestServer instance, CancellationToken cancellationToken)
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

			internal override Task<TestServer> Start (CancellationToken cancellationToken)
			{
				return Task.FromResult (TestServer.StartLocal (Manager.App));
			}
		}

		class ConnectCommand : ServerCommand
		{
			public ConnectCommand (ServerManager manager)
				: base (manager)
			{
			}

			internal override Task<TestServer> Start (CancellationToken cancellationToken)
			{
				return TestServer.Connect (Manager.App, Manager.ServerAddress, cancellationToken);
			}
		}

		class StartCommand : ServerCommand
		{
			public StartCommand (ServerManager manager)
				: base (manager)
			{
			}

			internal override Task<TestServer> Start (CancellationToken cancellationToken)
			{
				return TestServer.StartServer (Manager.App, cancellationToken);
			}
		}
	}
}

