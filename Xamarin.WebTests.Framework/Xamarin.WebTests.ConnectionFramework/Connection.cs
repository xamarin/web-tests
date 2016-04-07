using System;
using System.IO;
using System.Net;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Xamarin.AsyncTests;
using Xamarin.AsyncTests.Portable;

namespace Xamarin.WebTests.ConnectionFramework
{
	public abstract class Connection : AbstractConnection, IConnection
	{
		public abstract bool SupportsCleanShutdown {
			get;
		}

		public abstract ProtocolVersions SupportedProtocols {
			get;
		}

		protected Connection (IPortableEndPoint endpoint, ConnectionParameters parameters)
			: base (endpoint, parameters)
		{
		}

		protected override Task Initialize (TestContext ctx, CancellationToken cancellationToken)
		{
			return Start (ctx, cancellationToken);
		}

		protected override Task PreRun (TestContext ctx, CancellationToken cancellationToken)
		{
			return FinishedTask;
		}

		protected override Task PostRun (TestContext ctx, CancellationToken cancellationToken)
		{
			return FinishedTask;
		}

		protected override Task Destroy (TestContext ctx, CancellationToken cancellationToken)
		{
			return Task.Run (() => Stop ());
		}

		public abstract Task Start (TestContext ctx, CancellationToken cancellationToken);

		public abstract Task WaitForConnection (TestContext ctx, CancellationToken cancellationToken);

		protected abstract void Stop ();

		public abstract Task<bool> Shutdown (TestContext ctx, CancellationToken cancellationToken);
	}
}

