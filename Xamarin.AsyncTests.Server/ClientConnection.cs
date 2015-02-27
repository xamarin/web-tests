//
// ClientConnection.cs
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
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.ComponentModel;

namespace Xamarin.AsyncTests.Server
{
	using Portable;
	using Framework;

	public class ClientConnection : Connection
	{
		IPortableSupport support;
		TestFramework localFramework;
		TestFramework remoteFramework;
		TaskCompletionSource<TestFramework> startTcs;

		public ClientConnection (TestApp app, Stream stream, IServerConnection connection)
			: base (app, stream)
		{
			support = app.PortableSupport;
			localFramework = app.GetLocalTestFramework ();
			startTcs = new TaskCompletionSource<TestFramework> ();
		}

		public TestFramework LocalFramework {
			get { return localFramework; }
		}

		public IPortableSupport PortableSupport {
			get { return support; }
		}

		public async Task<TestFramework> StartClient (CancellationToken cancellationToken)
		{
			lock (this) {
				if (remoteFramework != null)
					return remoteFramework;
			}

			await Start (cancellationToken);
			return await startTcs.Task;
		}

		internal override async Task OnStart (CancellationToken cancellationToken)
		{
			var handshake = new Handshake { WantStatisticsEvents = true, Settings = App.Settings, Logger = App.Logger };

			await Hello (handshake, cancellationToken);

			await base.Start (cancellationToken);
		}

		public async Task Hello (Handshake handshake, CancellationToken cancellationToken)
		{
			Debug ("Client Handshake: {0}", handshake);

			var hello = new HelloCommand { Argument = handshake };
			await hello.Send (this, cancellationToken);

			cancellationToken.ThrowIfCancellationRequested ();
			var remoteFramework = await RemoteObjectManager.GetRemoteTestFramework (this, cancellationToken);

			Debug ("Client Handshake done: {0}", remoteFramework);

			startTcs.SetResult (remoteFramework);
		}
	}
}

