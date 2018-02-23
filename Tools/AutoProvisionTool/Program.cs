//
// Program.cs
//
// Author:
//       Martin Baulig <mabaul@microsoft.com>
//
// Copyright (c) 2018 Xamarin, Inc.
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
using System.Text;
using System.Reflection;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xamarin.AsyncTests.Console;
using NDesk.Options;
using Mono.Unix;

namespace AutoProvisionTool
{
	class Program
	{
		enum Command {
			Version,
			Provision
		}

		static TextWriter Output;
		static string HtmlFile;
		static string OutputFile;
		static string JenkinsJobPath;
		static TextWriter HtmlOutput;

		internal static bool SpecificCommit {
			get;
			private set;
		}

		internal static int MaxFallback {
			get;
			private set;
		}

		public static void Main (string[] args)
		{
			var products = new List<Product> ();
			string summaryFile = null;
			int? maxFallback = null;
			var p = new OptionSet ();
			p.Add ("summary=", "Write summary to file", v => summaryFile = v);
			p.Add ("out=", "Write output to file", v => OutputFile = v);
			p.Add ("html=", "Write html output to file", v => HtmlFile = v);
			p.Add ("mono=", "Mono branch", v => products.Add (new MonoProduct (v)));
			p.Add ("jenkins-job=", "Jenkins Job Path", v => JenkinsJobPath = v);
			p.Add ("xi=", "Xamarin.iOS branch", v => products.Add (new IOSProduct (v)));
			p.Add ("xm=", "Xamarin.Mac branch", v => products.Add (new MacProduct (v)));
			p.Add ("xa=", "Xamarin.Android branch", v => products.Add (new AndroidProduct (v)));
			p.Add ("specific", "Use this specific commit", v => SpecificCommit = true);
			p.Add ("max-fallback=", "Maximum number of fallback commits to try", v => maxFallback = int.Parse (v));

			if (SpecificCommit)
				MaxFallback = 0;
			else if (maxFallback != null) {
				if (maxFallback.Value <= 0) {
					SpecificCommit = true;
					MaxFallback = 0;
				} else {
					MaxFallback = maxFallback.Value;
				}
			} else {
				MaxFallback = 25;
			}

			List<string> arguments;
			try {
				arguments = p.Parse (args);
			} catch (OptionException ex) {
				Usage (ex.Message);
				return;
			}

			if (arguments.Count < 1) {
				Usage ("Missing command.");
				return;
			}

			Command command;
			if (!Enum.TryParse (arguments[0], true, out command)) {
				Usage ("Invalid command.");
				return;
			}

			if (OutputFile != null) {
				Output = new StreamWriter (OutputFile);
				Log ($"Logging output to {OutputFile}.");
			}

			if (HtmlFile != null) {
				HtmlOutput = new StreamWriter (HtmlFile);
				HtmlOutput.WriteLine ("<h2>Provision Summary</h2>");
			}

			try {
				Run (command, products).Wait ();
			} catch {
				LogError ("Provisioning failed.");
			}

			var versionProducts = new List<Product> (products);
			if (versionProducts.Count == 0)
				versionProducts.AddRange (Product.GetDefaultProducts ());

			var shortSummary = GetVersionSummary (products, VersionFormat.Summary).Result;
			var detailedSummary = GetVersionSummary (versionProducts, VersionFormat.Normal).Result;

			if (summaryFile != null) {
				Log ($"Writing version summary to {summaryFile}.");
				using (var writer = new StreamWriter (summaryFile)) {
					writer.WriteLine (shortSummary);
					writer.Flush ();
				}
			}

			Log ("Short version summary:");
			LogOutput (shortSummary);
			Log ("Detailed version summary:");
			LogOutput (detailedSummary); 

			Log ("Provisioning complete.");

			if (Output != null) {
				Output.Flush ();
				Output.Dispose ();
			}

			Finish ();

			void Usage (string message = null)
			{
				var me = Assembly.GetEntryAssembly ().GetName ().Name;
				using (var sw = new StringWriter ()) {
					if (!string.IsNullOrEmpty (message)) {
						sw.WriteLine ($"Usage error: {message}");
						sw.WriteLine ();
					}
					sw.WriteLine ($"Usage: {me} [options] command [args]");
					sw.WriteLine ();
					sw.WriteLine ("Options:");
					p.WriteOptionDescriptions (sw);
					sw.WriteLine ();
					var options = sw.ToString ();
					LogError (options);
				}
			}
		}

		static Task Run (Command command, IList<Product> products)
		{
			switch (command) {
			case Command.Version:
				return PrintVersions (Output ?? Console.Error, products);
			case Command.Provision:
				return ProvisionProducts (products);
			default:
				throw new InvalidOperationException ();
			}
		}

		static async Task PrintVersions (TextWriter writer, IList<Product> products)
		{
			if (products.Count == 0)
				products = Product.GetDefaultProducts ();
			foreach (var product in products) {
				var version = await product.PrintVersion (VersionFormat.Full).ConfigureAwait (false);
				writer.WriteLine ($"{product.Name}: {version}");
			}
		}

