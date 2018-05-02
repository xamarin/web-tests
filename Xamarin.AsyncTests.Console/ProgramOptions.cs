//
// ProgramOptions.cs
//
// Author:
//       Martin Baulig <martin.baulig@xamarin.com>
//
// Copyright (c) 2017 Xamarin Inc. (http://www.xamarin.com)
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
using System.Net;
using System.Xml;
using System.Xml.Linq;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Xamarin.AsyncTests.Framework;
using NDesk.Options;

namespace Xamarin.AsyncTests.Console {
	public class ProgramOptions {
		public Assembly Assembly {
			get;
		}

		public string Application {
			get;
		}

		internal Command Command {
			get;
		}

		public SettingsBag Settings {
			get;
		}

		public string ResultOutput {
			get;
		}

		public string JUnitResultOutput {
			get;
		}

		public string PackageName {
			get;
		}

		public IPEndPoint EndPoint {
			get;
			private set;
		}

		public IPEndPoint GuiEndPoint {
			get;
			private set;
		}

		public bool Jenkins {
			get;
			private set;
		}

		public string JenkinsHtml {
			get;
			private set;
		}

		public string OutputDirectory {
			get;
		}

		public string IOSDeviceType {
			get;
		}

		public string IOSRuntime {
			get;
		}

		public string AndroidActivity {
			get;
			private set;
		}

		public string AndroidSerial {
			get;
			private set;
		}

		public string ExtraLauncherArgs {
			get;
			private set;
		}

		public bool OptionalGui {
			get;
			private set;
		}

		public bool ShowCategories {
			get;
			private set;
		}

		public bool ShowFeatures {
			get;
			private set;
		}

		public bool ShowConfiguration {
			get;
			private set;
		}

		public string StdOut {
			get;
		}

		public string JenkinsStdOutLink {
			get;
		}

		public string SdkRoot {
			get;
		}

		public string AndroidSdkRoot {
			get;
		}

		public string SaveLogCat {
			get;
			private set;
		}

		public string Category {
			get;
			private set;
		}

		public string Features {
			get;
			private set;
		}

		public string MartinTest {
			get;
			private set;
		}

		public string RootPath {
			get;
			private set;
		}

		public string JenkinsJobPath {
			get;
			private set;
		}

		public IList<string> Arguments {
			get;
		}

		public Assembly[] Dependencies {
			get;
		}

		bool? saveSettings;
		string settingsFile;

