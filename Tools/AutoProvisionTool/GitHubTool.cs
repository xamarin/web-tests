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
		}

		GitHubClient client;
		Repository repository;

		public async Task Initialize ()
		{
			var product = new ProductHeaderValue ("AutoProvisionTool");
			client = new GitHubClient (product);

			var token = Environment.GetEnvironmentVariable ("JENKINS_OAUTH_TOKEN");
			if (!string.IsNullOrEmpty (token)) {
				Program.Log ($"Using OAUTH token.");
				client.Credentials = new Credentials (token);
			}

			repository = await client.Repository.Get (Owner, Name).ConfigureAwait (false);
		}

		public async Task<CombinedCommitStatus> GetLatestCommit ()
		{
			Program.Log ($"Getting latest commit from {Owner}/{Name}:{Branch}");
			for (int goBack = 0; goBack < 25; goBack++) {
				var commit = goBack == 0 ? Branch : $"{Branch}~{goBack}";
				Program.Log ($"Trying {commit}");
				var combined = await client.Repository.Status.GetCombined (
					repository.Id, commit).ConfigureAwait (false);

				Program.Log ($"Found commit {combined.Sha}: {combined.State}");

				if (combined.State.Value == CommitState.Success)
					return combined;
			}

			Program.LogError ("Failed to found a successful commit.");
			return null;
		}

		public Uri GetPackageFromCommit (CombinedCommitStatus commit, string product)
		{
			Program.Log ($"Getting package from commit {commit.Sha} {commit.State} {commit.TotalCount}");
			var context = $"PKG-{product}";
			var package = commit.Statuses.FirstOrDefault (
				s => string.Equals (s.Context, context, StringComparison.Ordinal));
			if (package == null) {
				Program.LogError ($"Failed to get package from commit {commit.Sha}");
				return null;
			}
			Program.Log ($"Got package url {package.TargetUrl}");
			return new Uri (package.TargetUrl);
		}
	}
}