		static async Task<string> GetVersionSummary (IList<Product> products, VersionFormat format)
		{
			if (products.Count == 0)
				return "No products enabled.";
			var sb = new StringBuilder ();
			foreach (var product in products) {
				var version = await product.PrintVersion (VersionFormat.Summary).ConfigureAwait (false);
				if (format == VersionFormat.Summary) {
					if (sb.Length > 0)
						sb.Append ("<br>");
					sb.Append ($"{product.ShortName}: {version}");
				} else {
					sb.AppendLine ($"{product.Name}: {version}");
				}
			}
			return sb.ToString ();
		}

		static async Task ProvisionProducts (IList<Product> products)
		{
			foreach (var product in products) {
				var oldVersion = await product.PrintVersion (VersionFormat.Normal).ConfigureAwait (false);
				if (string.Equals (product.Branch, "current", StringComparison.OrdinalIgnoreCase)) {
					Log ($"Keeping current {product.Name}.");
					var currentVersion = await product.PrintVersion (VersionFormat.Normal);
					Log ($"Current {product.Name} version: {currentVersion}");
					LogHtml ($"<p>Keeping current {product.Name} version {currentVersion}.");
					await PrintFullVersion (product);
					continue;
				}
				Log ($"Provisioning {product.Name} from {product.Branch}.");
				Package package;
				try {
					package = await product.Provision ();
				} catch (Exception ex) {
					LogError ($"Failed to provision {product.Name} from {product.Branch}:\n{ex}");
					throw;
				}
				var newVersion = await product.PrintVersion (VersionFormat.Normal);
				Log ($"Old {product.Name} version: {oldVersion}");
				Log ($"New {product.Name} version: {newVersion}");
				LogHtml ($"<p>Provisioned {product.Name} version {newVersion} from " +
				         $"{BranchLink (product)} commit {CommitLink (package)} ({PackageLink (package)}).");
				await PrintFullVersion (product);
			}

			string BranchLink (Product product)
			{
				return $"<a href=\"https://github.com/{product.RepoOwner}/{product.RepoName}/commits/{product.Branch}\">{product.RepoName}/{product.Branch}</a>";
			}

			string CommitLink (Package package)
			{
				return $"<a href=\"https://github.com/{package.Product.RepoOwner}/{package.Product.RepoName}/commit/{package.Commit.Sha}\">{package.Commit.Sha.Substring (0, 7)}</a>";
			}

			string PackageLink (Package package)
			{
				return $"<a href=\"{package.TargetUri}\">{Path.GetFileName (package.TargetUri.AbsolutePath)}</a>";
			}

			async Task PrintFullVersion (Product product)
			{
				var fullVersion = await product.PrintVersion (VersionFormat.Full).ConfigureAwait (false);
				LogHtml ($"<pre>{fullVersion}</pre>");
			}
		}

		internal const string ME = "AutoProvisionTool";

		public static void Debug (string message)
		{
			Log (message);
		}

		public static void Debug (string format, params object[] args)
		{
			Log (string.Format (format, args));
		}

		public static void LogHtml (string message)
		{
			if (HtmlOutput != null)
				HtmlOutput.WriteLine (message);
		}

		public static async Task<string> RunCommandWithOutput (
			string command, string args, CancellationToken cancellationToken)
		{
			var output = await ProcessHelper.RunCommandWithOutput (
				command, args, cancellationToken).ConfigureAwait (false);
			LogOutput (output);
			return output;
		}

		public static void LogOutput (string output)
		{
			using (var reader = new StringReader (output)) {
				string line;
				while ((line = reader.ReadLine ()) != null) {
					Log ($"    {line}");
				}
			}
		}

		public static void Log (string message)
		{
			Console.Error.WriteLine ($"{ME}: {message}");
			if (Output != null)
				Output.WriteLine ($"{ME}: {message}");
		}

		public static void LogError (string message)
		{
			Console.Error.WriteLine ($"{ME} ERROR: {message}");
			if (Output != null) {
				Output.WriteLine ($"{ME} ERROR: {message}");
				Output.Flush ();
				Output.Dispose ();
			}
			if (HtmlOutput != null) {
				HtmlOutput.WriteLine ($"<p><b>Auto provisioning failed!</b></p>");
				HtmlOutput.WriteLine ($"<pre>{message}</pre>");
			}
			Finish ();
			Environment.Exit (1);
		}

		static void Finish ()
		{
			if (HtmlOutput != null) {
				if (OutputFile != null) {
					HtmlOutput.WriteLine ($"<p></p>");
					HtmlOutput.WriteLine ($"<p>Provision output: <a href=\"{GetOutputLink ()}\">{Path.GetFileName (OutputFile)}</a></p>");
				}
				HtmlOutput.WriteLine ($"<p></p>");
				HtmlOutput.Flush ();
				HtmlOutput.Dispose ();
			}

			string GetOutputLink ()
			{
				var prefix = "artifact";
				if (JenkinsJobPath != null)
					prefix = Path.Combine (JenkinsJobPath, prefix);
				return Path.Combine (prefix, Path.GetFileName (OutputFile));
			}
		}
	}
}
