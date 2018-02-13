using System;
using System.Threading;
using System.Threading.Tasks;
using Xamarin.AsyncTests.Console;

namespace AutoProvisionTool
{
	class Program
	{
		public static void Main (string[] args)
		{
			if (args.Length < 1) {
				LogError ("Usage error.");
				return;
			}

			switch (args [0]) {
			case "provision-mono":
				if (args.Length != 2) {
					LogError ("Branch name expected.");
					return;
				}
				ProvisionMono (args[1]).Wait (); 
				break;
			case "provision-ios":
				if (args.Length != 2) {
					LogError ("Branch name expected.");
					return;
				}
				ProvisionXI (args[1]).Wait ();
				break;
			case "provision-mac":
				if (args.Length != 2) {
					LogError ("Branch name expected.");
					return;
				}
				ProvisionXM (args[1]).Wait ();
				break;
			case "mono-version":
				PrintMonoVersion ().Wait ();
				break;
			default:
				LogError ($"Invalid command: `{args[0]}'.");
				return;
			}
		}

		public static async Task ProvisionMono (string branch)
		{
			var oldVersion = await GetMonoVersion ().ConfigureAwait (false);
			Log ($"Provisioning Mono from {branch}.");
			var github = new GitHubTool ("mono", "mono", branch);
			await github.Initialize ().ConfigureAwait (false);
			var latest = await github.GetLatestCommit ();
			var package = github.GetPackageFromCommit (latest, "mono");
			Log ($"Got package url {package}.");
			await InstallTool.InstallPackage (package);
			Log ($"Successfully provisioned Mono from {branch}.");
			var newVersion = await GetMonoVersion ().ConfigureAwait (false);
			Log ($"Old Mono version: {oldVersion}");
			Log ($"New Mono version: {newVersion}");
		}

		public static async Task ProvisionXI (string branch)
		{
			Log ($"Provisioning XI from {branch}.");
			var github = new GitHubTool ("xamarin", "xamarin-macios", branch);
			await github.Initialize ().ConfigureAwait (false);
			var latest = await github.GetLatestCommit ();
			var package = github.GetPackageFromCommit (latest, "xamarin.ios");
			Log ($"Got package url {package}.");
			await InstallTool.InstallPackage (package);
			Log ($"Successfully provisioned XI from {branch}.");
		}

		public static async Task ProvisionXM (string branch)
		{
			Log ($"Provisioning XM from {branch}.");
			var github = new GitHubTool ("xamarin", "xamarin-macios", branch);
			await github.Initialize ().ConfigureAwait (false);
			var latest = await github.GetLatestCommit ();
			var package = github.GetPackageFromCommit (latest, "xamarin.mac");
			Log ($"Got package url {package}.");
			await InstallTool.InstallPackage (package);
			Log ($"Successfully provisioned XM from {branch}.");
		}

		public static async Task<string> GetMonoVersion ()
		{
			var output = await ProcessHelper.RunCommandWithOutput (
				"mono", "--version", CancellationToken.None).ConfigureAwait (false);
			return output.Split ('\n')[0];
		}

		public static async Task PrintMonoVersion ()
		{
			var output = await ProcessHelper.RunCommandWithOutput (
				"mono", "--version", CancellationToken.None).ConfigureAwait (false);
			Log (output);
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

		public static void Log (string message)
		{
			Console.Error.WriteLine ($"{ME}: {message}");
		}

		public static void LogError (string message)
		{
			Console.Error.WriteLine ($"{ME} ERROR: {message}");
			Environment.Exit (1);
		}
	}
}