		public ProgramOptions (Assembly assembly, string[] args)
		{
			Assembly = assembly;

			var dependencies = new List<string> ();

			var resultOutput = "TestResult.xml";
			var junitResultOutput = "JUnitTestResult.xml";
			string packageName = null;

			string outputDir = null, stdout = null;
			string customSettings = null;
			string sdkRoot = null, iosDeviceType = null, iosRuntime = null;
			string androidSdkRoot = null;
			int? repeat = null;

			bool debugMode = false;
			bool noJUnit = false;
			bool dontSaveLogging = false;
			string logLevel = null;
			int? localLogLevel = null;

			var p = new OptionSet ();
			p.Add ("settings=", v => settingsFile = v);
			p.Add ("endpoint=", v => EndPoint = Program.GetEndPoint (v));
			p.Add ("extra-launcher-args=", v => ExtraLauncherArgs = v);
			p.Add ("gui=", v => GuiEndPoint = Program.GetEndPoint (v));
			p.Add ("no-result", v => resultOutput = junitResultOutput = null);
			p.Add ("package-name=", v => packageName = v);
			p.Add ("result=", v => resultOutput = v);
			p.Add ("junit-result=", v => junitResultOutput = v);
			p.Add ("log-level=", v => logLevel = v);
			p.Add ("local-log-level=", v => localLogLevel = int.Parse (v));
			p.Add ("dependency=", v => dependencies.Add (v));
			p.Add ("optional-gui", v => OptionalGui = true);
			p.Add ("no-junit", v => noJUnit = true);
			p.Add ("set=", v => customSettings = v);
			p.Add ("category=", v => Category = v);
			p.Add ("features=", v => Features = v);
			p.Add ("martin=", v => MartinTest = v);
			p.Add ("debug", v => debugMode = true);
			p.Add ("save-options", v => saveSettings = true);
			p.Add ("show-categories", v => ShowCategories = true);
			p.Add ("show-features", v => ShowFeatures = true);
			p.Add ("show-config", v => ShowCategories = ShowFeatures = true);
			p.Add ("ios-device-type=", v => iosDeviceType = v);
			p.Add ("ios-runtime=", v => iosRuntime = v);
			p.Add ("android-activity=", v => AndroidActivity = v);
			p.Add ("android-serial=", v => AndroidSerial = v);
			p.Add ("stdout=", v => stdout = v);
			p.Add ("sdkroot=", v => sdkRoot = v);
			p.Add ("android-sdkroot=", v => androidSdkRoot = v);
			p.Add ("save-logcat=", v => SaveLogCat = v);
			p.Add ("jenkins", v => Jenkins = true);
			p.Add ("jenkins-html=", v => JenkinsHtml = v);
			p.Add ("output-dir=", v => outputDir = v);
			p.Add ("repeat=", v => repeat = int.Parse (v));
			p.Add ("dont-save-logging", v => dontSaveLogging = true);
			p.Add ("jenkins-job=", "Jenkins Job Path", v => JenkinsJobPath = v);
			p.Add ("root-path=", v => RootPath = v);
			var arguments = p.Parse (args);

			PackageName = packageName;

			if (assembly != null) {
				Command = Command.Local;

				if (arguments.Count > 0 && Enum.TryParse (arguments[0], true, out Command command)) {
					arguments.RemoveAt (0);
					Command = command;

				}
			} else {
				if (arguments.Count < 1)
					throw new ProgramException ("Missing argument.");

				Command command;
				if (!Enum.TryParse (arguments[0], true, out command))
					throw new ProgramException ("Unknown command.");
				arguments.RemoveAt (0);
				Command = command;
			}

			Arguments = arguments;

			var dependencyAssemblies = new Assembly[dependencies.Count];
			for (int i = 0; i < dependencyAssemblies.Length; i++) {
				dependencyAssemblies[i] = Assembly.LoadFile (dependencies[i]);
			}

			Dependencies = dependencyAssemblies;

			switch (Command) {
			case Command.Listen:
				if (EndPoint == null)
					EndPoint = Program.GetLocalEndPoint ();
				break;
			case Command.Local:
				if (assembly != null) {
					if (arguments.Count != 0) {
						arguments.ForEach (a => Program.PrintError ($"Unexpected remaining argument: {a}"));
						throw new ProgramException ("Unexpected extra argument.");
					}
					Assembly = assembly;
				} else if (arguments.Count == 1) {
					Application = arguments[0];
					Assembly = Assembly.LoadFile (arguments[0]);
					arguments.RemoveAt (0);
				} else if (EndPoint == null) {
					throw new ProgramException ("Missing endpoint");
				}
				break;
			case Command.Connect:
				if (assembly != null)
					throw new ProgramException ("Cannot use 'connect' with assembly.");
				if (arguments.Count == 1) {
					EndPoint = Program.GetEndPoint (arguments[0]);
					arguments.RemoveAt (0);
				} else if (arguments.Count == 0) {
					if (EndPoint == null)
						throw new ProgramException ("Missing endpoint");
				} else {
					arguments.ForEach (a => Program.PrintError ($"Unexpected remaining argument: {a}"));
					throw new ProgramException ("Unexpected extra argument.");
				}
				break;
			case Command.Fork:
				if (arguments.Count == 1) {
					EndPoint = Program.GetEndPoint (arguments[0]);
					arguments.RemoveAt (0);
				} else if (arguments.Count == 0) {
					if (EndPoint == null)
						throw new ProgramException ("Missing endpoint");
				} else {
					arguments.ForEach (a => Program.PrintError ($"Unexpected remaining argument: {a}"));
					throw new ProgramException ("Unexpected extra argument.");
				}
				break;
			case Command.Simulator:
			case Command.Device:
			case Command.TVOS:
				if (arguments.Count < 1)
					throw new ProgramException ("Expected .app argument");
				Application = arguments[0];
				arguments.RemoveAt (0);

				if (EndPoint == null)
					EndPoint = Program.GetLocalEndPoint ();
				break;
			case Command.Mac:
				if (arguments.Count < 1)
					throw new ProgramException ("Expected .app argument");
				Application = arguments[0];
				arguments.RemoveAt (0);

				if (EndPoint == null)
					EndPoint = Program.GetLocalEndPoint ();
				break;
			case Command.Android:
				if (arguments.Count != 0)
					throw new ProgramException ("Unexpected extra arguments");

				if (EndPoint == null)
					EndPoint = Program.GetLocalEndPoint ();
				break;
			case Command.Avd:
			case Command.Emulator:
				if (arguments.Count != 0)
					throw new ProgramException ("Unexpected extra arguments");

				break;
			case Command.Apk:
				if (arguments.Count != 1)
					throw new ProgramException ("Expected .apk argument");

				Application = arguments[0];
				arguments.RemoveAt (0);
				break;
			case Command.Result:
				if (arguments.Count != 1)
					throw new ProgramException ("Expected TestResult.xml argument");
				resultOutput = arguments[0];
				arguments.RemoveAt (0);
				break;
			default:
				throw new ProgramException ("Unknown command '{0}'.", Command);
			}

			OutputDirectory = outputDir;

			if (!string.IsNullOrEmpty (OutputDirectory) && !Directory.Exists (OutputDirectory))
				Directory.CreateDirectory (OutputDirectory);

			if (stdout != null) {
				var fullPath = Path.Combine ("artifact", stdout);
				if (JenkinsJobPath != null)
					fullPath = Path.Combine (JenkinsJobPath, fullPath);
				JenkinsStdOutLink = $"<a href=\"{fullPath}\">{stdout}</a>";
			}

			StdOut = MakeAbsolute (OutputDirectory, stdout);
			ResultOutput = MakeAbsolute (OutputDirectory, resultOutput);
			if (!noJUnit)
				JUnitResultOutput = MakeAbsolute (OutputDirectory, junitResultOutput);
			JenkinsHtml = MakeAbsolute (OutputDirectory, JenkinsHtml);

			if (settingsFile != null) {
				if (saveSettings == null)
					saveSettings = true;
			} else if (Assembly != null) {
				var name = Assembly.GetName ().Name;
				var path = Environment.GetFolderPath (Environment.SpecialFolder.LocalApplicationData);
				path = Path.Combine (path, "Xamarin", "AsyncTests");

				if (!Directory.Exists (path))
					Directory.CreateDirectory (path);

				settingsFile = Path.Combine (path, name + ".xml");
			}

			if (settingsFile == null || !File.Exists (settingsFile)) {
				Settings = SettingsBag.CreateDefault ();
			} else {
				Settings = LoadSettings (settingsFile);
			}

			if (repeat != null) {
				Settings.Repeat = true;
				Settings.RepeatCount = repeat.Value;
			}

			if (customSettings != null) {
				ParseSettings (Settings, customSettings);
				if (saveSettings ?? false)
					SaveSettings (Settings, settingsFile);
			}

			if (debugMode) {
				Settings.LogLevel = -1;
				Settings.LocalLogLevel = -1;
				Settings.DisableTimeouts = true;
			}

			if (logLevel != null) {
				var (defaultLevel, categories) = ParseLogLevel (logLevel);
				Settings.LogLevel = defaultLevel;
				foreach (var entry in categories)
					Settings.SetLogLevel (entry.Key, entry.Value);
				if (localLogLevel == null)
					Settings.LocalLogLevel = defaultLevel;
			}
			if (localLogLevel != null)
				Settings.LocalLogLevel = localLogLevel.Value;

			if (!debugMode)
				Settings.DisableTimeouts = Settings.LogLevel > SettingsBag.DisableTimeoutsAtLogLevel;

			if (dontSaveLogging)
				Settings.DontSaveLogging = true;

			if (MartinTest != null) {
				if (Category != null)
					throw new ProgramException ("Cannot use both --category and --martin");
				Category = "martin";
				Settings.MartinTest = MartinTest;
			}

			bool needSdk = false, needAndroidSdk = false;

			switch (Command) {
			case Command.Device:
			case Command.Simulator:
				IOSDeviceType = iosDeviceType ?? GetEnvironmentVariable ("IOS_DEVICE_TYPE", "iPhone-5s");
				IOSRuntime = iosRuntime ?? GetEnvironmentVariable ("IOS_RUNTIME", "iOS-10-3");
				needSdk = true;
				break;
			case Command.TVOS:
				IOSDeviceType = iosDeviceType ?? GetEnvironmentVariable ("IOS_DEVICE_TYPE", "Apple-TV-1080p");
				IOSRuntime = iosRuntime ?? GetEnvironmentVariable ("IOS_RUNTIME", "tvOS-9-2");
				needSdk = true;
				break;
			case Command.Android:
			case Command.Avd:
			case Command.Emulator:
			case Command.Apk:
				needAndroidSdk = true;
				break;
			}

			if (needSdk)
				SdkRoot = sdkRoot ?? GetEnvironmentVariable ("XCODE_DEVELOPER_ROOT", "/Applications/Xcode.app/Contents/Developer");

			if (needAndroidSdk) {
				AndroidSdkRoot = androidSdkRoot ?? Environment.GetEnvironmentVariable ("ANDROID_SDK_PATH");
				if (String.IsNullOrEmpty (AndroidSdkRoot)) {
					var home = Environment.GetFolderPath (Environment.SpecialFolder.Personal);
					AndroidSdkRoot = Path.Combine (home, "Library", "Developer", "Xamarin", "android-sdk");
				}
			}
		}

