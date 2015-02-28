//
// TestLoggerClient.cs
//
// Author:
//       Martin Baulig <martin.baulig@xamarin.com>
//
// Copyright (c) 2015 Xamarin, Inc.
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

namespace Xamarin.AsyncTests.Server
{
	class EventSinkClient : ObjectClient<EventSinkClient>, RemoteEventSink
	{
		public Connection Connection {
			get;
			private set;
		}

		public long ObjectID {
			get;
			private set;
		}

		public TestLoggerBackend Backend {
			get;
			private set;
		}

		internal EventSinkClient (Connection connection, long objectID)
		{
			Connection = connection;
			ObjectID = objectID;
		}

		EventSinkClient RemoteObject<EventSinkClient,EventSinkServant>.Client {
			get { return this; }
		}

		EventSinkServant RemoteObject<EventSinkClient,EventSinkServant>.Servant {
			get { throw new ServerErrorException (); }
		}

		public async Task LogEvent (TestLoggerBackend.LogEntry entry, CancellationToken cancellationToken)
		{
			var command = new LogCommand ();
			await command.Send (this, entry, cancellationToken);
		}

		class LogCommand : RemoteObjectCommand<RemoteEventSink,TestLoggerBackend.LogEntry,object>
		{
			protected override Task<object> Run (
				Connection connection, RemoteEventSink proxy,
				TestLoggerBackend.LogEntry argument, CancellationToken cancellationToken)
			{
				return Task.Run<object> (() => {
					proxy.Servant.LogEvent (argument);
					return null;
				});
			}
		}

		class StatisticsCommand : RemoteObjectCommand<RemoteEventSink,TestLoggerBackend.StatisticsEventArgs,object>
		{
			protected override Task<object> Run (
				Connection connection, RemoteEventSink proxy,
				TestLoggerBackend.StatisticsEventArgs argument, CancellationToken cancellationToken)
			{
				return Task.Run<object> (() => {
					proxy.Servant.StatisticsEvent (argument);
					return null;
				});
			}
		}

		internal static Task<EventSinkClient> FromProxy (ObjectProxy proxy, CancellationToken cancellationToken)
		{
			return Task.Run (() => {
				var client = (EventSinkClient)proxy;
				if (client.Backend != null)
					return client;

				client.Backend = new EventSinkBackend (client);
				return client;
			});
		}

		class EventSinkBackend : TestLoggerBackend
		{
			readonly EventSinkClient client;

			public EventSinkBackend (EventSinkClient client)
			{
				this.client = client;
			}

			protected internal override void OnLogEvent (LogEntry entry)
			{
				client.LogEvent (entry, CancellationToken.None).Wait ();
			}

			protected internal override void OnStatisticsEvent (StatisticsEventArgs args)
			{
				;
			}
		}
	}
}

