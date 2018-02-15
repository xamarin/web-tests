//
// IOSProduct.cs
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
using System.Threading;
using System.Threading.Tasks;
using System.Text.RegularExpressions;

namespace AutoProvisionTool
{
	public class IOSProduct : Product
	{
		public override string Name => "Xamarin.iOS";

		public override string ShortName => "XI";

		public override string FrameworkName => "Xamarin.iOS.framework";

		public IOSProduct (string branch)
			: base (branch)
		{
		}

		public override async Task<string> GetVersion (CancellationToken cancellationToken)
		{
			cancellationToken.ThrowIfCancellationRequested ();
			var mtouch = Path.Combine (GetFrameworkDirectory (), "bin", "mtouch");
			if (!File.Exists (mtouch))
				return null;
			var output = await Program.RunCommandWithOutput (
				mtouch, "--version", cancellationToken).ConfigureAwait (false);
			return output.Split ('\n')[0];
		}

		const string VersionRE = @"mtouch\s+(.*?)\s+\((.*?):\s*(.*?)\)";

		public override string GetShortVersion (
			string versionString, out string branch, out string commit)
		{
			var match = Regex.Match (versionString, VersionRE);
			if (!match.Success || match.Groups.Count != 4) {
				branch = commit = null;
				return null;
			}
			branch = match.Groups[2].Value;
			commit = match.Groups[3].Value;
			return match.Groups[1].Value;
		}

		public override async Task Provision ()
		{
			var github = new GitHubTool ("xamarin", "xamarin-macios", Branch);
			var latest = await github.GetLatestCommit ().ConfigureAwait (false);
			var package = github.GetPackageFromCommit (latest, "xamarin.ios");
			await InstallTool.InstallPackage (package);
		}


	}
}
