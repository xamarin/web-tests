//
// InstallTool.cs
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

		public static async Task InstallPackage (Package package)
		{
			var uri = package.TargetUri;
			Program.Log ($"Installing package from {uri}.");
			var packageFile = await DownloadPackage (uri).ConfigureAwait (false);
			var packagePath = Path.GetFullPath (packageFile);

			try {
				await ProcessHelper.RunCommand ("/usr/bin/sudo", $"/usr/sbin/installer -verboseR -target / -pkg {packagePath}", CancellationToken.None);
				Program.Log ($"Installed {packageFile}.");
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
