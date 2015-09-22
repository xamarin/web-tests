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

namespace Xamarin.AsyncTests.Remoting
{
	using Framework;

	interface RemoteEventSink : RemoteObject<EventSinkClient,EventSinkServant>
	{
	}

	interface RemoteTestSession : RemoteObject<TestSessionClient,TestSessionServant>
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
			var objectID = long.Parse (node.Attribute ("ObjectID").Value);

			C client;
			if (connection.TryGetRemoteObject (objectID, out client))
				return client;

			client = createClientFunc (objectID);
			connection.RegisterObjectClient (client);
			return client;
		}

		internal static XElement WriteProxy (ObjectProxy proxy)
		{
			var element = new XElement (proxy.Type);
			element.SetAttributeValue ("ObjectID", proxy.ObjectID);
			return element;
		}

		internal static ObjectProxy ReadTestCase (TestSessionClient session, XElement node)
		{
			return ReadProxy (session.Connection, node, (objectID) => new TestCaseClient (session, objectID));
		}

		static Handshake ReadHandshake (Connection connection, XElement node)
		{
			if (!node.Name.LocalName.Equals ("Handshake"))
				throw new ServerErrorException ();

			var instance = new Handshake ();
			instance.WantStatisticsEvents = bool.Parse (node.Attribute ("WantStatisticsEvents").Value);

			var settings = node.Element ("Settings");
			Connection.Debug ("Handshake: {0}", settings);
			instance.Settings = TestSerializer.ReadSettings (settings);

			var logger = node.Element ("EventSink");
			instance.EventSink = ReadProxy (connection, logger, (objectID) => new EventSinkClient ((ServerConnection)connection, objectID));

			return instance;
		}

		static XElement WriteHandshake (Connection connection, Handshake instance)
		{
			var element = new XElement ("Handshake");
			element.SetAttributeValue ("WantStatisticsEvents", instance.WantStatisticsEvents);

			element.Add (TestSerializer.WriteSettings (instance.Settings));

			element.Add (WriteProxy (instance.EventSink));

			return element;
		}

		class HandshakeCommand : Command<Handshake,object>
		{
			protected override XElement WriteArgument (Connection connection, Handshake instance)
			{
				return WriteHandshake (connection, instance);
			}

			protected override Handshake ReadArgument (Connection connection, XElement node)
			{
				return ReadHandshake (connection, node);
			}

			protected async override Task<object> Run (Connection connection, Handshake argument, CancellationToken cancellationToken)
			{
				var serverConnection = (ServerConnection)connection;
				await serverConnection.Initialize (argument, cancellationToken);
				return null;
			}
		}

		internal static async Task Handshake (ClientConnection connection, TestLoggerBackend logger, Handshake handshake, CancellationToken cancellationToken)
		{
			Connection.Debug ("Client Handshake: {0}", handshake);

			handshake.EventSink = new EventSinkServant (connection, logger);

			var handshakeCommand = new HandshakeCommand ();
			await handshakeCommand.Send (connection, handshake, cancellationToken);

			cancellationToken.ThrowIfCancellationRequested ();

			Connection.Debug ("Client Handshake done");
		}

		class GetRemoteTestSessionCommand : Command<object,ObjectProxy>
		{
			protected override ObjectProxy ReadResponse (Connection connection, XElement node)
			{
				var clientConnection = (ClientConnection)connection;
				return ReadProxy (connection, node, (objectID) => new TestSessionClient (clientConnection, objectID));
			}

			protected override Task<ObjectProxy> Run (Connection connection, object argument, CancellationToken cancellationToken)
			{
				return Task.FromResult<ObjectProxy> (new TestSessionServant ((ServerConnection)connection));
			}
		}

		public static async Task<TestSession> GetRemoteTestSession (
			ClientConnection connection, CancellationToken cancellationToken)
		{
			var command = new GetRemoteTestSessionCommand ();
			var proxy = await command.Send (connection, cancellationToken);
			return await TestSessionClient.FromProxy (proxy, cancellationToken);
		}

		class GetRemoteTestSuiteCommand : RemoteObjectCommand<RemoteTestSession,object,ObjectProxy>
		{
			protected override ObjectProxy ReadResponse (Connection connection, XElement node)
			{
				return ReadProxy (connection, node, (objectID) => new TestSuiteClient (Proxy.Client, objectID));
			}

			protected override Task<ObjectProxy> Run (Connection connection, RemoteTestSession proxy, object argument, CancellationToken cancellationToken)
			{
				return Task.FromResult<ObjectProxy> (new TestSuiteServant ((ServerConnection)connection, proxy.Servant));
			}
		}

		public static async Task<TestSuiteClient> GetRemoteTestSuite (
			TestSessionClient session, CancellationToken cancellationToken)
		{
			var command = new GetRemoteTestSuiteCommand ();
			return (TestSuiteClient)await command.Send (session, null, cancellationToken);
		}

		class GetRemoteTestConfigurationCommand : RemoteObjectCommand<RemoteTestSession,object,XElement>
		{
			protected override Task<XElement> Run (Connection connection, RemoteTestSession proxy, object argument, CancellationToken cancellationToken)
			{
				return Task.FromResult (proxy.Servant.GetConfiguration ());
			}
		}

		public static async Task<ITestConfigurationProvider> GetRemoteTestConfiguration (
			TestSessionClient session, CancellationToken cancellationToken)
		{
			var command = new GetRemoteTestConfigurationCommand ();
			var result = await command.Send (session, null, cancellationToken);
			return TestSerializer.ReadConfiguration (result);
		}

		class GetRootTestCaseCommand : RemoteObjectCommand<RemoteTestSession,object,ObjectProxy>
		{
			protected override ObjectProxy ReadResponse (Connection connection, XElement node)
			{
				return ReadTestCase (Proxy.Client, node);
			}

			protected override Task<ObjectProxy> Run (
				Connection connection, RemoteTestSession proxy, object argument, CancellationToken cancellationToken)
			{
				var servant = new TestCaseServant ((ServerConnection)connection, proxy.Servant, proxy.Servant.RootTestCase);
				return Task.FromResult<ObjectProxy> (servant);
			}
		}

		public static async Task<TestCaseClient> GetRootTestCase (TestSessionClient session, CancellationToken cancellationToken)
		{
			var command = new GetRootTestCaseCommand ();
			var proxy = await command.Send (session, null, cancellationToken);

			return await TestCaseClient.FromProxy (proxy, cancellationToken);
		}

		class ResolveFromPathCommand : RemoteObjectCommand<RemoteTestSession,XElement,ObjectProxy>
		{
			protected override ObjectProxy ReadResponse (Connection connection, XElement node)
			{
				return ReadTestCase (Proxy.Client, node);
			}

			protected override async Task<ObjectProxy> Run (
				Connection connection, RemoteTestSession proxy, XElement argument, CancellationToken cancellationToken)
			{
				var test = await proxy.Servant.ResolveFromPath (argument, cancellationToken);
				return new TestCaseServant ((ServerConnection)connection, proxy.Servant, test);
			}
		}

		public static async Task<TestCase> ResolveFromPath (
			TestSessionClient session, XElement path, CancellationToken cancellationToken)
		{
			var command = new ResolveFromPathCommand ();
			var proxy = await command.Send (session, path, cancellationToken);

			return await TestCaseClient.FromProxy (proxy, cancellationToken);
		}

		class UpdateSettingsCommand : RemoteObjectCommand<RemoteTestSession,XElement,object>
		{
			protected override Task<object> Run (Connection connection, RemoteTestSession proxy, XElement argument, CancellationToken cancellationToken)
			{
				Connection.Debug ("Update settings: {0}", argument);
				proxy.Servant.UpdateSettings (argument);
				return Task.FromResult<object> (null);
			}
		}

		public static async Task UpdateSettings (TestSessionClient session, CancellationToken cancellationToken)
		{
			var settings = TestSerializer.WriteSettings (session.App.Settings);
			var command = new UpdateSettingsCommand ();
			await command.Send (session, settings, cancellationToken);
		}
	}
}