		static (int level, Dictionary<string,int> categories) ParseLogLevel (string argument)
		{
			var categories = new Dictionary<string, int> ();
			if (int.TryParse (argument, out int level))
				return (level, categories);
			int? defaultLevel = null;
			foreach (var part in argument.Split (',')) {
				var pos = part.IndexOf (':');
				if (pos < 0) {
					if (defaultLevel != null)
						throw new ProgramException ("Duplicate default log level");
					defaultLevel = int.Parse (part);
				} else {
					var name = part.Substring (0, pos).Trim ();
					categories.Add (name, int.Parse (part.Substring (pos + 1)));
				}
			}
			return (defaultLevel ?? 0, categories);
		}

		static SettingsBag LoadSettings (string file)
		{
			Program.Debug ("Loading settings from {0}.", file);
			using (var reader = new StreamReader (file)) {
				var doc = XDocument.Load (reader);
				return TestSerializer.ReadSettings (doc.Root);
			}
		}

		static void SaveSettings (SettingsBag settings, string file)
		{
			Program.Debug ("Saving settings to {0}.", file);
			using (var writer = new StreamWriter (file)) {
				var xws = new XmlWriterSettings ();
				xws.Indent = true;

				using (var xml = XmlTextWriter.Create (writer, xws)) {
					var node = TestSerializer.WriteSettings (settings);
					node.WriteTo (xml);
					xml.Flush ();
				}
			}
		}

