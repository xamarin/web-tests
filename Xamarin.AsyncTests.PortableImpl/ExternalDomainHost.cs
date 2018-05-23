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
			this.server = server;
			startTcs = new TaskCompletionSource<object> ();
			tcs = new TaskCompletionSource<object> ();
		}

		AppDomain domain;
		ExternalDomainServer server;
		TaskCompletionSource<object> startTcs;
		TaskCompletionSource<object> tcs;

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
			startTcs.TrySetCanceled ();
			tcs.TrySetCanceled ();
		}

		internal void OnError (Exception error)
		{
			startTcs.TrySetException (error);
			tcs.TrySetException (error);
		}

		public async Task Start (IPortableEndPoint address, CancellationToken cancellationToken)
		{
			using (var cts = CancellationTokenSource.CreateLinkedTokenSource (cancellationToken)) {
				cts.Token.Register (() => server.Cancel ());
				server.Start (address);
				await startTcs.Task.ConfigureAwait (false);
			}
		}

		public async Task WaitForCompletion (CancellationToken cancellationToken)
		{
			using (var cts = CancellationTokenSource.CreateLinkedTokenSource (cancellationToken)) {
				cts.Token.Register (() => server.Cancel ());
				await tcs.Task.ConfigureAwait (false);
			}
		}

		public void Cancel ()
		{
			server.Cancel (); 
		}

		bool disposed;

		void Dispose (bool disposing)
		{
			if (disposed)
				return;
			disposed = true;

			if (server != null) {
				server.Cancel ();
				server = null;
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
