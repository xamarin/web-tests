//
// Program.cs
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
using System.IO;
using SD = System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace Xamarin.AsyncTests.Client
{
	using Framework;
	using Server;

	class MainClass
	{
		public static void Main (string[] args)
		{
			SD.Debug.Listeners.Add (new SD.ConsoleTraceListener ());

			var main = new MainClass ();
			try {
				var task = main.Run (args);
				task.Wait ();
			} catch (Exception ex) {
				Debug ("ERROR: {0}", ex);
			}
		}

		async Task Run (string[] args)
		{
			if (args.Length == 0) {
				await RunServer ();
				return;
			}

			switch (args [0]) {
			case "client":
				var address = GetEndpoint (args [1]);
				await RunClient (address);
				return;
			default:
				Console.Error.WriteLine ("UNKNOWN COMMAND: {0}", args [0]);
				return;
			}
		}

		static IPEndPoint GetEndpoint (string text)
		{
			int port;
			string host;
			var pos = text.IndexOf (":");
			if (pos < 0) {
				host = text;
				port = 8888;
			} else {
				host = text.Substring (0, pos);
				port = int.Parse (text.Substring (pos + 1));
			}

			var address = IPAddress.Parse (host);
			return new IPEndPoint (address, port);
		}

		static void Debug (string message, params object[] args)
		{
			SD.Debug.WriteLine (message, args);
		}

		static TestResult CreateTestResult ()
		{
			var builder = new TestNameBuilder ();
			builder.PushName ("Hello");
			builder.PushParameter ("A", "B");
			builder.PushParameter ("Foo", "Bar", true);
			var name = builder.GetName ();

			return new TestResult (name, TestStatus.Success);
		}

		async Task RunServer ()
		{
			var listener = new TcpListener (IPAddress.Any, 8888);
			listener.Start ();

			while (true) {
				await RunServer (listener);
			}
		}

		async Task RunServer (TcpListener listener)
		{
			Debug ("Server running");

			var socket = await listener.AcceptSocketAsync ();
			var stream = new NetworkStream (socket);

			Debug ("Got remote connection from {0}.", socket.RemoteEndPoint);

			var connection = new ConsoleServer (stream);
			await connection.RunServer ();

			Debug ("Closed remote connection.");
		}

		async Task RunClient (IPEndPoint endpoint)
		{
			await Task.Yield ();

			var client = new TcpClient ();
			await client.ConnectAsync (endpoint.Address, endpoint.Port);

			var stream = client.GetStream ();
			var server = new ConsoleServer (stream);
			await server.RunClient ();
		}
	}
}
