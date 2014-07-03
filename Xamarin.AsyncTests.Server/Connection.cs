//
// TestServer.cs
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
using System.Text;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Xamarin.AsyncTests.Server
{
	public abstract class Connection
	{
		Stream stream;
		Serializer serializer;

		public Connection (Stream stream)
		{
			this.stream = stream;
			serializer = new Serializer (this);
		}

		public int DebugLevel {
			get; set;
		}

		#region Public Client API

		public async Task Hello (CancellationToken cancellationToken)
		{
			var command = new HelloCommand ();
			await SendCommand (command);
		}

		public async Task Debug (int level, string message)
		{
			var command = new DebugCommand { Level = level, Message = message };
			await SendCommand (command);
		}

		public async Task Message (string message)
		{
			var command = new MessageCommand { Message = message };
			await SendCommand (command);
		}

		public async Task SetDebugLevel (int level)
		{
			var command = new SetDebugLevelCommand { Level = level };
			await SendCommand (command);
		}

		#endregion

		internal Serializer Serializer {
			get { return serializer; }
		}

		public void Run ()
		{
			MainLoop ();
		}

		public ITestLogger GetLogger ()
		{
			return new ServerLogger (this);
		}

		internal async Task SendCommand (Command command)
		{
			var formatted = serializer.Write (command);
			var bytes = new UTF8Encoding ().GetBytes (formatted);

			var header = BitConverter.GetBytes (bytes.Length);
			await stream.WriteAsync (header, 0, 4).ConfigureAwait (false);
			await stream.FlushAsync ();

			await stream.WriteAsync (bytes, 0, bytes.Length);

			await stream.FlushAsync ();
		}

		internal void SendCommandSync (Command command)
		{
			SendCommand (command).Wait ();
		}

		internal static void Debug (string message, params object[] args)
		{
			System.Diagnostics.Debug.WriteLine (message, args);
		}

		async Task<byte[]> ReadBuffer (int length)
		{
			var buffer = new byte [length];
			int pos = 0;
			while (pos < length) {
				var ret = await stream.ReadAsync (buffer, pos, length-pos);
				if (ret <= 0)
					throw new InvalidOperationException ();
				pos += ret;
			}
			return buffer;
		}

		async void MainLoop ()
		{
			while (true) {
				var header = await ReadBuffer (4);
				var len = BitConverter.ToInt32 (header, 0);
				if (len == 0)
					return;

				var body = await ReadBuffer (len);
				var content = new UTF8Encoding ().GetString (body, 0, body.Length);
				await HandleCommand (content);
			}
		}

		async Task HandleCommand (string formatted)
		{
			var command = serializer.ReadCommand (formatted);

			var commonCommand = command as ICommonCommand;
			if (commonCommand != null) {
				await commonCommand.Run (this, CancellationToken.None);
				return;
			}

			await Run (command);
		}

		internal abstract Task Run (Command command);

		internal Task Run (MessageCommand command, CancellationToken cancellationToken)
		{
			return Task.Run (() => OnMessage (command.Message));
		}

		internal Task Run (DebugCommand command, CancellationToken cancellationToken)
		{
			return Task.Run (() => OnDebug (command.Level, command.Message));
		}

		internal Task Run (SetDebugLevelCommand command, CancellationToken cancellationToken)
		{
			return Task.Run (() => DebugLevel = command.Level);
		}

		internal Task Run (HelloCommand command, CancellationToken cancellationToken)
		{
			return OnHello (cancellationToken);
		}

		protected abstract Task OnHello (CancellationToken cancellationToken);

		protected abstract void OnMessage (string message);

		protected abstract void OnDebug (int level, string message);
	}
}

