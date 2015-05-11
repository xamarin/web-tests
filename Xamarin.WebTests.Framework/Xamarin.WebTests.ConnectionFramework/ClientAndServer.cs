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

		new public ClientAndServerParameters Parameters {
			get { return (ClientAndServerParameters)base.Parameters; }
		}

		public override ProtocolVersions SupportedProtocols {
			get {
				return GetSupportedProtocols () ?? ProtocolVersions.Default;
			}
		}

		public ProtocolVersions? GetSupportedProtocols ()
		{
			if (server.SupportedProtocols != ProtocolVersions.Default) {
				if (client.SupportedProtocols == ProtocolVersions.Default)
					return server.SupportedProtocols;
				return server.SupportedProtocols & client.SupportedProtocols;
			} else if (client.SupportedProtocols != ProtocolVersions.Default) {
				if (server.SupportedProtocols == ProtocolVersions.Default)
					return client.SupportedProtocols;
				return server.SupportedProtocols & client.SupportedProtocols;
			}
			return null;
		}

		public ProtocolVersions? GetRequestedProtocol ()
		{
			var supported = GetSupportedProtocols ();
			var bothVersion = Parameters.ProtocolVersion;
			var serverVersion = Parameters.ServerParameters.ProtocolVersion;
			var clientVersion = Parameters.ServerParameters.ProtocolVersion;

			if (bothVersion != ProtocolVersions.Default) {
				if (supported != null)
					bothVersion &= supported.Value;
				return bothVersion;
			}

			if (serverVersion != ProtocolVersions.Default) {
				if (supported != null)
					serverVersion &= supported.Value;
				if (clientVersion != ProtocolVersions.Default)
					serverVersion &= supported.Value;
				return serverVersion;
			}

			if (clientVersion != ProtocolVersions.Default) {
				if (supported != null)
					clientVersion &= supported.Value;
				if (serverVersion != ProtocolVersions.Default)
					clientVersion &= supported.Value;
				return clientVersion;
			}

			return supported;
		}

		protected ClientAndServer (IServer server, IClient client, ClientAndServerParameters parameters)
			: base (server.EndPoint, parameters)
		{
			this.server = server;
			this.client = client;

			var requested = GetRequestedProtocol ();
			if (requested != null) {
				Parameters.ProtocolVersion = requested.Value;
				Parameters.ServerParameters.ProtocolVersion = requested.Value;
				Parameters.ClientParameters.ProtocolVersion = requested.Value;
			}
		}

		public ClientAndServer (IServer server, IClient client)
			: this (server, client, new ClientAndServerParameters (client.Parameters, server.Parameters))
		{
		}

		public override async Task Start (TestContext ctx, CancellationToken cancellationToken)
		{
			ctx.LogMessage ("Starting client and server: {0} {1}", client, server);
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

