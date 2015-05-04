using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Xamarin.AsyncTests;

namespace Xamarin.WebTests.ConnectionFramework
{
	public abstract class ClientAndServerHandler : IConnectionHandler
	{
		public ClientAndServer Connection {
			get;
			private set;
		}

		public ClientAndServerHandler (IServer server, IClient client)
		{
			Connection = new ClientAndServer (server, client);
		}

		public ClientAndServerHandler (ClientAndServer connection)
		{
			Connection = connection;
		}

		public bool SupportsCleanShutdown {
			get { return Connection.SupportsCleanShutdown; }
		}

		public Task WaitForConnection (TestContext ctx, CancellationToken cancellationToken)
		{
			cancellationToken.ThrowIfCancellationRequested ();
			return Connection.WaitForConnection (ctx,cancellationToken);
		}

		public async Task Run (TestContext ctx, CancellationToken cancellationToken)
		{
			await WaitForConnection (ctx, cancellationToken);

			cancellationToken.ThrowIfCancellationRequested ();
			var serverWrapper = new StreamWrapper (Connection.Server.Stream);
			var clientWrapper = new StreamWrapper (Connection.Client.Stream);
			await MainLoop (ctx, serverWrapper, clientWrapper, cancellationToken);
		}

		protected abstract Task MainLoop (TestContext ctx, ILineBasedStream serverStream, ILineBasedStream clientStream, CancellationToken cancellationToken);

		public Task<bool> Shutdown (TestContext ctx, bool attemptCleanShutdown, bool waitForReply, CancellationToken cancellationToken)
		{
			return Connection.Shutdown (ctx, attemptCleanShutdown, waitForReply, cancellationToken);
		}

		public void Close ()
		{
			Connection.Dispose ();
		}
	}
}

