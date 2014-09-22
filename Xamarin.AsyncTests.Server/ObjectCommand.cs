//
// ObjectCommand.cs
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
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace Xamarin.AsyncTests.Server
{
	abstract class ObjectCommand<C,S,A,R> : Command
	{
		A argument;
		RemoteObject<C,S>.ClientProxy clientProxy;
		RemoteObject<C,S>.ServerProxy serverProxy;

		protected abstract Serializer<A> ArgumentSerializer {
			get;
		}

		protected abstract Serializer<R> ResponseSerializer {
			get;
		}

		public async Task<R> Send (RemoteObject<C,S>.ClientProxy proxy, A argument, CancellationToken cancellationToken)
		{
			CommandResponse response = null;
			if (!IsOneWay)
				response = new CommandResponse (this);

			this.clientProxy = proxy;
			this.argument = argument;

			await proxy.Connection.SendCommand (this, response, cancellationToken);
			if (IsOneWay)
				return default(R);
			if (response.Error != null)
				throw new SavedException (response.Error);
			if (!response.Success)
				throw new ServerErrorException ();
			return response.Response;
		}

		public override void Read (Connection connection, XElement node)
		{
			base.Read (connection, node);

			var argElement = node.Element ("Argument");
			if (ArgumentSerializer != null && argElement != null) {
				if (argElement.Elements ().Count () > 1)
					throw new ServerErrorException ();
				var first = argElement.Elements ().First ();
				argument = ArgumentSerializer.Read (connection, first);
			}

			var instanceID = long.Parse (node.Attribute ("InstanceID").Value);
			if (!connection.TryGetRemoteObject (instanceID, out serverProxy))
				throw new ServerErrorException ();
		}

		public override void Write (Connection connection, XElement node)
		{
			base.Write (connection, node);

			node.SetAttributeValue ("InstanceID", clientProxy.ObjectID);

			if (ArgumentSerializer != null && argument != null) {
				var element = ArgumentSerializer.Write (connection, argument);
				if (element != null) {
					var argElement = new XElement ("Argument");
					argElement.Add (element);
					node.Add (argElement);
				}
			}
		}

		public sealed override async Task<Response> Run (Connection connection, CancellationToken cancellationToken)
		{
			var response = new CommandResponse (this);

			try {
				response.Response = await Run (connection, serverProxy.Instance, argument, cancellationToken);
				response.Success = true;
			} catch (Exception ex) {
				response.Error = ex.ToString ();
				Connection.Debug ("COMMAND FAILED: {0}", ex);
			}

			return response;
		}

		protected abstract Task<R> Run (Connection connection, S server,
			A argument, CancellationToken cancellationToken);

		class CommandResponse : Response
		{
			public ObjectCommand<C,S,A,R> Command {
				get;
				private set;
			}

			public R Response {
				get; set;
			}

			public CommandResponse (ObjectCommand<C,S,A,R> command)
			{
				Command = command;
				ObjectID = command.ResponseID;
			}

			public override void Read (Connection connection, XElement node)
			{
				base.Read (connection, node);

				var response = node.Element ("Response");
				if (Command.ResponseSerializer != null && response != null) {
					if (response.Elements ().Count () > 1)
						throw new ServerErrorException ();
					var first = response.Elements ().First ();
					Response = Command.ResponseSerializer.Read (connection, first);
				}
			}

			public override void Write (Connection connection, XElement node)
			{
				base.Write (connection, node);
				if (Command.ResponseSerializer != null && Response != null) {
					var element = Command.ResponseSerializer.Write (connection, Response);
					if (element != null) {
						var response = new XElement ("Response");
						response.Add (element);
						node.Add (response);
					}
				}
			}

			public override string ToString ()
			{
				return string.Format ("[CommandResponse: Command={0}, Response={1}]", Command, base.ToString ());
			}
		}
	}
}

