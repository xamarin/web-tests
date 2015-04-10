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

namespace Xamarin.AsyncTests.MacUI
{
	using Framework;
	using Remoting;

	public class ServerManager : CommandProvider<TestServer>
	{
		readonly ServerCommand startCommand;

		public Command<TestServer,ServerParameters> Start {
			get { return startCommand; }
		}

		public readonly InstanceProperty<TestSession> TestSession = new InstanceProperty<TestSession> ("TestSession", null);

		public ServerManager (MacUI app)
			: base (app, null)
		{
			startCommand = new ServerCommand (this);
		}

		protected async Task<bool> OnRun (TestServer instance, CancellationToken cancellationToken)
		{
			TestSession.Value = instance.Session;
			SetStatusMessage ("Got test session from server: {0}", instance.Session);
			return await instance.WaitForExit (cancellationToken).ConfigureAwait (false);
		}

		protected async Task OnStop (TestServer instance, CancellationToken cancellationToken)
		{
			TestSession.Value = null;
			SetStatusMessage ("Server stopped.");
			if (instance != null)
				await instance.Stop (cancellationToken);
		}

		class ServerCommand : Command<TestServer,ServerParameters>
		{
			public readonly ServerManager Manager;

			public ServerCommand (ServerManager manager)
				: base (manager, manager.NotifyCanExecute)
			{
				Manager = manager;
			}

			internal override Task<TestServer> Start (ServerParameters parameters, CancellationToken cancellationToken)
			{
				switch (parameters.Mode) {
				case ServerMode.WaitForConnection:
					return TestServer.WaitForConnection (Manager.App, parameters.EndPoint, cancellationToken);
				case ServerMode.Android:
				case ServerMode.iOS:
					return TestServer.ConnectToServer (Manager.App, parameters.EndPoint, cancellationToken);
				case ServerMode.Local:
					return TestServer.CreatePipe (Manager.App, parameters.EndPoint, parameters.Arguments, cancellationToken);
				case ServerMode.Builtin:
					return StartBuiltin (cancellationToken);
				default:
					throw new InternalErrorException ();
				}
			}

			Task<TestServer> StartBuiltin (CancellationToken cancellationToken)
			{
				var builtin = DependencyInjector.Get<IBuiltinTestServer> ();
				return builtin.Start (cancellationToken);
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
	}
}

