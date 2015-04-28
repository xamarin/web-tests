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
		new public IClientParameters Parameters {
			get { return (IClientParameters)base.Parameters; }
		}

		public DotNetClient (IPEndPoint endpoint, ISslStreamProvider provider, IClientParameters parameters)
			: base (endpoint, provider, parameters.ConnectionParameters)
		{
		}

		protected override async Task<Stream> Start (TestContext ctx, Socket socket, CancellationToken cancellationToken)
		{
			ctx.LogDebug (1, "Connected.");

			List<IClientCertificate> clientCertificates = null;
			if (Parameters.ClientCertificate != null) {
				clientCertificates = new List<IClientCertificate> ();
				clientCertificates.Add (Parameters.ClientCertificate);
			}

			var targetHost = "Hamiller-Tube.local";

			var stream = new NetworkStream (socket);
			var server = await SslStreamProvider.CreateClientStreamAsync (
				stream, targetHost, clientCertificates, null, SslStreamFlags.None, cancellationToken);

			ctx.LogDebug (1, "Successfully authenticated.");

			return server;
		}
	}
}

