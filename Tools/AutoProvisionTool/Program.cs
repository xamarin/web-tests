using System;
using System.Threading;
using System.Threading.Tasks;

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
			default:
				LogError ($"Invalid command: `{args[0]}'.");
				return;
			}
		}

		public static async Task ProvisionMono (string branch)
		{
			Log ($"Provisioning Mono from {branch}.");
			var github = new GitHubTool ("mono", "mono", branch);
			await github.Initialize ().ConfigureAwait (false);
			var latest = await github.GetLatestCommit ();
			var package = github.GetPackageFromCommit (latest);
			Log ($"Got package url {package}.");
			await InstallTool.InstallPackage (package);
			Log ($"Successfully provisioned Mono from {branch}.");
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
