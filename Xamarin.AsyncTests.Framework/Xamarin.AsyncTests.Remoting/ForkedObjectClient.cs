//
// ForkedObjectClient.cs
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
using System.Linq;
using System.Xml.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Xamarin.AsyncTests.Remoting
{
	class ForkedObjectClient : ObjectClient<ForkedObjectClient>, RemoteForkedObject, IForkedObjectClient
	{
		public IServerConnection ServerConnection {
			get;
		}

		public Connection Connection => ServerConnection.Connection;

		public long ObjectID {
			get;
		}

		public string Type => "RemoteObject";

		ForkedObjectClient RemoteObject<ForkedObjectClient, ForkedObjectServant>.Client => this;

		ForkedObjectServant RemoteObject<ForkedObjectClient, ForkedObjectServant>.Servant => throw new ServerErrorException ();

		internal ForkedObjectClient (IServerConnection connection, long objectID)
		{
			ServerConnection = connection;
			ObjectID = objectID;
		}

		public async Task<XElement> SendMessage (string message, XElement body, CancellationToken cancellationToken)
		{
			var command = new MessageCommand ();
			var element = new XElement ("ForkedObjectMessage");
			element.Add (new XAttribute ("Message", message));
			if (body != null)
				element.Add (body);
			return await command.Send (this, element, cancellationToken).ConfigureAwait (false);
		}

		public async Task SendOneWayMessage (string message, XElement body, CancellationToken cancellationToken)
		{
			var command = new OneWayMessageCommand ();
			var element = new XElement ("ForkedObjectOneWayMessage");
			element.Add (new XAttribute ("Message", message));
			if (body != null)
				element.Add (body);
			await command.Send (this, element, cancellationToken).ConfigureAwait (false);
		}

		public async Task<TestResult> RunRemoteTest (TestPath path, CancellationToken cancellationToken)
		{
			var command = new RunRemoteTestCommand ();
			var serialized = path.SerializePath ();
			var result = await command.Send (this, serialized, cancellationToken).ConfigureAwait (false);
			Connection.Debug ($"REMOTE TEST DONE: {result}");
			return result;
		}

		class MessageCommand : RemoteObjectCommand<RemoteForkedObject, XElement, XElement>
		{
			public override bool IsOneWay => false;

			protected override Task<XElement> Run (
				Connection connection, RemoteForkedObject proxy,
				XElement argument, CancellationToken cancellationToken)
			{
				var message = argument.Attribute ("Message").Value;
				XElement body = null;
				if (argument.HasElements)
					body = argument.Elements ().First ();
				return proxy.Servant.HandleMessage (message, body, cancellationToken);
			}
		}

		class OneWayMessageCommand : RemoteObjectCommand<RemoteForkedObject, XElement, object>
		{
			public override bool IsOneWay => true;

			protected async override Task<object> Run (
				Connection connection, RemoteForkedObject proxy,
				XElement argument, CancellationToken cancellationToken)
			{
				var message = argument.Attribute ("Message").Value;
				XElement body = null;
				if (argument.HasElements)
					body = argument.Elements ().First ();
				await proxy.Servant.HandleMessage (message, body, cancellationToken).ConfigureAwait (false);
				return null;
			}
		}

		class RunRemoteTestCommand : RemoteObjectCommand<RemoteForkedObject, XElement, TestResult>
		{
			protected override async Task<TestResult> Run (
				Connection connection, RemoteForkedObject proxy,
				XElement argument, CancellationToken cancellationToken)
			{
				var path = TestPath.Read (argument);
				return await proxy.Servant.RunRemoteCommand (argument, cancellationToken).ConfigureAwait (false);
			}
		}
	}
}
