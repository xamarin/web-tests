using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Xamarin.AsyncTests;

namespace Xamarin.WebTests.ConnectionFramework
{
	public class ClientAndServer : Connection
	{
		IServer server;
		IClient client;

		public IServer Server {
			get { return server; }
		}

		public IClient Client {
			get { return client; }
		}

		public override bool SupportsCleanShutdown {
			get { return server.SupportsCleanShutdown && client.SupportsCleanShutdown; }
		}

		public ClientAndServer (IServer server, IClient client)
			: base (server.EndPoint, ClientAndServerParameters.Create (client.Parameters, server.Parameters))
		{
			this.server = server;
			this.client = client;
		}

		public override async Task Start (TestContext ctx, CancellationToken cancellationToken)
		{
			await server.Start (ctx, cancellationToken);
			await client.Start (ctx, cancellationToken);
		}

		protected virtual Task WaitForServerConnection (TestContext ctx, CancellationToken cancellationToken)
		{
			return server.WaitForConnection (ctx, cancellationToken);
		}

		protected virtual Task WaitForClientConnection (TestContext ctx, CancellationToken cancellationToken)
		{
			return client.WaitForConnection (ctx, cancellationToken);
		}

		public override async Task WaitForConnection (TestContext ctx, CancellationToken cancellationToken)
		{
			var serverTask = WaitForServerConnection (ctx, cancellationToken);
			var clientTask = WaitForClientConnection (ctx, cancellationToken);

			var t1 = clientTask.ContinueWith (t => {
				if (t.IsFaulted || t.IsCanceled)
					server.Dispose ();
			});
			var t2 = serverTask.ContinueWith (t => {
				if (t.IsFaulted || t.IsCanceled)
					client.Dispose ();
			});

			await Task.WhenAll (serverTask, clientTask, t1, t2);
		}

		protected override void Stop ()
		{
			client.Dispose ();
			server.Dispose ();
		}

		public override async Task<bool> Shutdown (TestContext ctx, bool attemptCleanShutdown, bool waitForReply, CancellationToken cancellationToken)
		{
			var clientShutdown = client.Shutdown (ctx, attemptCleanShutdown, waitForReply, cancellationToken);
			var serverShutdown = server.Shutdown (ctx, attemptCleanShutdown, waitForReply, cancellationToken);
			await Task.WhenAll (clientShutdown, serverShutdown);
			return clientShutdown.Result && serverShutdown.Result;
		}
	}
}

