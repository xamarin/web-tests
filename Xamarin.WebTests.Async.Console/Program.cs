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
using Xamarin.WebTests.Portable;
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

		public TestApp Context {
			get;
			private set;
		}

		public int LogLevel {
			get;
			private set;
		}

		public bool LogRemotely {
			get;
			private set;
		}

		public bool IsServer {
			get { return Endpoint == null; }
		}

		public bool Wait {
			get;
			private set;
		}

		public bool Run {
			get;
			private set;
		}

		public bool UseMySettings {
			get { return SettingsFile != null; }
		}

		public bool UseMyTestSuite {
			get;
			private set;
		}

		ConsoleClient connection;

		public static void Main (string[] args)
		{
			SD.Debug.AutoFlush = true;
			SD.Debug.Listeners.Add (new SD.ConsoleTraceListener ());

			PortableSupportImpl.Initialize ();

			var program = new Program (args);

			try {
				var task = program.RunMain ();
				task.Wait ();
			} catch (Exception ex) {
				Debug ("ERROR: {0}", ex);
			}
		}

		Program (string[] args)
		{
			LogLevel = -1;

			var p = new OptionSet ();
			p.Add ("settings=", v => SettingsFile = v);
			p.Add ("connect=", v => Endpoint = GetEndpoint (v));
			p.Add ("wait", v => Wait = true);
			p.Add ("run", v => Run = true);
			p.Add ("result=", v => ResultOutput = v);
			p.Add ("log-level=", v => LogLevel = int.Parse (v));
			p.Add ("log-remotely", v => LogRemotely = true);
			p.Add ("my-tests", v => UseMyTestSuite = true);
			var remaining = p.Parse (args);

			Settings = LoadSettings (SettingsFile);

			if (remaining.Count > 0) {
				Console.Error.WriteLine ("Failed to parse command-line args!");
				Environment.Exit (255);
				return;
			}

			Context = new TestApp (PortableSupport.Instance, Settings);
			Context.Logger = new ConsoleLogger (this);
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

		Task RunMain ()
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
			await RunServer (listener);
			listener.Stop ();
		}

		async Task RunServer (TcpListener listener)
		{
			Debug ("Server running");

			var socket = await listener.AcceptSocketAsync ();
			var stream = new NetworkStream (socket);

			Debug ("Got remote connection from {0}.", socket.RemoteEndPoint);

			connection = new ConsoleClient (this, stream);
			await connection.RunClient (CancellationToken.None);

			Debug ("Closed remote connection.");

			connection = null;
		}

		async Task RunClient ()
		{
			await Task.Yield ();

			var client = new TcpClient ();
			await client.ConnectAsync (Endpoint.Address, Endpoint.Port);

			var stream = client.GetStream ();
			connection = new ConsoleClient (this, stream);
			await connection.RunClient (CancellationToken.None);

			Debug ("Closed remote connection.");

			connection = null;
		}

		async void OnLogMessage (string message)
		{
			if (connection == null || !LogRemotely)
				return;
			await connection.LogMessage (message);
		}

		class ConsoleLogger : TestLogger
		{
			readonly Program Program;

			public ConsoleLogger (Program program)
			{
				Program = program;
			}

			protected override void OnLogEvent (LogEntry entry)
			{
				switch (entry.Kind) {
				case LogEntry.EntryKind.Debug:
					if (entry.LogLevel <= Program.Context.DebugLevel)
						Program.OnLogMessage (entry.Text);
					break;

				case LogEntry.EntryKind.Error:
					if (entry.Error != null)
						Program.OnLogMessage (string.Format ("ERROR: {0}", entry.Error));
					else
						Program.OnLogMessage (entry.Text);
					break;

				default:
					Program.OnLogMessage (entry.Text);
					break;
				}
			}
		}
	}
}

