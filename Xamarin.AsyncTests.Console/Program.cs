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
using System.Linq;
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

		public Assembly Assembly {
			get;
			private set;
		}

		public SettingsBag Settings {
			get { return settings; }
		}

		public TestLogger Logger {
			get { return logger; }
		}

		public int? LogLevel {
			get;
			private set;
		}

		public int? LocalLogLevel {
			get;
			private set;
		}

		public bool Wait {
			get;
			private set;
		}

		public bool DebugMode {
			get;
			private set;
		}

		TestSession session;
		SettingsBag settings;
		TestLogger logger;
		Assembly[] dependencyAssemblies;
		bool optionalGui;
		bool showCategories;
		bool showFeatures;
		bool saveOptions;
		string customSettings;
		string category;
		string features;

		public static void Run (Assembly assembly, string[] args)
		{
			SD.Debug.AutoFlush = true;
			SD.Debug.Listeners.Add (new SD.ConsoleTraceListener ());

			DependencyInjector.RegisterAssembly (typeof(PortableSupportImpl).Assembly);

			var program = new Program (assembly, args);

			try {
				var task = program.Run (CancellationToken.None);
				task.Wait ();
			} catch (Exception ex) {
				Debug ("ERROR: {0}", ex);
			}
		}

		static void Main (string[] args)
		{
			Run (null, args);
		}

		Program (Assembly assembly, string[] args)
		{
			var dependencies = new List<string> ();

			ResultOutput = "TestResult.xml";

			var p = new OptionSet ();
			p.Add ("settings=", v => SettingsFile = v);
			p.Add ("connect=", v => EndPoint = GetEndPoint (v));
			p.Add ("gui=", v => GuiEndPoint = GetEndPoint (v));
			p.Add ("wait", v => Wait = true);
			p.Add ("no-result", v => ResultOutput = null);
			p.Add ("result=", v => ResultOutput = v);
			p.Add ("log-level=", v => LogLevel = int.Parse (v));
			p.Add ("local-log-level=", v => LocalLogLevel = int.Parse (v));
			p.Add ("dependency=", v => dependencies.Add (v));
			p.Add ("optional-gui", v => optionalGui = true);
			p.Add ("set=", v => customSettings = v);
			p.Add ("category=", v => category = v);
			p.Add ("features=", v => features = v);
			p.Add ("debug", v => DebugMode = true);
			p.Add ("save-options", v => saveOptions = true);
			p.Add ("show-categories", v => showCategories = true);
			p.Add ("show-features", v => showFeatures = true);
			p.Add ("show-config", v => showCategories = showFeatures = true);
			var remaining = p.Parse (args);

			dependencyAssemblies = new Assembly [dependencies.Count];
			for (int i = 0; i < dependencyAssemblies.Length; i++) {
				dependencyAssemblies [i] = Assembly.LoadFile (dependencies [i]);
			}

			if (assembly != null) {
				if (remaining.Count != 0)
					throw new InvalidOperationException ();
				Assembly = assembly;
			} else if (remaining.Count == 1) {
				Assembly = Assembly.LoadFile (remaining [0]);
			} else if (EndPoint == null) {
				throw new InvalidOperationException ();
			}

			CheckSettingsFile ();

			settings = LoadSettings (SettingsFile);

			if (customSettings != null)
				ParseSettings (customSettings);

			if (DebugMode) {
				settings.LogLevel = -1;
				settings.LocalLogLevel = -1;
				settings.DisableTimeouts = true;
			}

			if (LogLevel != null)
				settings.LogLevel = LogLevel.Value;
			if (LocalLogLevel != null)
				settings.LocalLogLevel = LocalLogLevel.Value;

			logger = new TestLogger (new ConsoleLogger (this));
		}

		static void WriteLine ()
		{
			global::System.Console.WriteLine();
		}

		static void WriteLine (string message, params object[] args)
		{
			global::System.Console.WriteLine (message, args);
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

		void ParseSettings (string arg)
		{
			var parts = arg.Split (',');
			foreach (var part in parts) {
				var pos = part.IndexOf ('=');
				if (pos > 0) {
					var key = part.Substring (0, pos);
					var value = part.Substring (pos + 1);
					Debug ("SET: |{0}|{1}|", key, value);
					if (key [0] == '-')
						throw new InvalidOperationException ();
					settings.SetValue (key, value);
				} else if (part [0] == '-') {
					var key = part.Substring (1);
					settings.RemoveValue (key);
				} else {
					throw new InvalidOperationException ();
				}
			}

			SaveSettings ();
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

		void CheckSettingsFile ()
		{
			if (SettingsFile != null || Assembly == null)
				return;

			var name = Assembly.GetName ().Name;
			var path = Environment.GetFolderPath (Environment.SpecialFolder.LocalApplicationData);
			path = Path.Combine (path, "Xamarin", "AsyncTests");

			if (!Directory.Exists (path))
				Directory.CreateDirectory (path);

			SettingsFile = Path.Combine (path, name + ".xml");
		}

		static SettingsBag LoadSettings (string filename)
		{
			if (filename == null || !File.Exists (filename))
				return SettingsBag.CreateDefault ();

			Debug ("Loading settings from {0}.", filename);
			using (var reader = new StreamReader (filename)) {
				var doc = XDocument.Load (reader);
				return TestSerializer.ReadSettings (doc.Root);
			}
		}

		void SaveSettings ()
		{
			if (SettingsFile == null)
				return;

			Debug ("Saving settings to {0}.", SettingsFile);
			using (var writer = new StreamWriter (SettingsFile)) {
				var xws = new XmlWriterSettings ();
				xws.Indent = true;

				using (var xml = XmlTextWriter.Create (writer, xws)) {
					var node = TestSerializer.WriteSettings (Settings);
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
			var framework = TestFramework.GetLocalFramework (Assembly, dependencyAssemblies);

			TestServer server;
			try {
				var endpoint = GetPortableEndPoint (GuiEndPoint);
				server = await TestServer.ConnectToGui (this, endpoint, framework, cancellationToken);
			} catch (SocketException ex) {
				if (ex.SocketErrorCode == SocketError.ConnectionRefused && optionalGui) {
					await RunLocal (cancellationToken);
					return;
				}
				throw;
			}

			OnSessionCreated (server.Session);

			cancellationToken.ThrowIfCancellationRequested ();
			await server.WaitForExit (cancellationToken);
		}

		bool OnSessionCreated (TestSession session)
		{
			var config = session.Configuration;

			bool done = false;
			if (showCategories) {
				WriteLine ("Test Categories:");
				foreach (var category in session.ConfigurationProvider.Categories) {
					var builtinText = category.IsBuiltin ? " (builtin)" : string.Empty;
					var explicitText = category.IsExplicit ? " (explicit)" : string.Empty;
					var currentText = config.CurrentCategory != null && config.CurrentCategory.Name.Equals (category.Name) ? " (current)" : string.Empty;
					WriteLine ("  {0}{1}{2}{3}", category.Name, builtinText, explicitText, currentText);
				}
				WriteLine ();
				done = true;
			}

			if (showFeatures) {
				WriteLine ("Test Features:");
				foreach (var feature in session.ConfigurationProvider.Features) {
					var constText = feature.Constant != null ? string.Format (" (const = {0})", feature.Constant.Value ? "enabled" : "disabled") : string.Empty;
					var defaultText = feature.DefaultValue != null ? string.Format (" (default = {0})", feature.DefaultValue.Value ? "enabled" : "disabled") : string.Empty;
					var currentText = feature.CanModify ? string.Format (" ({0})", config.IsEnabled (feature) ? "enabled" : "disabled") : string.Empty;
					WriteLine ("  {0,-30} {1}{2}{3}{4}", feature.Name, feature.Description, constText, defaultText, currentText);
				}
				WriteLine ();
				done = true;
			}

			if (done)
				Environment.Exit (0);

			bool modified = false;

			if (category != null) {
				config.CurrentCategory = config.Categories.First (c => c.Name.Equals (category));
				modified = true;
			}

			if (features != null) {
				modified = true;
				var parts = features.Split (',');
				foreach (var part in parts) {
					var name = part;
					bool enable = true;
					if (part [0] == '-') {
						name = part.Substring (1);
						enable = false;
					} else if (part [0] == '+') {
						name = part.Substring (1);
						enable = true;
					}

					if (name.Equals ("all")) {
						foreach (var feature in config.Features) {
							if (feature.CanModify)
								config.SetIsEnabled (feature, enable);
						}
					} else {
						var feature = config.Features.First (f => f.Name.Equals (name));
						config.SetIsEnabled (feature, enable);
					}
				}
			}

			if (modified && saveOptions)
				SaveSettings ();

			return modified;
		}

		async Task RunLocal (CancellationToken cancellationToken)
		{
			var framework = TestFramework.GetLocalFramework (Assembly, dependencyAssemblies);

			cancellationToken.ThrowIfCancellationRequested ();
			session = TestSession.CreateLocal (this, framework);
			OnSessionCreated (session);

			var test = session.RootTestCase;

			Debug ("Got test: {0}", test);
			var start = DateTime.Now;
			var result = await session.Run (test, cancellationToken);
			var end = DateTime.Now;
			Debug ("Got result: {0}", result);

			Debug ("{0} tests, {1} passed, {2} errors, {3} ignored.", countTests, countSuccess, countErrors, countIgnored);
			Debug ("Total time: {0}.", end - start);

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
			if (OnSessionCreated (session))
				await session.UpdateSettings (cancellationToken);

			var test = session.RootTestCase;
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
			if (Settings.LocalLogLevel >= 0 && level > Settings.LocalLogLevel)
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

