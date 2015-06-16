using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using System.Net.Security;
using System.Diagnostics;
using System.Collections.Generic;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;

using Xamarin.AsyncTests;
using Xamarin.AsyncTests.Portable;

namespace Xamarin.WebTests.ConnectionFramework
{
	using Providers;
	using Portable;
	using Server;

	public class DotNetClient : DotNetConnection, IClient
	{
		new public ClientParameters Parameters {
			get { return (ClientParameters)base.Parameters; }
		}

		readonly ISslStreamProvider sslStreamProvider;

		public DotNetClient (ConnectionProvider provider, ClientParameters parameters, ISslStreamProvider sslStreamProvider)
			: base (provider, parameters)
		{
			this.sslStreamProvider = sslStreamProvider;
		}

		protected override bool IsServer {
			get { return false; }
		}

		protected override async Task<ISslStream> Start (TestContext ctx, Socket socket, CancellationToken cancellationToken)
		{
			ctx.LogDebug (1, "Connected.");

			var targetHost = Parameters.TargetHost ?? EndPoint.HostName ?? EndPoint.Address;
			ctx.LogDebug (1, "Using '{0}' as target host.", targetHost);

			var stream = new NetworkStream (socket);
			var server = await sslStreamProvider.CreateClientStreamAsync (
				stream, targetHost, Parameters, cancellationToken);

			ctx.LogDebug (1, "Successfully authenticated client.");

			return server;
		}
	}
}