		static string MakeAbsolute (string directory, string file)
		{
			string fullName;
			if (string.IsNullOrEmpty (directory) || string.IsNullOrEmpty (file) || Path.IsPathRooted (file))
				fullName = file;
			else
				fullName = Path.Combine (directory, file);

			var fullDir = Path.GetDirectoryName (fullName);
			if (!string.IsNullOrEmpty (fullDir) && !Directory.Exists (fullDir))
				Directory.CreateDirectory (fullDir);
			return fullName;
		}

		static void ParseSettings (SettingsBag settings, string arg)
		{
			var parts = arg.Split (',');
			foreach (var part in parts) {
				var pos = part.IndexOf ('=');
				if (pos > 0) {
					var key = part.Substring (0, pos);
					var value = part.Substring (pos + 1);
					Program.Debug ("SET: |{0}|{1}|", key, value);
					if (key[0] == '-')
						throw new InvalidOperationException ();
					settings.SetValue (key, value);
				} else if (part[0] == '-') {
					var key = part.Substring (1);
					settings.RemoveValue (key);
				} else {
					throw new InvalidOperationException ();
				}
			}
		}

		internal static string GetEnvironmentVariable (string name, string defaultValue)
		{
			var value = Environment.GetEnvironmentVariable (name);
			if (string.IsNullOrEmpty (value))
				value = defaultValue;
			return value;
		}

