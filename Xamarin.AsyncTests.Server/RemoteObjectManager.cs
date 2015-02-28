//
// RemoteObjectManager.cs
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
using System.Xml.Linq;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Xamarin.AsyncTests.Server
{
	using Framework;

	interface RemoteTestLogger : RemoteObject<TestLoggerClient,TestLoggerServant>
	{
	}

	interface RemoteTestFramework : RemoteObject<TestFrameworkClient,TestFrameworkServant>
	{
	}

	interface RemoteTestSuite : RemoteObject<TestSuiteClient,TestSuiteServant>
	{
	}

	interface RemoteTestCase : RemoteObject<TestCaseClient,TestCaseServant>
	{
	}

	static class RemoteObjectManager
	{
		static C ReadProxy<C> (Connection connection, XElement node, Func<long,C> createClientFunc)
			where C : class, ObjectProxy
		{
			if (!node.Name.LocalName.Equals ("ObjectID"))
				throw new ServerErrorException ();

			var objectID = long.Parse (node.Attribute ("Value").Value);

			C client;
			if (connection.TryGetRemoteObject (objectID, out client))
				return client;

			client = createClientFunc (objectID);
			connection.RegisterObjectClient (client);
			return client;
		}

		internal static XElement WriteProxy (ObjectProxy proxy)
		{
			var element = new XElement ("ObjectID");
			element.SetAttributeValue ("Value", proxy.ObjectID);
			return element;
		}

		internal static ObjectProxy ReadTestCase (TestSuiteClient suite, XElement node)
		{
			return ReadProxy (suite.Connection, node, (objectID) => new TestCaseClient (suite, objectID));
		}

		public static async Task LogEvent (
			TestLoggerClient proxy, TestLoggerBackend.LogEntry entry, CancellationToken cancellationToken)
		{
			var command = new LogCommand ();
			await command.Send (proxy, entry, cancellationToken);
		}

		class LogCommand : RemoteObjectCommand<RemoteTestLogger,TestLoggerBackend.LogEntry,object>
		{
			protected override Task<object> Run (
				Connection connection, RemoteTestLogger proxy,
				TestLoggerBackend.LogEntry argument, CancellationToken cancellationToken)
			{
				return Task.Run<object> (() => {
					proxy.Servant.LogEvent (argument);
					return null;
				});
			}
		}

		class StatisticsCommand : RemoteObjectCommand<RemoteTestLogger,TestLoggerBackend.StatisticsEventArgs,object>
		{
			protected override Task<object> Run (
				Connection connection, RemoteTestLogger proxy,
				TestLoggerBackend.StatisticsEventArgs argument, CancellationToken cancellationToken)
			{
				return Task.Run<object> (() => {
					proxy.Servant.StatisticsEvent (argument);
					return null;
				});
			}
		}

		class GetRemoteTestFrameworkCommand : Command<object,ObjectProxy>
		{
			protected override ObjectProxy ReadResponse (Connection connection, XElement node)
			{
				var clientConnection = (ClientConnection)connection;
				return ReadProxy (connection, node, (objectID) => new TestFrameworkClient (clientConnection, objectID));
			}

			protected override Task<ObjectProxy> Run (Connection connection, object argument, CancellationToken cancellationToken)
			{
				return Task.FromResult<ObjectProxy> (new TestFrameworkServant ((ServerConnection)connection));
			}
		}

		public static async Task<TestFramework> GetRemoteTestFramework (
			ClientConnection connection, CancellationToken cancellationToken)
		{
			var command = new GetRemoteTestFrameworkCommand ();
			return (TestFrameworkClient)await command.Send (connection, cancellationToken);
		}

		class GetRemoteTestSuiteCommand : RemoteObjectCommand<RemoteTestFramework,object,ObjectProxy>
		{
			protected override ObjectProxy ReadResponse (Connection connection, XElement node)
			{
				return ReadProxy (connection, node, (objectID) => new TestSuiteClient (Proxy.Client, objectID));
			}

			protected override async Task<ObjectProxy> Run (Connection connection, RemoteTestFramework proxy, object argument, CancellationToken cancellationToken)
			{
				var serverConnection = (ServerConnection)connection;
				var suite = new TestSuiteServant (serverConnection, proxy.Servant);
				await suite.Initialize (serverConnection.Logger, cancellationToken);
				return suite;
			}
		}

		public static async Task<TestSuite> GetRemoteTestSuite (
			TestFrameworkClient framework, CancellationToken cancellationToken)
		{
			var command = new GetRemoteTestSuiteCommand ();
			return (TestSuiteClient)await command.Send (framework, null, cancellationToken);
		}

		class ResolveTestSuiteCommand : RemoteObjectCommand<RemoteTestSuite,object,ObjectProxy>
		{
			protected override ObjectProxy ReadResponse (Connection connection, XElement node)
			{
				return ReadTestCase (Proxy.Client, node);
			}

			protected override async Task<ObjectProxy> Run (
				Connection connection, RemoteTestSuite proxy, object argument, CancellationToken cancellationToken)
			{
				var test = await proxy.Servant.Resolve (cancellationToken);
				return new TestCaseServant ((ServerConnection)connection, proxy.Servant, test);
			}
		}

		public static async Task<TestCase> ResolveTestSuite (
			TestSuiteClient suite, CancellationToken cancellationToken)
		{
			var command = new ResolveTestSuiteCommand ();
			var proxy = await command.Send (suite, null, cancellationToken);

			return await TestCaseClient.FromProxy (proxy, cancellationToken);
		}
	}
}

