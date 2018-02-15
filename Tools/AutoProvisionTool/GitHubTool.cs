//
// GitHubTool.cs
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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Octokit;

namespace AutoProvisionTool
{
	public class GitHubTool
	{
		public string Owner {
			get;
		}

		public string Name {
			get;
		}

		public string Branch {
			get;
		}

		public GitHubTool (string owner, string name, string branch)
		{
			Owner = owner;
			Name = name;
			Branch = branch;

			Initialize ();
		}

		GitHubClient client;

		void Initialize ()
		{
			var product = new ProductHeaderValue ("AutoProvisionTool");
			client = new GitHubClient (product);

			var token = Environment.GetEnvironmentVariable ("JENKINS_OAUTH_TOKEN");
			if (!string.IsNullOrEmpty (token)) {
				Program.Log ($"Using OAUTH token.");
				client.Credentials = new Credentials (token);
			}
		}

		public async Task<T> GetLatestCommit<T> (Func<CombinedCommitStatus,T> filter)
			where T : class
		{
			Program.Log ($"Getting latest commit from {Owner}/{Name}:{Branch}");
			for (int goBack = 0; goBack < 25; goBack++) {
				var commit = goBack == 0 ? Branch : $"{Branch}~{goBack}";
				Program.Log ($"Trying {commit}");
				var combined = await client.Repository.Status.GetCombined (
					Owner, Name, commit).ConfigureAwait (false);

				Program.Log ($"Found commit {combined.Sha}: {combined.State}");

				if (combined.State.Value != CommitState.Success)
					continue;

				var selected = filter (combined);
				if (selected == null)
					Program.Debug ($"Commit {combined.Sha} does not have any packages.");

				return selected;
			}

			Program.LogError ("Failed to found a successful commit.");
			return null;
		}

		public async Task<Uri> GetLatestPackage (string product)
		{
			var status = await GetLatestCommit (Filter).ConfigureAwait (false);
			Program.Log ($"Got package url {status.TargetUrl}");
			return new Uri (status.TargetUrl);

			CommitStatus Filter (CombinedCommitStatus commit)
			{
				Program.Log ($"Getting package from commit {commit.Sha} {commit.State} {commit.TotalCount}");
				var context = $"PKG-{product}";
				var package = commit.Statuses.FirstOrDefault (
					s => string.Equals (s.Context, context, StringComparison.Ordinal));
				if (package == null)
					Program.Log ($"Failed to get package from commit {commit.Sha}");
				return package;
			}
		}
	}
}
