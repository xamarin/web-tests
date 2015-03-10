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
using System.Collections.Generic;
using System.Reflection;
using SD = System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using NDesk.Options;

namespace Xamarin.AsyncTests.Console
{
	using Remoting;
	using Portable;
	using Framework;

	public class Program : TestApp
	{
		public string SettingsFile {
			get;
			private set;
		}

		public string ResultOutput {
			get;
			private set;
		}

		public IPEndPoint EndPoint {
			get;
			private set;
		}

		public IPEndPoint GuiEndPoint {
			get;
			private set;
		}

		public SettingsBag Settings {
			get { return settings; }
		}

		public TestLogger Logger {
			get { return logger; }
		}

		public TestFramework Framework {
			get { return framework; }
		}

		public int LogLevel {
			get;
			private set;
		}

		public bool Wait {
			get;
			private set;
		}

		TestSession session;
		SettingsBag settings;
		TestFramework framework;
		TestLogger logger;

		public static void Main (string[] args)
		{
			SD.Debug.AutoFlush = true;
			SD.Debug.Listeners.Add (new SD.ConsoleTraceListener ());

			DependencyInjector.RegisterAssembly (typeof(PortableSupportImpl).Assembly);

			var program = new Program (args);

			try {
				var task = program.Run (CancellationToken.None);
				task.Wait ();
			} catch (Exception ex) {
				Debug ("ERROR: {0}", ex);
			}
		}

		Program (string[] args)
		{
			LogLevel = -1;

			var dependencies = new List<string> ();

			var p = new OptionSet ();
			p.Add ("settings=", v => SettingsFile = v);
			p.Add ("connect=", v => EndPoint = GetEndPoint (v));
			p.Add ("gui=", v => GuiEndPoint = GetEndPoint (v));
			p.Add ("wait", v => Wait = true);
			p.Add ("result=", v => ResultOutput = v);
			p.Add ("log-level=", v => LogLevel = int.Parse (v));
			p.Add ("dependency=", v => dependencies.Add (v));
			var remaining = p.Parse (args);

			settings = LoadSettings (SettingsFile);

			var dependencyAsms = new Assembly [dependencies.Count];
			for (int i = 0; i < dependencyAsms.Length; i++) {
				dependencyAsms [i] = Assembly.LoadFile (dependencies [i]);
			}

			Assembly assembly;
			if (remaining.Count == 1)
				assembly = Assembly.LoadFile (remaining [0]);
			else if (remaining.Count == 0)
				assembly = typeof(Sample.SampleFeatures).Assembly;
			else
				throw new InvalidOperationException ();

			logger = new TestLogger (new ConsoleLogger (this));
			logger.LogLevel = LogLevel;

			framework = TestFramework.GetLocalFramework (assembly, dependencyAsms);
		}

		static void Debug (string message, params object[] args)
		{
			SD.Debug.WriteLine (message, args);
		}

		static IPEndPoint GetEndPoint (string text)
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

		static string PrintEndPoint (IPEndPoint endpoint)
		{
			return string.Format ("{0}:{1}", endpoint.Address, endpoint.Port);
		}

		static IPortableEndPoint GetPortableEndPoint (IPEndPoint endpoint)
		{
			var support = DependencyInjector.Get<IPortableEndPointSupport> ();
			return support.GetEndpoint (endpoint.Address.ToString (), endpoint.Port);
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

		Task Run (CancellationToken cancellationToken)
		{
			if (GuiEndPoint != null)
				return ConnectToGui (cancellationToken);
			else if (EndPoint != null)
				return ConnectToServer (cancellationToken);
			else
				return RunLocal (cancellationToken);
		}

		async Task ConnectToGui (CancellationToken cancellationToken)
		{
			var endpoint = GetPortableEndPoint (GuiEndPoint);
			var server = await TestServer.ConnectToGui (this, endpoint, framework, cancellationToken);
			cancellationToken.ThrowIfCancellationRequested ();

			await server.WaitForExit (cancellationToken);
		}

		async Task RunLocal (CancellationToken cancellationToken)
		{
			session = TestSession.CreateLocal (this, framework);
			var test = await session.GetRootTestCase (cancellationToken);

			Debug ("Got test: {0}", test);
			var result = await session.Run (test, cancellationToken);
			Debug ("Got result: {0}", result);

			Debug ("{0} tests, {1} passed, {2} errors, {3} ignored.", countTests, countSuccess, countErrors, countIgnored);

			if (ResultOutput != null) {
				var serialized = TestSerializer.WriteTestResult (result);
				var settings = new XmlWriterSettings ();
				settings.Indent = true;
				using (var writer = XmlTextWriter.Create (ResultOutput, settings))
					serialized.WriteTo (writer);
				Debug ("Result writting to {0}.", ResultOutput);
			}
		}

		async Task ConnectToServer (CancellationToken cancellationToken)
		{
			var endpoint = GetPortableEndPoint (EndPoint);
			var server = await TestServer.ConnectToServer (this, endpoint, cancellationToken);
			cancellationToken.ThrowIfCancellationRequested ();

			session = server.Session;
			var test = await session.GetRootTestCase (cancellationToken);
			cancellationToken.ThrowIfCancellationRequested ();

			Debug ("Got test: {0}", test);
			var result = await session.Run (test, cancellationToken);
			cancellationToken.ThrowIfCancellationRequested ();
			Debug ("Got result: {0}", result);

			Debug ("{0} tests, {1} passed, {2} errors, {3} ignored.", countTests, countSuccess, countErrors, countIgnored);

			if (ResultOutput != null) {
				var serialized = TestSerializer.WriteTestResult (result);
				var settings = new XmlWriterSettings ();
				settings.Indent = true;
				using (var writer = XmlTextWriter.Create (ResultOutput, settings))
					serialized.WriteTo (writer);
				Debug ("Result writting to {0}.", ResultOutput);
			}

			await server.Stop (cancellationToken);
		}

		void OnLogMessage (string message)
		{
			Debug (message);
		}

		void OnLogDebug (int level, string message)
		{
			if (logger.LogLevel >= 0 && level > Logger.LogLevel)
				return;
			Debug (message);
		}

		int countTests;
		int countSuccess;
		int countErrors;
		int countIgnored;

		void OnStatisticsEvent (TestLoggerBackend.StatisticsEventArgs args)
		{
			switch (args.Type) {
			case TestLoggerBackend.StatisticsEventType.Running:
				++countTests;
				Debug ("Running {0}", args.Name);
				break;
			case TestLoggerBackend.StatisticsEventType.Finished:
				switch (args.Status) {
				case TestStatus.Success:
					++countSuccess;
					break;
				case TestStatus.Ignored:
				case TestStatus.None:
					++countIgnored;
					break;
				default:
					++countErrors;
					break;
				}

				Debug ("Finished {0}: {1}", args.Name, args.Status);
				break;
			case TestLoggerBackend.StatisticsEventType.Reset:
				break;
			}
		}

		class ConsoleLogger : TestLoggerBackend
		{
			readonly Program Program;

			public ConsoleLogger (Program program)
			{
				Program = program;
			}

			protected override void OnLogEvent (LogEntry entry)
			{
				switch (entry.Kind) {
				case EntryKind.Debug:
					Program.OnLogDebug (entry.LogLevel, entry.Text);
					break;

				case EntryKind.Error:
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

			protected override void OnStatisticsEvent (StatisticsEventArgs args)
			{
				Program.OnStatisticsEvent (args);
			}
		}
	}
}

