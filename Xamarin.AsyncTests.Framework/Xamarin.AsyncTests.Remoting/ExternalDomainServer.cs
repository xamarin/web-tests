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
using System.IO;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Xamarin.AsyncTests.Remoting
{
	using Framework;

	class ExternalDomainServer : Connection, IServerConnection
	{
		IExternalDomainServer server;
		EventSinkClient eventSink;

		protected override bool IsServer => true;

		Connection IServerConnection.Connection => this;

		SettingsBag IServerConnection.Settings => App.Settings;

		EventSinkClient IServerConnection.EventSink => eventSink;

		public TestFramework Framework {
			get;
		}

		public TestLoggerBackend LocalLogger {
			get;
		}

		public ExternalDomainServer (TestApp app, TestFramework framework, IExternalDomainServer server)
			: base (app)
		{
			Framework = framework;
			this.server = server;
			LocalLogger = app.Logger?.Backend;
		}

		protected override Task MainLoop ()
		{
			return server.WaitForCompletion ();
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
				if (server != null) {
					server.Dispose ();
					server = null;
				}
			} catch {
				;
			}
		}

		protected override async Task SendMessage (Message message)
		{
			var element = message.Write (this);

			var response = await server.SendMessage (element, cancelCts.Token).ConfigureAwait (false);
			if (response == null)
				return;

			var objectID = response.Attribute ("ObjectID").Value;
			var operation = GetResponse (long.Parse (objectID));
			operation.Response.Read (this, response);
			operation.Task.SetResult (true);
		}
	}
}
