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
using Xamarin.WebTests.Portable;
using Xamarin.WebTests.Server;

namespace Xamarin.WebTests.ConnectionFramework
{
	using Providers;

	public class DotNetServer : DotNetConnection, IServer
	{
		public IServerCertificate Certificate {
			get { return Parameters.ServerCertificate; }
		}

		new public ServerParameters Parameters {
			get { return (ServerParameters)base.Parameters; }
		}

		public DotNetServer (IPEndPoint endpoint, ISslStreamProvider provider, ServerParameters parameters)
			: base (endpoint, provider, parameters)
		{
		}

		protected override async Task<ISslStream> Start (TestContext ctx, Socket socket, CancellationToken cancellationToken)
		{
			ctx.LogMessage ("Accepted connection from {0}.", socket.RemoteEndPoint);

			var stream = new NetworkStream (socket);
			var server = await SslStreamProvider.CreateServerStreamAsync (stream, Parameters, cancellationToken);

			ctx.LogMessage ("Successfully authenticated server.");

			return server;
		}
	}
}

