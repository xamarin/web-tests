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
using System.Xml;
using System.Xml.Linq;
using SD = System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Xamarin.WebTests;
using NDesk.Options;

namespace Xamarin.AsyncTests.Client
{
	using Server;
	using Framework;

	public class Program
	{
		public string SettingsFile {
			get;
			private set;
		}

		public string ResultOutput {
			get;
			private set;
		}

		public IPEndPoint Endpoint {
			get;
			private set;
		}

		public SettingsBag Settings {
			get;
			private set;
		}

		public TestContext Context {
			get;
			private set;
		}

		public bool Wait {
			get;
			private set;
		}

		public bool NoRun {
			get;
			private set;
		}

		public bool UseServerSettings {
			get { return SettingsFile == null; }
		}

		public static void Main (string[] args)
		{
			SD.Debug.AutoFlush = true;
			SD.Debug.Listeners.Add (new SD.ConsoleTraceListener ());

			var program = new Program (args);

			try {
				var task = program.Run ();
				task.Wait ();
			} catch (Exception ex) {
				Debug ("ERROR: {0}", ex);
			}
		}

		Program (string[] args)
		{
			var p = new OptionSet ();
			p.Add ("settings=", v => SettingsFile = v);
			p.Add ("connect=", v => Endpoint = GetEndpoint (v));
			p.Add ("wait", v => Wait = true);
			p.Add ("norun", v => NoRun = true);
			p.Add ("result=", v => ResultOutput = v);
			var remaining = p.Parse (args);

			Settings = LoadSettings (SettingsFile);

			Debug ("REMAINING ARGS: {0}", remaining.Count);

			if (remaining.Count > 0) {
				Console.Error.WriteLine ("Failed to parse command-line args!");
				Environment.Exit (255);
				return;
			}

			Context = new TestContext (Settings);
		}

		static void Debug (string message, params object[] args)
		{
			SD.Debug.WriteLine (message, args);
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

		static SettingsBag LoadSettings (string filename)
		{
			if (filename == null || !File.Exists (filename))
				return SettingsBag.CreateDefault ();

			using (var reader = new StreamReader (filename)) {
				var doc = XDocument.Load (reader);
				return Connection.LoadSettings (doc.Root);
			}
		}

		void SaveSettings ()
		{
			if (SettingsFile == null)
				return;

			using (var writer = new StreamWriter (SettingsFile)) {
				var xws = new XmlWriterSettings ();
				xws.Indent = true;

				using (var xml = XmlTextWriter.Create (writer, xws)) {
					var node = Connection.WriteSettings (Settings);
					node.WriteTo (xml);
					xml.Flush ();
				}
			}
		}

		Task Run ()
		{
			if (Endpoint == null)
				return RunServer ();
			else
				return RunClient ();
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

			var connection = new ConsoleServer (this, stream);
			await connection.RunServer ();

			Debug ("Closed remote connection.");
		}

		async Task RunClient ()
		{
			await Task.Yield ();

			var client = new TcpClient ();
			await client.ConnectAsync (Endpoint.Address, Endpoint.Port);

			var stream = client.GetStream ();
			var server = new ConsoleServer (this, stream);
			await server.RunClient ();
		}

	}
}

