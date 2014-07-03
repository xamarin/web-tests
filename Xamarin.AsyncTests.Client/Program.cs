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
			var task = main.Run ();
			task.Wait ();
			Console.WriteLine ("DONE!");
			Console.ReadLine ();
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

		async Task Run ()
		{
			var listener = new TcpListener (IPAddress.Any, 8888);
			listener.Start ();

			var socket = await listener.AcceptSocketAsync ();
			var stream = new NetworkStream (socket);

			Debug ("Got remote connection from {0}.", socket.RemoteEndPoint);

			var connection = new ConsoleClient (stream);
			connection.Run ();

			await connection.Hello (CancellationToken.None);
			await connection.Message ("Hello World");

			var result = await connection.RunTest ();

			await connection.LoadResult (result, CancellationToken.None);
		}
	}
}
