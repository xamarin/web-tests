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

		public bool Wrench {
			get;
			private set;
		}

		public ApplicationLauncher Launcher {
			get;
			private set;
		}

		public LauncherOptions LauncherOptions {
			get;
			private set;
		}

		DroidHelper droidHelper;
		TestSession session;
		SettingsBag settings;
		TestLogger logger;
		TestResult result;
		DateTime startTime, endTime;
		Command command;
		Assembly[] dependencyAssemblies;
		List<string> arguments;
		bool optionalGui;
		bool showCategories;
		bool showFeatures;
		bool saveOptions;
		string extraLauncherArgs;
		string customSettings;
		string category;
		string features;
		string stdout;
		string stderr;
		string device;
		string sdkroot;

		public static void Run (Assembly assembly, string[] args)
		{
			SD.Debug.AutoFlush = true;
			SD.Debug.Listeners.Add (new SD.ConsoleTraceListener ());

			DependencyInjector.RegisterAssembly (typeof(PortableSupportImpl).Assembly);

			var program = new Program ();

			try {
				program.ParseArguments (assembly, args);
				program.Initialize ();

				var task = program.Run (CancellationToken.None);
				task.Wait ();
				Environment.Exit (task.Result ? 0 : 1);
			} catch (Exception ex) {
				PrintException (ex);
				Environment.Exit (-1);
			}
		}

		static void PrintException (Exception ex)
		{
			var aggregate = ex as AggregateException;
			if (aggregate != null && aggregate.InnerExceptions.Count == 1) {
				PrintException (aggregate.InnerException);
				return;
			}

			var toolEx = ex as ExternalToolException;
			if (toolEx != null) {
				if (!string.IsNullOrEmpty (toolEx.ErrorOutput))
					Debug ("ERROR: External tool '{0}' failed:\n{1}\n", toolEx.Tool, toolEx.ErrorOutput);
				else
					Debug ("ERROR: External tool '{0}' failed:\n{1}\n", toolEx.Tool, toolEx);
				return;
			}

			Debug ("ERROR: {0}", ex);
		}

		static void Main (string[] args)
		{
			Run (null, args);
		}

		void ParseArguments (Assembly assembly, string[] args)
		{
			var dependencies = new List<string> ();

			ResultOutput = "TestResult.xml";

			var p = new OptionSet ();
			p.Add ("settings=", v => SettingsFile = v);
			p.Add ("endpoint=", v => EndPoint = GetEndPoint (v));
			p.Add ("extra-launcher-args=", v => extraLauncherArgs = v);
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
			p.Add ("stdout=", v => stdout = v);
			p.Add ("stderr=", v => stderr = v);
			p.Add ("device=", v => device = v);
			p.Add ("sdkroot=", v => sdkroot = v);
			p.Add ("wrench", v => Wrench = true);
			var remaining = p.Parse (args);

			if (assembly != null) {
				command = Command.Local;

				if (remaining.Count > 0 && remaining[0].Equals ("local"))
					remaining.RemoveAt (0);
			} else {
				if (remaining.Count < 1)
					throw new InvalidOperationException ("Missing argument.");

				if (!Enum.TryParse<Command> (remaining[0], true, out command))
					throw new InvalidOperationException ("Unknown command.");
				remaining.RemoveAt (0);
			}

			arguments = remaining;

			dependencyAssemblies = new Assembly [dependencies.Count];
			for (int i = 0; i < dependencyAssemblies.Length; i++) {
				dependencyAssemblies [i] = Assembly.LoadFile (dependencies [i]);
			}

			if (command == Command.Listen) {
				if (EndPoint == null)
					EndPoint = GetLocalEndPoint ();
			} else if (command == Command.Local || command == Command.Listen) {
				if (assembly != null) {
					if (arguments.Count != 0) {
						arguments.ForEach (a => Debug ("Unexpected remaining argument: {0}", a));
						throw new InvalidOperationException ("Unexpected extra argument.");
					}
					Assembly = assembly;
				} else if (arguments.Count == 1) {
					Assembly = Assembly.LoadFile (arguments [0]);
				} else if (EndPoint == null) {
					throw new InvalidOperationException ("Missing endpoint");
				}
			} else if (command == Command.Connect) {
				if (assembly != null)
					throw new InvalidOperationException ();
				if (arguments.Count == 1)
					EndPoint = GetEndPoint (arguments [0]);
				else if (arguments.Count == 0) {
					if (EndPoint == null)
						throw new InvalidOperationException ("Missing endpoint");
				} else {
					arguments.ForEach (a => Debug ("Unexpected remaining argument: {0}", a));
					throw new InvalidOperationException ("Unexpected extra argument.");
				}
			} else if (command == Command.Simulator || command == Command.Device || command == Command.TVOS) {
				if (arguments.Count < 1)
					throw new InvalidOperationException ("Expected .app argument");
				Launcher = new TouchLauncher (arguments [0], command, sdkroot, stdout, stderr, device, extraLauncherArgs);
				arguments.RemoveAt (0);

				if (EndPoint == null)
					EndPoint = GetLocalEndPoint ();
			} else if (command == Command.Mac) {
				if (arguments.Count < 1)
					throw new InvalidOperationException ("Expected .app argument");
				Launcher = new MacLauncher (arguments [0], stdout, stderr);
				arguments.RemoveAt (0);

				if (EndPoint == null)
					EndPoint = GetLocalEndPoint ();
			} else if (command == Command.Android || command == Command.Avd) {
				if (arguments.Count < 1)
					throw new InvalidOperationException ("Expected activity argument");

				Launcher = new DroidLauncher (arguments [0], stdout, stderr);
				arguments.RemoveAt (0);

				if (EndPoint == null)
					EndPoint = GetLocalEndPoint ();
			} else if (command == Command.Avd || command == Command.Emulator) {
				if (arguments.Count != 0)
					throw new InvalidOperationException ("Unexpected extra arguments");

				droidHelper = new DroidHelper (sdkroot);
			} else if (command == Command.Result) {
				if (arguments.Count != 1)
					throw new InvalidOperationException ("Expected TestResult.xml argument");
				ResultOutput = arguments[0];
			} else {
				throw new NotImplementedException ();
			}
		}

		void Initialize ()
		{
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

			if (Launcher != null) {
				LauncherOptions = new LauncherOptions {
					Category = category, Features = features
				};
			}
		}

		static void WriteLine ()
		{
			global::System.Console.WriteLine();
		}

		static void WriteLine (string message, params object[] args)
		{
			global::System.Console.WriteLine (message, args);
		}

		internal static void Debug (string message)
		{
			SD.Debug.WriteLine (message);
		}

		internal static void Debug (string message, params object[] args)
		{
			SD.Debug.WriteLine (message, args);
		}

		internal void WriteSummary (string format, params object[] args)
		{
			WriteSummary (string.Format (format, args));
		}

		internal void WriteSummary (string message)
		{
			Debug (message);
			if (Wrench)
				global::System.Console.WriteLine ("@MonkeyWrench: AddSummary: <p>{0}</p>", message);
		}

		internal void WriteErrorSummary (string message)
		{
			Debug ("ERROR: {0}", message);
			if (Wrench)
				global::System.Console.WriteLine ("@MonkeyWrench: AddSummary: <p><b>ERROR: {0}</b></p>", message);
		}

		internal void AddFile (string filename)
		{
			filename = Path.GetFullPath (filename);
			if (Wrench)
				global::System.Console.WriteLine ("@MonkeyWrench: AddFile: {0}", filename);
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

		static IPEndPoint GetLocalEndPoint ()
		{
			return new IPEndPoint (PortableSupportImpl.LocalAddress, 11111);
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

		Task<bool> Run (CancellationToken cancellationToken)
		{
			switch (command) {
			case Command.Local:
				return RunLocal (cancellationToken);
			case Command.Connect:
				return ConnectToServer (cancellationToken);
			case Command.Gui:
				return ConnectToGui (cancellationToken);
			case Command.Listen:
				return WaitForConnection (cancellationToken);
			case Command.Simulator:
			case Command.Device:
			case Command.Mac:
			case Command.Android:
			case Command.TVOS:
				return LaunchApplication (cancellationToken);
			case Command.Avd:
				return droidHelper.CheckAvd (cancellationToken);
			case Command.Emulator:
				return droidHelper.CheckEmulator (cancellationToken);
			case Command.Result:
				return ShowResult (cancellationToken);
			default:
				throw new NotImplementedException ();
			}
		}

		async Task<bool> ConnectToGui (CancellationToken cancellationToken)
		{
			var framework = TestFramework.GetLocalFramework (Assembly, dependencyAssemblies);

			TestServer server;
			try {
				var endpoint = GetPortableEndPoint (GuiEndPoint);
				server = await TestServer.ConnectToGui (this, endpoint, framework, cancellationToken);
			} catch (SocketException ex) {
				if (ex.SocketErrorCode == SocketError.ConnectionRefused && optionalGui) {
					return await RunLocal (cancellationToken);
				}
				throw;
			}

			OnSessionCreated (server.Session);

			cancellationToken.ThrowIfCancellationRequested ();
			await server.WaitForExit (cancellationToken);
			return true;
		}

		bool ModifyConfiguration (TestConfiguration config)
		{
			bool modified = false;

			if (category != null) {
				if (string.Equals (category, "all", StringComparison.OrdinalIgnoreCase))
					config.CurrentCategory = TestCategory.All;
				else if (string.Equals (category, "global", StringComparison.OrdinalIgnoreCase))
					config.CurrentCategory = TestCategory.Global;
				else
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

			return modified;
		}

		bool OnSessionCreated (TestSession session)
		{
			var config = session.Configuration;

			var modified = ModifyConfiguration (config);

			if (Wrench) {
				WriteSummary ("Test category: {0}", config.CurrentCategory.Name);
				var enabledFeatures = session.ConfigurationProvider.Features.Where (f => f.CanModify && config.IsEnabled (f));
				var featureSummary = string.Join (",", enabledFeatures.Select (f => f.Name));
				if (!string.IsNullOrWhiteSpace (featureSummary))
					WriteSummary ("Test features: {0}", featureSummary);
				var constantFeatures = session.ConfigurationProvider.Features.Where (f => f.Constant ?? false);
				var constantSummary = string.Join (",", constantFeatures.Select (f => f.Name));
				if (!string.IsNullOrWhiteSpace (constantSummary))
					WriteSummary ("Constant test features: {0}", constantSummary);
			}

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

			if (modified && saveOptions)
				SaveSettings ();

			return modified;
		}

		bool IsSuccessResult {
			get {
				if (result.Status == TestStatus.Success)
					return true;
				else if (result.Status == TestStatus.Ignored)
					return true;
				else
					return false;
			}
		}

		async Task<bool> RunLocal (CancellationToken cancellationToken)
		{
			var framework = TestFramework.GetLocalFramework (Assembly, dependencyAssemblies);

			cancellationToken.ThrowIfCancellationRequested ();
			session = TestSession.CreateLocal (this, framework);
			OnSessionCreated (session);

			var test = session.RootTestCase;

			Debug ("Got test: {0}", test);
			startTime = DateTime.Now;
			result = await session.Run (test, cancellationToken);
			endTime = DateTime.Now;
			Debug ("Got result: {0}", result);

			SaveResult ();

			return IsSuccessResult;
		}

		async Task<bool> ConnectToServer (CancellationToken cancellationToken)
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
			startTime = DateTime.Now;
			result = await session.Run (test, cancellationToken);
			endTime = DateTime.Now;
			cancellationToken.ThrowIfCancellationRequested ();
			Debug ("Got result: {0}", result);

			SaveResult ();

			await server.Stop (cancellationToken);

			return IsSuccessResult;
		}

		async Task<bool> LaunchApplication (CancellationToken cancellationToken)
		{
			var endpoint = GetPortableEndPoint (EndPoint);

			TestServer server;
			try {
				server = await TestServer.LaunchApplication (this, endpoint, Launcher, LauncherOptions, cancellationToken);
			} catch (LauncherErrorException ex) {
				WriteErrorSummary (ex.Message);
				Environment.Exit (255);
				throw;
			}

			cancellationToken.ThrowIfCancellationRequested ();

			Debug ("Test app launched.");
			var ok = await RunRemoteSession (server, cancellationToken);

			Debug ("Application finished.");

			return ok;
		}

		async Task<bool> WaitForConnection (CancellationToken cancellationToken)
		{
			var endpoint = GetPortableEndPoint (EndPoint);
			var server = await TestServer.WaitForConnection (this, endpoint, cancellationToken);
			cancellationToken.ThrowIfCancellationRequested ();

			Debug ("Got server connection.");
			return await RunRemoteSession (server, cancellationToken);
		}

		async Task<bool> RunRemoteSession (TestServer server, CancellationToken cancellationToken)
		{
			session = server.Session;
			if (OnSessionCreated (session))
				await session.UpdateSettings (cancellationToken);

			var test = session.RootTestCase;
			cancellationToken.ThrowIfCancellationRequested ();

			Debug ("Got test: {0}", test);
			startTime = DateTime.Now;
			result = await session.Run (test, cancellationToken);
			endTime = DateTime.Now;
			cancellationToken.ThrowIfCancellationRequested ();
			Debug ("Got result: {0}", result);

			SaveResult ();

			await server.Stop (cancellationToken);

			return IsSuccessResult;
		}

		void SaveResult ()
		{
			WriteSummary ("{0} tests, {1} passed, {2} errors, {3} ignored.", countTests, countSuccess, countErrors, countIgnored);
			WriteSummary ("Total time: {0}.", endTime - startTime);

			if (ResultOutput != null) {
				var serialized = TestSerializer.WriteTestResult (result);
				var settings = new XmlWriterSettings ();
				settings.Indent = true;
				using (var writer = XmlTextWriter.Create (ResultOutput, settings))
					serialized.WriteTo (writer);
				Debug ("Result writting to {0}.", ResultOutput);
				AddFile (ResultOutput);
			}

			if (!string.IsNullOrWhiteSpace (stdout) && File.Exists (stdout))
				AddFile (stdout);
			if (!string.IsNullOrWhiteSpace (stderr) && File.Exists (stderr))
				AddFile (stderr);

			var printer = new ResultPrinter (global::System.Console.Out, result);
			printer.Print ();
		}

		async Task<bool> ShowResult (CancellationToken cancellationToken)
		{
			await Task.Yield ();

			var printer = ResultPrinter.Load (global::System.Console.Out, ResultOutput);
			return printer.Print ();
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

