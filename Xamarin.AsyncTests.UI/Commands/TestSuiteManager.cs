//
// TestSuiteProvider.cs
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

	public class TestSuiteManager : CommandProvider<TestSuite>
	{
		readonly LoadLocalCommand loadLocal;
		readonly LoadFromServerCommand loadFromServer;

		public Command<TestSuite> LoadLocal {
			get { return loadLocal; }
		}

		public Command<TestSuite> LoadFromServer {
			get { return loadFromServer; }
		}

		public TestSuiteManager (TestApp app)
			: base (app)
		{
			loadLocal = new LoadLocalCommand (this);
			loadFromServer = new LoadFromServerCommand (this);

			StatusMessage = "No TestSuite loaded.";
			loadLocal.CanExecute = true;
		}

		protected async Task<TestSuite> OnLoadLocal (CancellationToken cancellationToken)
		{
			try {
				StatusMessage = "Loading TestSuite ...";
				var suite = await TestSuite.LoadAssembly (App.Assembly);
				if (suite.Configuration != null)
					App.Context.Configuration.AddTestSuite (suite.Configuration);
				App.RootTestResult.Result.Test = suite;
				SetStatusMessage ("Loaded {0}.", suite.Name);
				return suite;
			} catch (Exception ex) {
				SetStatusMessage ("Error loading TestSuite: {0}", ex.Message);
				throw;
			}
		}

		protected async Task<TestSuite> OnLoadFromServer (CancellationToken cancellationToken)
		{
			try {
				var instance = App.ServerManager.Instance as TestServer;
				if (instance == null) {
					StatusMessage = "Server not started!";
					return null;
				}
				StatusMessage = "Loading TestSuite from server ...";
				var suite = await instance.LoadTestSuite (cancellationToken);
				if (suite.Configuration != null)
					App.Context.Configuration.AddTestSuite (suite.Configuration);
				App.RootTestResult.Result.Test = suite;
				SetStatusMessage ("Loaded {0} from server.", suite.Name);
				return suite;
			} catch (Exception ex) {
				SetStatusMessage ("Error loading TestSuite: {0}", ex.Message);
				throw;
			}
		}

		protected Task OnStop ()
		{
			StatusMessage = "TestSuite unloaded.";
			App.RootTestResult.Result.Clear ();
			App.TestRunner.CurrentTestResult = App.RootTestResult;
			App.Context.Configuration.Clear ();
			return Task.FromResult<object> (null);
		}

		class LoadLocalCommand : Command<TestSuite>
		{
			public readonly TestSuiteManager Manager;

			public LoadLocalCommand (TestSuiteManager manager)
				: base (manager)
			{
				Manager = manager;
			}

			internal override Task<TestSuite> Start (CancellationToken cancellationToken)
			{
				return Manager.OnLoadLocal (cancellationToken);
			}

			internal override Task<bool> Run (CancellationToken cancellationToken)
			{
				return Task.FromResult (true);
			}

			internal override Task Stop (CancellationToken cancellationToken)
			{
				return Manager.OnStop ();
			}
		}

		class LoadFromServerCommand : Command<TestSuite>
		{
			public readonly TestSuiteManager Manager;

			public LoadFromServerCommand (TestSuiteManager manager)
				: base (manager, manager.App.ServerManager)
			{
				Manager = manager;
			}

			internal override Task<TestSuite> Start (CancellationToken cancellationToken)
			{
				return Manager.OnLoadFromServer (cancellationToken);
			}

			internal override Task<bool> Run (CancellationToken cancellationToken)
			{
				return Task.FromResult (true);
			}

			internal override Task Stop (CancellationToken cancellationToken)
			{
				return Manager.OnStop ();
			}
		}
	}
}

