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

namespace Xamarin.AsyncTests.Remoting
{
	using Portable;
	using Framework;

	public class ClientConnection : Connection
	{
		IPortableSupport support;
		TestFramework localFramework;
		TaskCompletionSource<object> startTcs;

		public ClientConnection (TestApp app, Stream stream, IServerConnection connection)
			: base (app, stream)
		{
			support = app.PortableSupport;
			localFramework = app.Framework;
			startTcs = new TaskCompletionSource<object> ();
		}

		public TestFramework LocalFramework {
			get { return localFramework; }
		}

		public IPortableSupport PortableSupport {
			get { return support; }
		}

		protected override bool IsServer {
			get { return false; }
		}

		public async Task StartClient (CancellationToken cancellationToken)
		{
			await Start (cancellationToken);
			await startTcs.Task;
		}

		internal override async Task OnStart (CancellationToken cancellationToken)
		{
			var handshake = new Handshake { WantStatisticsEvents = true, Settings = App.Settings };

			await RemoteObjectManager.Handshake (this, App.Logger.Backend, handshake, cancellationToken);

			await base.Start (cancellationToken);
		}
	}
}

