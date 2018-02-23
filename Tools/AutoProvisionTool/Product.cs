//
// Product.cs
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
using System.Collections.Generic;
using Mono.Unix;

namespace AutoProvisionTool
{
	public abstract class Product
	{
		public abstract string Name {
			get;
		}

		public abstract string ShortName {
			get;
		}

		public abstract string PackageName {
			get;
		}

		public abstract string RepoOwner {
			get;
		}

		public abstract string RepoName {
			get;
		}

		public string Branch {
			get;
		}

		public Product (string branch)
		{
			Branch = branch;
		}

		public static IList<Product> GetDefaultProducts ()
		{
			var list = new List<Product> ();
			list.Add (new MonoProduct ("master"));
			list.Add (new IOSProduct ("master"));
			list.Add (new MacProduct ("master"));
			// list.Add (new AndroidProduct ("master"));
			return list;
		}

		public abstract string FrameworkName {
			get;
		}

		public string GetFrameworkDirectory ()
		{
			return $"/Library/Frameworks/{FrameworkName}/Versions/Current";
		}

		public string GetFrameworkVersion ()
		{
			var frameworkDir = GetFrameworkDirectory ();
			if (!Directory.Exists (frameworkDir))
				return null;
			return new UnixSymbolicLinkInfo (frameworkDir).GetContents ().Name;
		}

		public bool IsFrameworkInstalled => Directory.Exists (GetFrameworkDirectory ());

		public abstract Task<string> GetVersion (CancellationToken cancellationToken);

		public abstract string GetShortVersion (
			string versionString, out string branch, out string commit);

		public async Task<string> PrintVersion (VersionFormat format)
		{
			if (!IsFrameworkInstalled)
				return "not installed";
			var frameworkVersion = GetFrameworkVersion ();
			if (string.IsNullOrEmpty (frameworkVersion))
				return "not installed";

			string version;
			try {
				version = await GetVersion (CancellationToken.None).ConfigureAwait (false);
				if (string.IsNullOrEmpty (version))
					throw new NotSupportedException ();
			} catch {
				if (format == VersionFormat.Summary)
					return "error";
				return $"{frameworkVersion} (<unable to get detailed version info>)";
			}

			string revision, shortWithRevision;
			try {
				string branch, commit;
				var shortVersion = GetShortVersion (version, out branch, out commit);
				if (string.IsNullOrWhiteSpace (shortVersion))
					throw new NotSupportedException ();
				if (string.IsNullOrWhiteSpace (branch) || string.IsNullOrWhiteSpace (commit)) {
					revision = "<unknown>";
					shortWithRevision = shortVersion;
				} else {
					revision = $"{branch}/{commit}";
					shortWithRevision = $"{shortVersion} ({revision})";
				}
			} catch {
				revision = "<unknown>";
				shortWithRevision = "<unknown>";
			}

			switch (format) {
			case VersionFormat.Verbose:
				return $"{frameworkVersion}: {shortWithRevision}";
			case VersionFormat.Normal:
				return shortWithRevision;
			case VersionFormat.Summary:
				return revision;
			default:
				return $"{frameworkVersion}: {shortWithRevision}\n{version}";
			}
		}

		public abstract Task<Package> Provision ();
	}
}
