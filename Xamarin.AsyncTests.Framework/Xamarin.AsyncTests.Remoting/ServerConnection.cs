﻿//
// ServerConnection.cs
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
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.ComponentModel;

namespace Xamarin.AsyncTests.Remoting
{
	using Portable;
	using Framework;

	class ServerConnection : SocketConnection, IServerConnection
	{
		TestFramework framework;
		SocketListener listener;
		SocketClient client;
		EventSinkClient eventSink;

		Connection IServerConnection.Connection => this;

		EventSinkClient IServerConnection.EventSink => eventSink;

		SettingsBag IServerConnection.Settings => App.Settings;

		internal EventSinkClient EventSink {
			get { return eventSink; }
		}

		public TestFramework Framework {
			get { return framework; }
		}

		public TestLogger Logger {
			get { return eventSink.LoggerClient; }
		}

		public TestLoggerBackend LocalLogger {
			get;
		}

		protected override bool IsServer {
			get { return true; }
		}

		public ServerConnection (TestApp app, TestFramework framework, SocketListener listener)
			: this (app, framework, listener.Stream)
		{
			this.listener = listener;
		}

		public ServerConnection (TestApp app, TestFramework framework, SocketClient client)
			: this (app, framework, client.Stream)
		{
			this.client = client;
		}

		ServerConnection (TestApp app, TestFramework framework, Stream stream)
			: base (app, stream)
		{
			LocalLogger = app.Logger?.Backend;
			this.framework = framework;
		}

		public async Task Initialize (Handshake handshake, CancellationToken cancellationToken)
		{
			Debug ($"Server Handshake: {handshake}");

			eventSink = await EventSinkClient.FromProxy (handshake.EventSink, cancellationToken);

			if (handshake.Settings != null)
				App.Settings.Merge (handshake.Settings);

			Debug ("Server Handshake done");
		}

		public override void Stop ()
		{
			try {
				base.Stop ();
			} catch {
				;
			}

			try {
				if (listener != null) {
					listener.Dispose ();
					listener = null;
				}
				if (client != null) {
					client.Dispose ();
					client = null;
				}
			} catch {
				;
			}
		}
	}
}

