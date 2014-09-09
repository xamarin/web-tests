//
// RemoteTestLogger.cs
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
using System.Threading;
using System.Threading.Tasks;

namespace Xamarin.AsyncTests.Server
{
	class RemoteTestLogger : RemoteObject<TestLogger, TestLoggerBackend>
	{
		internal static ServerProxy CreateServer (Connection connection)
		{
			return new ServerProxy (connection, new RemoteTestLogger ());
		}

		internal static ClientProxy CreateClient (Connection connection, long objectId)
		{
			return new ClientProxy (connection, new RemoteTestLogger (), objectId);
		}

		protected override TestLogger CreateClientProxy (ClientProxy proxy)
		{
			var backend = new LoggerClient (proxy);
			return new TestLogger (backend);
		}

		protected override TestLoggerBackend CreateServerProxy (Connection connection)
		{
			return new LoggerServer (connection);
		}

		class LoggerClient : TestLoggerBackend
		{
			readonly ClientProxy proxy;

			public LoggerClient (ClientProxy proxy)
			{
				this.proxy = proxy;
			}

			async protected internal override void OnLogEvent (LogEntry entry)
			{
				var command = new LogCommand ();
				await command.Send (proxy, entry, CancellationToken.None);
			}

			protected internal override void OnStatisticsEvent (StatisticsEventArgs args)
			{
			}
		}

		class LoggerServer : TestLoggerBackend
		{
			readonly Connection connection;

			public LoggerServer (Connection connection)
			{
				this.connection = connection;
			}

			protected internal override void OnLogEvent (LogEntry entry)
			{
				System.Diagnostics.Debug.WriteLine ("ON LOG EVENT: {0}", entry);
			}

			protected internal override void OnStatisticsEvent (StatisticsEventArgs args)
			{
				System.Diagnostics.Debug.WriteLine ("ON STATISTICS EVENT: {0}", args);
			}
		}

		class LogCommand : ObjectCommand<TestLogger,TestLoggerBackend,TestLoggerBackend.LogEntry,object>
		{
			protected override Serializer<TestLoggerBackend.LogEntry> ArgumentSerializer {
				get { return Serializer.LogEntry; }
			}

			protected override Serializer<object> ResponseSerializer {
				get { return null; }
			}

			protected override Task<object> Run (
				Connection connection, TestLoggerBackend server,
				TestLoggerBackend.LogEntry argument, CancellationToken cancellationToken)
			{
				return Task.Run<object> (() => {
					server.OnLogEvent (argument);
					return null;
				});
			}
		}

		class StatisticsCommand : ObjectCommand<TestLogger,TestLoggerBackend,TestLoggerBackend.StatisticsEventArgs,object>
		{
			protected override Serializer<TestLoggerBackend.StatisticsEventArgs> ArgumentSerializer {
				get { return Serializer.StatisticsEventArgs; }
			}

			protected override Serializer<object> ResponseSerializer {
				get { return null; }
			}

			protected override Task<object> Run (
				Connection connection, TestLoggerBackend server,
				TestLoggerBackend.StatisticsEventArgs argument, CancellationToken cancellationToken)
			{
				return Task.Run<object> (() => {
					server.OnStatisticsEvent (argument);
					return null;
				});
			}
		}
	}
}

