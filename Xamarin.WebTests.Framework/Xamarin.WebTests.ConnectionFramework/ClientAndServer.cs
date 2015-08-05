using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Xamarin.AsyncTests;
using Xamarin.AsyncTests.Portable;

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

		new public ClientAndServerParameters Parameters {
			get { return (ClientAndServerParameters)base.Parameters; }
		}

		public override ProtocolVersions SupportedProtocols {
			get { return server.SupportedProtocols & client.SupportedProtocols; }
		}

		public ProtocolVersions? GetRequestedProtocol ()
		{
			var supported = SupportedProtocols;
			var bothVersion = Parameters.ProtocolVersion;
			var serverVersion = Parameters.ServerParameters.ProtocolVersion;
			var clientVersion = Parameters.ServerParameters.ProtocolVersion;

			if (bothVersion != null) {
				bothVersion &= supported;
				return bothVersion;
			}

			if (serverVersion != null) {
				serverVersion &= supported;
				if (clientVersion != null)
					serverVersion &= clientVersion;
				return serverVersion;
			}

			if (clientVersion != null) {
					clientVersion &= supported;
				if (serverVersion != null)
					clientVersion &= serverVersion;
				return clientVersion;
			}

			return null;
		}

		public ClientAndServer (IServer server, IClient client, ClientAndServerParameters parameters)
			: base (server.EndPoint, parameters)
		{
			this.server = server;
			this.client = client;

			var requested = GetRequestedProtocol ();
			if (requested != null) {
				if (requested == ProtocolVersions.Unspecified)
					throw new NotSupportedException ("Incompatible protocol versions between client and server.");
				Parameters.ProtocolVersion = requested.Value;
				Parameters.ServerParameters.ProtocolVersion = requested.Value;
				Parameters.ClientParameters.ProtocolVersion = requested.Value;
			}
		}

		public ClientAndServer (IServer server, IClient client)
			: this (server, client, new ClientAndServerParameters (client.Parameters, server.Parameters))
		{
		}

		protected virtual void InitializeConnection (TestContext ctx)
		{
		}

		public override async Task Start (TestContext ctx, CancellationToken cancellationToken)
		{
			ctx.LogMessage ("Starting client and server: {0} {1} {2}", client, server, server.EndPoint);
			InitializeConnection (ctx);
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

		public override async Task<bool> Shutdown (TestContext ctx, CancellationToken cancellationToken)
		{
			var clientShutdown = client.Shutdown (ctx, cancellationToken);
			var serverShutdown = server.Shutdown (ctx, cancellationToken);
			await Task.WhenAll (clientShutdown, serverShutdown);
			return clientShutdown.Result && serverShutdown.Result;
		}
	}
}

