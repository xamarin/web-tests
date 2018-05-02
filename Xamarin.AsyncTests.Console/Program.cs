﻿//
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
using System.Text;
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
		public ProgramOptions Options {
			get;
		}

		public Assembly Assembly => Options.Assembly;

		public string PackageName => Options.PackageName;

		public SettingsBag Settings => Options.Settings;

		public TestLogger Logger {
			get;
		}

		public ApplicationLauncher Launcher {
			get;
		}

		public LauncherOptions LauncherOptions {
			get;
		}

		internal DroidHelper DroidHelper {
			get;
		}

		internal static bool Jenkins {
			get;
			private set;
		}

		internal static TextWriter JenkinsHtml {
			get;
			private set;
		}

		internal static TextWriter StdOut {
			get;
			private set;
		}

		TestSession session;
		TestResult result;
		DateTime startTime, endTime;

		public static void Run (Assembly assembly, string[] args)
		{
			SD.Debug.AutoFlush = true;
			SD.Debug.Listeners.Add (new SD.ConsoleTraceListener ());

			DependencyInjector.RegisterAssembly (typeof(PortableSupportImpl).Assembly);

			Program program = null;
			int result;
			try {
				program = new Program (assembly, args);
			} catch (Exception ex) {
				PrintException (ex);
				Environment.Exit (-1);
			}

			try {
				var task = program.Run (CancellationToken.None);
				task.Wait ();
				result = task.Result;
			} catch (Exception ex) {
				PrintException (ex);
				result = -1;
			}
			program.Finish ();
			Environment.Exit (result);
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
					PrintError ($"External tool '{toolEx.Tool}' failed:\n{toolEx.ErrorOutput}\n");
				else
					PrintError ($"External tool '{toolEx.Tool}' failed:\n{toolEx}\n");
				return;
			}

			PrintError (ex.ToString ());
		}

		static void Main (string[] args)
		{
			Run (null, args);
		}

		Program (Assembly assembly, string[] args)
		{
			Options = new ProgramOptions (assembly, args);

			switch (Options.Command) {
			case Command.Simulator:
			case Command.Device:
			case Command.TVOS:
				Launcher = new TouchLauncher (this);
				break;
			case Command.Mac:
				if (Options.StdOut != null)
					StdOut = new StreamWriter (Options.StdOut);
				Launcher = new MacLauncher (this);
				break;
			case Command.Android:
				if (Options.StdOut != null)
					StdOut = new StreamWriter (Options.StdOut);
				DroidHelper = new DroidHelper (this);
				Launcher = new DroidLauncher (this);
				break;
			case Command.Avd:
			case Command.Emulator:
			case Command.Apk:
				DroidHelper = new DroidHelper (this);
				break;
			default:
				if (Options.StdOut != null)
					StdOut = new StreamWriter (Options.StdOut);
				break;
			}

			if (StdOut != null)
				SD.Debug.Listeners.Add (new SD.TextWriterTraceListener (StdOut));

			Jenkins = Options.Jenkins;

			Logger = new TestLogger (new ConsoleLogger (this));

			if (Options.JenkinsHtml != null)
				JenkinsHtml = new StreamWriter (Options.JenkinsHtml);

			if (Launcher != null) {
				LauncherOptions = new LauncherOptions {
					Category = Options.Category, Features = Options.Features
				};
			}

			ExternalDomainSupport domainSupport = null;
			switch (Options.Command) {
			case Command.Local:
				domainSupport = new ExternalDomainSupport (this);
				break;
			}

			if (domainSupport != null)
				DependencyInjector.RegisterDependency<IExternalDomainSupport> (() => domainSupport);
		}

		void Finish ()
		{
			if (JenkinsHtml != null) {
				if (Options.StdOut != null)
					JenkinsHtml.WriteLine ($"<p>Output: {Options.JenkinsStdOutLink}.");
				JenkinsHtml.Flush ();
				JenkinsHtml.Dispose ();
			}
			if (StdOut != null) {
				StdOut.Flush ();
				StdOut.Dispose ();
			}
		}

		void LogInfo (string message)
		{
			if (Options.Jenkins)
				System.Console.WriteLine ($"[info] {message}");
			Debug (message);
		}

		internal static void PrintError (string message, Exception error = null)
		{
			if (Jenkins) {
				WriteLine ($"[error] {message}");
				if (error != null)
					WriteLine ($"[error] {error}");
			} else {
				WriteLine ($"ERROR: {message}");
				if (error != null)
					WriteLine (error.ToString ());
			}

			if (JenkinsHtml != null) {
				JenkinsHtml.WriteLine ("<p><b>{message}</b></p>");
				if (error != null)
					JenkinsHtml.WriteLine ("<pre>{error}</pre>");
			}

			void WriteLine (string text)
			{
				System.Console.Error.WriteLine (text);
				if (StdOut != null)
					StdOut.WriteLine (text);
			}
		}

		internal static void WriteLine ()
		{
			System.Console.WriteLine ();
			if (StdOut != null)
				StdOut.WriteLine ();
		}

		internal static void WriteLine (string message, params object[] args)
		{
			System.Console.WriteLine (message, args);
			if (StdOut != null)
				StdOut.WriteLine (message, args);
		}

		internal static void Debug (string message)
		{
			SD.Debug.WriteLine (message);
		}

		internal static void Debug (string message, params object[] args)
		{
			Debug (string.Format (message, args));
		}

		internal static IPEndPoint GetEndPoint (string text)
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

		internal static IPEndPoint GetLocalEndPoint ()
		{
			return new IPEndPoint (PortableSupportImpl.LocalAddress, 11111);
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

		async Task<int> Run (CancellationToken cancellationToken)
		{
			LogInfo ("Running test suite.");

			bool success = false;
			int? exitCode = null;

			switch (Options.Command) {
			case Command.Local:
				exitCode = await RunLocal (cancellationToken).ConfigureAwait (false);
				break;
			case Command.Connect:
				exitCode = await ConnectToServer (cancellationToken).ConfigureAwait (false);
				break;
			case Command.Gui:
				exitCode = await ConnectToGui (cancellationToken).ConfigureAwait (false);
				break;
			case Command.Listen:
				exitCode = await WaitForConnection (cancellationToken).ConfigureAwait (false);
				break;
			case Command.Fork:
				exitCode = await ConnectToForkedParent (cancellationToken).ConfigureAwait (false);
				break;
			case Command.Simulator:
			case Command.Device:
			case Command.Mac:
			case Command.Android:
			case Command.TVOS:
				exitCode = await LaunchApplication (cancellationToken).ConfigureAwait (false);
				break;
			case Command.Avd:
				success = await DroidHelper.CheckAvd (cancellationToken).ConfigureAwait (false);
				break;
			case Command.Emulator:
				success = await DroidHelper.CheckEmulator (cancellationToken).ConfigureAwait (false);
				break;
			case Command.Apk:
				await DroidHelper.InstallApk (Options.Application, cancellationToken).ConfigureAwait (false);
				success = true;
				break;
			case Command.Result:
				success = await ShowResult (cancellationToken).ConfigureAwait (false);
				break;
			default:
				throw new NotImplementedException ();
			}

			return exitCode ?? (success ? 0 : 1);
		}

		async Task<int> ConnectToGui (CancellationToken cancellationToken)
		{
			var framework = TestFramework.GetLocalFramework (PackageName, Assembly, Options.Dependencies);

			TestServer server;
			try {
				var endpoint = GetPortableEndPoint (Options.GuiEndPoint);
				server = await TestServer.ConnectToGui (this, endpoint, framework, cancellationToken);
			} catch (SocketException ex) {
				if (ex.SocketErrorCode == SocketError.ConnectionRefused && Options.OptionalGui) {
					return await RunLocal (cancellationToken);
				}
				throw;
			}

			Options.UpdateConfiguration (server.Session);

			cancellationToken.ThrowIfCancellationRequested ();
			await server.WaitForExit (cancellationToken);
			return 0;
		}

		int ExitCodeForResult {
			get {
				if (result == null)
					return 4;
				switch (result.Status) {
				case TestStatus.Success:
					return 0;
				case TestStatus.Unstable:
					return 2;
				case TestStatus.Canceled:
					return 3;
				default:
					return 1;
				}
			}
		}

		async Task<int> RunLocal (CancellationToken cancellationToken)
		{
			var framework = TestFramework.GetLocalFramework (PackageName, Assembly, Options.Dependencies);

			cancellationToken.ThrowIfCancellationRequested ();
			session = TestSession.CreateLocal (this, framework);
			Options.UpdateConfiguration (session);

			PrintSummary ();

			TestCase test;
			if (Options.RootPath != null) {
				var doc = XDocument.Load (Options.RootPath);
				test = await session.ResolveFromPath (doc.Root, cancellationToken);
			} else {
				test = session.RootTestCase;
			}

			Debug ("Got test: {0}", test.Path.FullName);
			startTime = DateTime.Now;
			result = await session.Run (test, cancellationToken);
			endTime = DateTime.Now;
			Debug ("Got result: {0} {1}", result.Status, test.Path.FullName);

			SaveResult ();

			return ExitCodeForResult;
		}

		async Task<int> ConnectToServer (CancellationToken cancellationToken)
		{
			var endpoint = GetPortableEndPoint (Options.EndPoint);
			var server = await TestServer.ConnectToServer (this, endpoint, cancellationToken);
			cancellationToken.ThrowIfCancellationRequested ();

			session = server.Session;
			if (Options.UpdateConfiguration (session))
				await session.UpdateSettings (cancellationToken);

			PrintSummary ();

			var test = session.RootTestCase;
			cancellationToken.ThrowIfCancellationRequested ();

			Debug ("Got test: {0}", test.Path.FullName);
			startTime = DateTime.Now;
			result = await session.Run (test, cancellationToken);
			endTime = DateTime.Now;
			cancellationToken.ThrowIfCancellationRequested ();
			Debug ("Got result: {0} {1}", result.Status, test.Path.FullName);

			SaveResult ();

			await server.Stop (cancellationToken);

			return ExitCodeForResult;
		}

		async Task<int> ConnectToForkedParent (CancellationToken cancellationToken)
		{
			var framework = TestFramework.GetLocalFramework (PackageName, Assembly, Options.Dependencies);
			var endpoint = GetPortableEndPoint (Options.EndPoint);
			var server = await TestServer.ConnectToForkedParent (this, endpoint, framework, cancellationToken);
			cancellationToken.ThrowIfCancellationRequested ();

			Debug ($"Connected to forked parent."); 

			await server.Session.WaitForShutdown (cancellationToken).ConfigureAwait (false);

			Debug ($"Forked child session exited.");

			await server.WaitForExit (cancellationToken);

			Debug ($"Forked child done.");

			await server.Stop (cancellationToken);

			Debug ($"Forked child exiting.");

			return 0;
		}

		async Task<int> LaunchApplication (CancellationToken cancellationToken)
		{
			var endpoint = GetPortableEndPoint (Options.EndPoint);

			TestServer server;
			try {
				server = await TestServer.LaunchApplication (this, endpoint, Launcher, LauncherOptions, cancellationToken);
			} catch (LauncherErrorException ex) {
				PrintException (ex);
				Environment.Exit (255);
				throw;
			}

			cancellationToken.ThrowIfCancellationRequested ();

			Debug ("Test app launched.");
			var exitCode = await RunRemoteSession (server, cancellationToken);

			Debug ("Application finished.");

			return exitCode;
		}

		async Task<int> WaitForConnection (CancellationToken cancellationToken)
		{
			var endpoint = GetPortableEndPoint (Options.EndPoint);
			var server = await TestServer.WaitForConnection (this, endpoint, cancellationToken);
			cancellationToken.ThrowIfCancellationRequested ();

			Debug ("Got server connection.");
			return await RunRemoteSession (server, cancellationToken);
		}

		async Task<int> RunRemoteSession (TestServer server, CancellationToken cancellationToken)
		{
			session = server.Session;
			if (Options.UpdateConfiguration (session))
				await session.UpdateSettings (cancellationToken);

			PrintSummary ();

			var test = session.RootTestCase;
			cancellationToken.ThrowIfCancellationRequested ();

			Debug ("Got test: {0}", test.Path.FullName);
			startTime = DateTime.Now;
			result = await session.Run (test, cancellationToken);
			endTime = DateTime.Now;
			cancellationToken.ThrowIfCancellationRequested ();
			Debug ("Got result: {0} {1}", result.Status, result.Path.FullName);

			SaveResult ();

			await server.Stop (cancellationToken);

			return ExitCodeForResult;
		}

		void PrintSummary ()
		{
			var config = session.Configuration;
			var category = config.CurrentCategory.Name;
			var shortText = new StringBuilder ();
			var detailedText = new List<string> ();
			detailedText.Add ($"Test Category: {category}");
			foreach (var feature in config.Features) {
				if (!feature.CanModify || feature.Constant != null)
					continue;
				var defaultValue = feature.DefaultValue ?? false;
				var enabled = config.IsEnabled (feature);
				if (enabled == defaultValue)
					continue;
				var prefix = enabled ? "+" : "-";
				detailedText.Add ($"Test Feature: {prefix}{feature.Name}");
				if (shortText.Length > 0)
					shortText.Append (", ");
				shortText.Append ($"{prefix}{feature.Name}");
			}
			var categoryText = category;
			if (shortText.Length > 0)
				categoryText += $" ({shortText})";

			var header = $"Test Suite: {session.App.PackageName ?? session.Name} - {categoryText}";
			if (JenkinsHtml != null)
				JenkinsHtml.WriteLine ($"<h2>{header}</h2>");
			LogInfo (header);

			foreach (var line in detailedText)
				LogInfo (line);
		}

		void SaveResult ()
		{
			LogInfo ($"Test Result: {result.Status}");
			LogInfo ($"{countTests} tests, {countSuccess} passed, {countErrors} errors, {countUnstable} unstable, {countIgnored} ignored.");
			LogInfo ($"Total time: {endTime - startTime}.");
			if (JenkinsHtml != null) {
				JenkinsHtml.WriteLine ($"<p>Test Result: {result.Status}");
				JenkinsHtml.WriteLine ($"<br>{countTests} tests, {countSuccess} passed, {countErrors} errors, {countUnstable} unstable, {countIgnored} ignored.");
				JenkinsHtml.WriteLine ($"<br>Total time: {endTime - startTime}.</p>");
			}

			if (Options.ResultOutput != null) {
				var serialized = TestSerializer.WriteTestResult (result);
				var settings = new XmlWriterSettings ();
				settings.Indent = true;
				using (var writer = XmlTextWriter.Create (Options.ResultOutput, settings))
					serialized.WriteTo (writer);
				Debug ("Result written to {0}.", Options.ResultOutput);
			}

			if (Options.JUnitResultOutput != null) {
				JUnitResultPrinter.Print (result, Options.JUnitResultOutput);
				Debug ("JUnit result written to {0}.", Options.JUnitResultOutput);
			}

			var printer = new ResultPrinter (System.Console.Out, result);
			printer.Print ();

			if (StdOut != null) {
				var stdoutPrinter = new ResultPrinter (StdOut, result);
				stdoutPrinter.Print ();
			}
		}

		async Task<bool> ShowResult (CancellationToken cancellationToken)
		{
			await Task.Yield ();

			var printer = ResultPrinter.Load (global::System.Console.Out, Options.ResultOutput);
			var ret = printer.Print ();

			if (Options.JUnitResultOutput != null) {
				JUnitResultPrinter.Print (printer.Result, Options.JUnitResultOutput);
				Debug ("JUnit result written to {0}.", Options.JUnitResultOutput);
			}

			return ret;
		}

		void OnLogMessage (string message)
		{
			Debug (message);
		}

		void OnLogMessage (string format, params object[] args)
		{
			OnLogMessage (string.Format (format, args));
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
		int countUnstable;
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
				case TestStatus.Unstable:
					++countUnstable;
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
						Program.OnLogMessage ("ERROR: {0}\n", entry.Error);
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

