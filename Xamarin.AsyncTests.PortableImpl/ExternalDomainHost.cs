//
// ExternalDomainEventSink.cs
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
using System.Xml.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Xamarin.AsyncTests.Portable
{
	using Framework;
	using Portable;
	using Remoting;

	class ExternalDomainHost : MarshalByRefObject, IExternalDomainHost
	{
		public TestApp App {
			get;
		}

		public string PackageName => App.PackageName;

		public ExternalDomainHost (TestApp app)
		{
			App = app;
		}

		public void Initialize (AppDomain domain, ExternalDomainServer server)
		{
			this.domain = domain;
			this.domainServer = server;
			startTcs = new TaskCompletionSource<object> ();
			tcs = new TaskCompletionSource<object> ();
			cts = new CancellationTokenSource ();
		}

		AppDomain domain;
		TestServer localServer;
		ExternalDomainServer domainServer;
		TaskCompletionSource<object> startTcs;
		TaskCompletionSource<object> tcs;
		CancellationTokenSource cts;

		internal void OnServerStarted ()
		{
			startTcs.TrySetResult (null);
		}

		internal void OnCompleted ()
		{
			startTcs.TrySetResult (null);
			tcs.TrySetResult (null);
		}

		internal void OnCanceled ()
		{
			cts.Cancel ();
			startTcs.TrySetCanceled ();
			tcs.TrySetCanceled ();
		}

		internal void OnError (Exception error)
		{
			startTcs.TrySetException (error);
			tcs.TrySetException (error);
		}

		public string HandleMessage (string message)
		{
			var task = RemotingHelper.HandleMessage (localServer, message, cts.Token);
			task.Wait ();
			return task.Result;
		}

		public async Task<XElement> SendMessage (XElement element, CancellationToken cancellationToken)
		{
			using (var cts = CancellationTokenSource.CreateLinkedTokenSource (cancellationToken)) {
				cts.Token.Register (domainServer.Cancel);
				var serialized = TestSerializer.Serialize (element);

				var response = await Task.Run (() => domainServer.HandleMessage (serialized)).ConfigureAwait (false);
				if (response == null)
					return null;
				return TestSerializer.Deserialize (response);
			}
		}

		public async Task Start (TestServer server, CancellationToken cancellationToken)
		{
			localServer = server;
			using (var cts = CancellationTokenSource.CreateLinkedTokenSource (cancellationToken)) {
				cts.Token.Register (domainServer.Cancel);
				domainServer.Start ();
				await startTcs.Task.ConfigureAwait (false);
			}
		}

		public async Task WaitForCompletion (CancellationToken cancellationToken)
		{
			using (var cts = CancellationTokenSource.CreateLinkedTokenSource (cancellationToken)) {
				cts.Token.Register (domainServer.Cancel);
				await tcs.Task.ConfigureAwait (false);
			}
		}

		public void Cancel ()
		{
			cts.Cancel ();
			domainServer.Cancel (); 
		}

		bool disposed;

		void Dispose (bool disposing)
		{
			if (disposed)
				return;
			disposed = true;

			if (domainServer != null) {
				domainServer.Cancel ();
				domainServer = null;
			}
			if (cts != null) {
				cts.Dispose ();
				cts = null;
			}
			if (domain != null) {
				AppDomain.Unload (domain);
				domain = null;
			}
		}

		public void Dispose ()
		{
			Dispose (true);
		}
	}
}
