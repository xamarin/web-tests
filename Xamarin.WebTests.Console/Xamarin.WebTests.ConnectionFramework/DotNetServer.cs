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

		new public IServerParameters Parameters {
			get { return (IServerParameters)base.Parameters; }
		}

		public DotNetServer (IPEndPoint endpoint, ISslStreamProvider provider, IServerParameters parameters)
			: base (endpoint, provider, parameters)
		{
		}

		protected override async Task<Stream> Start (TestContext ctx, Socket socket, CancellationToken cancellationToken)
		{
			ctx.LogMessage ("Accepted connection from {0}.", socket.RemoteEndPoint);

			if (Parameters.AskForClientCertificate || Parameters.RequireClientCertificate)
				throw new NotSupportedException ();

			var stream = new NetworkStream (socket);
			var server = await SslStreamProvider.CreateServerStreamAsync (stream, Parameters, cancellationToken);

			ctx.LogMessage ("Successfully authenticated.");

			return server;
		}
	}
}