		internal bool ModifyConfiguration (TestConfiguration config)
		{
			bool modified = false;

			if (Category != null) {
				if (string.Equals (Category, "all", StringComparison.OrdinalIgnoreCase))
					config.CurrentCategory = TestCategory.All;
				else if (string.Equals (Category, "martin", StringComparison.OrdinalIgnoreCase))
					config.CurrentCategory = TestCategory.Martin;
				else
					config.CurrentCategory = config.Categories.First (c => c.Name.Equals (Category));
				modified = true;
			}

			if (Features != null) {
				modified = true;
				var parts = Features.Split (',');
				foreach (var part in parts) {
					var name = part;
					bool enable = true;
					if (part[0] == '-') {
						name = part.Substring (1);
						enable = false;
					} else if (part[0] == '+') {
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
						if (!feature.CanModify)
							throw new ProgramException ($"Cannot modify TestFeature '{feature.Name}'.");
						config.SetIsEnabled (feature, enable);
					}
				}
			}

			return modified;
		}

		internal bool UpdateConfiguration (TestSession session)
		{
			var config = session.Configuration;

			var modified = ModifyConfiguration (config);

			bool done = false;
			if (ShowCategories) {
				Program.WriteLine ("Test Categories:");
				foreach (var category in session.ConfigurationProvider.Categories) {
					var builtinText = category.IsBuiltin ? " (builtin)" : string.Empty;
					var explicitText = category.IsExplicit ? " (explicit)" : string.Empty;
					var currentText = config.CurrentCategory != null && config.CurrentCategory.Name.Equals (category.Name) ? " (current)" : string.Empty;
					Program.WriteLine ("  {0}{1}{2}{3}", category.Name, builtinText, explicitText, currentText);
				}
				Program.WriteLine ();
				done = true;
			}

			if (ShowFeatures) {
				Program.WriteLine ("Test Features:");
				foreach (var feature in session.ConfigurationProvider.Features) {
					var constText = feature.Constant != null ? string.Format (" (const = {0})", feature.Constant.Value ? "enabled" : "disabled") : string.Empty;
					var defaultText = feature.DefaultValue != null ? string.Format (" (default = {0})", feature.DefaultValue.Value ? "enabled" : "disabled") : string.Empty;
					var currentText = feature.CanModify ? string.Format (" ({0})", config.IsEnabled (feature) ? "enabled" : "disabled") : string.Empty;
					Program.WriteLine ("  {0,-30} {1}{2}{3}{4}", feature.Name, feature.Description, constText, defaultText, currentText);
				}
				Program.WriteLine ();
				done = true;
			}

			if (done)
				Environment.Exit (0);

			if (modified && (saveSettings ?? false))
				SaveSettings (Settings, settingsFile);

			return modified;
		}
	}
}
