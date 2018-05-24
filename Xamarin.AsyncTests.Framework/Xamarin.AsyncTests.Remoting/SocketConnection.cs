//
// SocketConnection.cs
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
using System.Xml;
using System.Xml.Linq;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Xamarin.AsyncTests.Remoting
{
	using Framework;
	using Portable;

	abstract class SocketConnection : Connection
	{
		readonly Stream stream;

		internal SocketConnection (TestApp app, Stream stream)
			: base (app)
		{
			this.stream = stream;
		}

		#region Sending Commands and Main Loop

		QueuedMessage currentMessage;

		protected override async Task SendMessage (Message message)
		{
			Debug ($"SEND MESSAGE: {message}");

			var queued = new QueuedMessage (message);

			while (!cancelCts.IsCancellationRequested) {
				var old = Interlocked.CompareExchange (ref currentMessage, queued, null);
				Debug ($"SEND MESSAGE QUEUE: {message} {old != null}");

				if (old == null)
					break;

				await old.Task.Task.ConfigureAwait (false);
			}

			cancelCts.Token.ThrowIfCancellationRequested ();

			Debug ($"SEND MESSAGE #1: {message}");

			var doc = message.Write (this);

			var sb = new StringBuilder ();
			var settings = new XmlWriterSettings {
				OmitXmlDeclaration = true
			};

			using (var writer = XmlWriter.Create (sb, settings)) {
				doc.WriteTo (writer);
			}

			var bytes = new UTF8Encoding ().GetBytes (sb.ToString ());

			var header = BitConverter.GetBytes (bytes.Length);
			if (bytes.Length == 0)
				throw new ServerErrorException ();

			await stream.WriteAsync (header, 0, 4).ConfigureAwait (false);
			await stream.FlushAsync ();

			await stream.WriteAsync (bytes, 0, bytes.Length);

			await stream.FlushAsync ();

			Debug ($"SEND MESSAGE #2: {message}");

			var old2 = Interlocked.CompareExchange (ref currentMessage, null, queued);
			if (old2 != queued)
				throw new ServerErrorException ();

			queued.Task.SetResult (true);

			Debug ($"SEND MESSAGE #3: {message}");
		}

		async Task<byte[]> ReadBuffer (int length)
		{
			var buffer = new byte[length];
			int pos = 0;
			while (pos < length) {
				var ret = await stream.ReadAsync (buffer, pos, length - pos, cancelCts.Token);
				if (ret <= 0)
					throw new IOException ("Read failed");
				pos += ret;
			}
			return buffer;
		}

		protected override async Task MainLoop ()
		{
			while (!cancelCts.IsCancellationRequested) {
				Debug ($"MAIN LOOP: {shutdownRequested}");

				var header = await ReadBuffer (4);
				var len = BitConverter.ToInt32 (header, 0);
				Debug ($"MAIN LOOP #0: {shutdownRequested} {len}");
				if (len == 0)
					return;

				var body = await ReadBuffer (len);
				var content = new UTF8Encoding ().GetString (body, 0, body.Length);

				var doc = XDocument.Load (new StringReader (content));
				var element = doc.Root;

				Debug ($"MAIN LOOP: {element}");

				if (element.Name.LocalName.Equals ("Response")) {
					var objectID = element.Attribute ("ObjectID").Value;
					var operation = GetResponse (long.Parse (objectID));
					operation.Response.Read (this, element);
					operation.Task.SetResult (true);
					if (operation.Command is TestSessionClient.SessionShutdownCommand)
						return;
					continue;
				}

				var command = Command.Create (this, element);

				Debug ($"MAIN LOOP #1: {command} {command.IsOneWay}");

				cancelCts.Token.ThrowIfCancellationRequested ();

				if (command.IsOneWay)
					await command.Run (this, cancelCts.Token);
				else
					HandleCommand (command);

				Debug ($"MAIN LOOP #2: {command} {command.IsOneWay}");

				if (command is ShutdownCommand)
					return;
			}
		}

		#endregion
	}
}

