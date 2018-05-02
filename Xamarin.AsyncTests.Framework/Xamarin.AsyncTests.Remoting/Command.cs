//
// Command.cs
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

namespace Xamarin.AsyncTests.Remoting
{
	abstract class Command : Message
	{
		public virtual bool IsOneWay {
			get { return false; }
		}

		public long ResponseID {
			get;
			internal set;
		}

		internal static Command Create (Connection connection, XElement node)
		{
			if (!node.Name.LocalName.Equals ("Command"))
				throw new ServerErrorException ();

			var typeName = node.Attribute ("Type");
			var type = Type.GetType (typeName.Value);

			var command = (Command)Activator.CreateInstance (type);
			command.Read (connection, node);

			return command;
		}

		public override void Read (Connection connection, XElement node)
		{
			base.Read (connection, node);

			if (!IsOneWay)
				ResponseID = long.Parse (node.Attribute ("ResponseID").Value);
		}

		public override void Write (Connection connection, XElement node)
		{
			base.Write (connection, node);

			node.SetAttributeValue ("Type", GetType ().FullName);

			if (!IsOneWay)
				node.SetAttributeValue ("ResponseID", ResponseID);
		}

		public abstract Task<Response> Run (Connection connection, CancellationToken cancellationToken);

		protected virtual string MyToString ()
		{
			return null;
		}

		public override string ToString ()
		{
			var my = MyToString ();
			if (my != null)
				my = ", " + my;
			return string.Format ("[{0}: IsOneWay={1}, ResponseID={2}{3}]", GetType ().Name, IsOneWay, ResponseID, my);
		}
	}

	abstract class Command<T,U> : Command
	{
		public T Argument {
			get;
			private set;
		}

		protected virtual XElement WriteResponse (Connection connection, U instance)
		{
			return Serializer.Write (instance);
		}

		protected virtual U ReadResponse (Connection connection, XElement node)
		{
			return Serializer.Read<U> (node);
		}

		protected virtual XElement WriteArgument (Connection connection, T instance)
		{
			return Serializer.Write (instance);
		}

		protected virtual T ReadArgument (Connection connection, XElement node)
		{
			return Serializer.Read<T> (node);
		}

		public Task<U> Send (Connection connection, T argument, CancellationToken cancellationToken)
		{
			Argument = argument;
			return Send (connection, cancellationToken);
		}

		public async Task<U> Send (Connection connection, CancellationToken cancellationToken)
		{
			CommandResponse response = null;
			if (!IsOneWay)
				response = new CommandResponse (this);
			await connection.SendCommand (this, response, cancellationToken);
			if (IsOneWay)
				return default(U);
			if (response.Error != null)
				throw new SavedException (response.Error);
			if (!response.Success)
				throw new ServerErrorException ();
			return response.Response;
		}

		public override void Read (Connection connection, XElement node)
		{
			base.Read (connection, node);

			var argument = node.Element ("Argument");
			if (argument != null) {
				if (argument.Elements ().Count () > 1)
					throw new ServerErrorException ();
				var first = argument.Elements ().First ();
				Argument = ReadArgument (connection, first);
			}
		}

		public override void Write (Connection connection, XElement node)
		{
			base.Write (connection, node);

			if (Argument != null) {
				var element = WriteArgument (connection, Argument);
				if (element != null) {
					var argument = new XElement ("Argument");
					argument.Add (element);
					node.Add (argument);
				}
			}
		}

		public sealed override async Task<Response> Run (Connection connection, CancellationToken cancellationToken)
		{
			var response = new CommandResponse (this);

			try {
				response.Response = await Run (connection, Argument, cancellationToken);
				response.Success = true;
			} catch (Exception ex) {
				response.Error = ex.ToString ();
				connection.Debug ($"COMMAND FAILED: {ex}");
			}

			return response;
		}

		protected abstract Task<U> Run (Connection connection, T argument, CancellationToken cancellationToken);

		class CommandResponse : Response
		{
			public Command<T,U> Command {
				get;
				private set;
			}

			public U Response {
				get; set;
			}

			public CommandResponse (Command<T,U> command)
			{
				Command = command;
				ObjectID = command.ResponseID;
			}

			public override void Read (Connection connection, XElement node)
			{
				base.Read (connection, node);

				var response = node.Element ("Response");
				if (response != null) {
					if (response.Elements ().Count () > 1)
						throw new ServerErrorException ();
					var first = response.Elements ().First ();
					Response = Command.ReadResponse (connection, first);
				}
			}

			public override void Write (Connection connection, XElement node)
			{
				base.Write (connection, node);
				if (Response != null) {
					var element = Command.WriteResponse (connection, Response);
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

