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
using Xamarin.WebTests.Server;

namespace Xamarin.WebTests.ConnectionFramework
{
	using Providers;
	using Portable;

	public class DotNetClient : DotNetConnection, IClient
	{
		new public ClientParameters Parameters {
			get { return (ClientParameters)base.Parameters; }
		}

		public DotNetClient (IPEndPoint endpoint, ISslStreamProvider provider, ClientParameters parameters)
			: base (endpoint, provider, parameters)
		{
		}

		protected override async Task<Stream> Start (TestContext ctx, Socket socket, CancellationToken cancellationToken)
		{
			ctx.LogDebug (1, "Connected.");

			var targetHost = "Hamiller-Tube.local";

			var stream = new NetworkStream (socket);
			var server = await SslStreamProvider.CreateClientStreamAsync (
				stream, targetHost, Parameters, cancellationToken);

			ctx.LogDebug (1, "Successfully authenticated client.");

			return server.AuthenticatedStream;
		}
	}
}

