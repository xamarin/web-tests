using System;
using System.Net;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Xamarin.AsyncTests.Console;

namespace AutoProvisionTool
{
	public static class InstallTool
	{
		public static async Task DownloadFile (Uri uri, string filename)
		{
			using (var client = new WebClient ()) {
				await client.DownloadFileTaskAsync (
					uri, filename).ConfigureAwait (false);
			}
		}

		public static async Task InstallPackage (Uri uri)
		{
			var package = await DownloadPackage (uri).ConfigureAwait (false);
			var packagePath = Path.GetFullPath (package);

			try {
				await ProcessHelper.RunCommand ("/usr/bin/sudo", $"/usr/sbin/installer -verboseR -target / -pkg {packagePath}", CancellationToken.None);
				Program.Log ($"Installed {package}.");
			} catch (Exception ex) {
				Program.LogError ($"Failed to install package: {ex.Message}\n{ex}");
			}
		}

		public static async Task<string> DownloadPackage (Uri uri)
		{
			var filename = Path.GetFileName (uri.LocalPath);
			if (File.Exists (filename)) {
				Program.Log ($"Package {filename} already exists.");
				return filename;
			}
			Program.Log ($"Trying to download {uri}");
			await DownloadFile (uri, filename).ConfigureAwait (false);
			Program.Log ($"Done downloading {uri}");
			return filename;
		}
	}
}
