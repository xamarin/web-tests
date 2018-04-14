//
// ExternalDomainServer.cs
//
// Author:
//       Martin Baulig <mabaul@microsoft.com>
//
// Copyright (c) 2018 Xamarin Inc. (http://www.xamarin.com)
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

namespace Xamarin.AsyncTests.Console
{
	using Framework;
	using Portable;
	using Remoting;

	class ExternalDomainServer : MarshalByRefObject, TestApp
	{
		public ExternalDomainSupport Support {
			get;
		}

		public ExternalDomainHost Host {
			get;
		}

		public string PackageName => Host.PackageName;

		TestLogger TestApp.Logger => null;

		public SettingsBag Settings {
			get;
		}

		internal ExternalDomainServer (
			ExternalDomainSupport support,
			ExternalDomainHost eventSink)
		{
			Support = support;
			Host = eventSink;
			Settings = SettingsBag.CreateDefault ();
			cts = new CancellationTokenSource ();
		}

		TestServer server;
		TestFramework framework;
		CancellationTokenSource cts;

		public void Cancel ()
		{
			cts.Cancel ();
		}

		public void Start (IPortableEndPoint address)
		{
			if (framework != null)
				throw new InternalErrorException ();

			DependencyInjector.RegisterAssembly (typeof (PortableSupportImpl).Assembly);
			DependencyInjector.RegisterAssembly (Support.Assembly);

			framework = TestFramework.GetLocalFramework (
				PackageName, Support.Assembly,
				Support.Dependencies);

			Program.Debug ($"START: {AppDomain.CurrentDomain.FriendlyName} {framework}!");

			TestServer.ConnectToForkedParent (
				this, address, framework, cts.Token).ContinueWith (OnStarted);
		}

		async void OnStarted (Task<TestServer> task)
		{
			if (task.IsFaulted) {
				Host.OnError (task.Exception);
				return;
			}
			if (task.IsCanceled) {
				Host.OnCanceled ();
				return;
			}
			server = task.Result;
			Program.Debug ($"Connected to forked parent.");
			Host.OnServerStarted ();

			try {
				cts.Token.ThrowIfCancellationRequested ();

				await server.WaitForExit (cts.Token);

				Program.Debug ($"Forked child done.");

				await server.Stop (cts.Token);

				Program.Debug ($"Forked child exiting.");

				Host.OnCompleted ();
			} catch (OperationCanceledException) {
				Host.OnCanceled ();
			} catch (Exception error) {
				Host.OnError (error);
			}
		}
	}
}
