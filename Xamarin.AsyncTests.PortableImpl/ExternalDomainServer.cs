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
using SD = System.Diagnostics;
using System.Xml.Linq;

namespace Xamarin.AsyncTests.Portable
{
	using Framework;
	using Remoting;

#if __IOS__
	[Foundation.Preserve (AllMembers = true)]
#endif
	class ExternalDomainServer : MarshalByRefObject, TestApp, IExternalDomainServer
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

		public TestFramework Framework {
			get;
		}

		public TestConfiguration Configuration {
			get;
		}

		public TestSession Session {
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
			mainTcs = new TaskCompletionSource<object> ();

#if !__MOBILE__ && !__UNIFIED__ && !__IOS__
			SD.Debug.AutoFlush = true;
			SD.Debug.Listeners.Add (new SD.ConsoleTraceListener ());
#endif

			DependencyInjector.RegisterAssembly (typeof (PortableSupportImpl).Assembly);
			DependencyInjector.RegisterAssembly (Support.Assembly);

			if (Support.DomainSetup != null)
				Support.DomainSetup.Initialize ();

			Framework = TestFramework.GetLocalFramework (PackageName, Support.Assembly, Support.Dependencies);

			Configuration = new TestConfiguration (Framework.ConfigurationProvider, Settings);

			Session = TestSession.CreateLocal (this, Framework);
		}

		TestServer server;
		CancellationTokenSource cts;
		TaskCompletionSource<object> mainTcs;

		[SD.Conditional ("CONNECTION_DEBUG")]
		static void Debug (string message)
		{
			System.Console.Error.WriteLine (message);
		}

		public void Cancel ()
		{
			cts.Cancel ();
		}

		public void Start ()
		{
			TestServer.ConnectToForkedDomain (this, Framework, this, cts.Token).ContinueWith (OnStarted);
		}

		public Task WaitForCompletion ()
		{
			return mainTcs.Task;
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
			Debug ($"Connected to forked parent.");
			Host.OnServerStarted ();

			try {
				cts.Token.ThrowIfCancellationRequested ();

				await server.Session.WaitForShutdown (cts.Token).ConfigureAwait (false);

				Debug ($"Forked child session exited.");

				await server.WaitForExit (cts.Token);

				Debug ($"Forked child done.");

				await server.Stop (cts.Token);

				Debug ($"Forked child exiting.");

				Host.OnCompleted ();
			} catch (OperationCanceledException) {
				mainTcs.TrySetCanceled ();
				Host.OnCanceled ();
			} catch (Exception error) {
				Host.OnError (error);
			} finally {
				mainTcs.TrySetResult (null);
			}
		}

		public string HandleMessage (string message)
		{
			var task = RemotingHelper.HandleMessage (server, message, cts.Token);
			task.Wait ();
			return task.Result;
		}

		public async Task<XElement> SendMessage (XElement element, CancellationToken cancellationToken)
		{
			using (var cts = CancellationTokenSource.CreateLinkedTokenSource (cancellationToken)) {
				cts.Token.Register (Cancel);
				var serialized = TestSerializer.Serialize (element);

				var response = await Task.Run (() => Host.HandleMessage (serialized)).ConfigureAwait (false);
				if (response == null)
					return null;
				return TestSerializer.Deserialize (response);
			}
		}

		bool disposed;

		void Dispose (bool disposing)
		{
			if (disposed)
				return;
			disposed = true;

			Cancel ();
		}

		public void Dispose ()
		{
			Dispose (true);
		}
	}
}
