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

	public class DotNetServer : DotNetConnection, IServer
	{
		public IServerCertificate Certificate {
			get { return Parameters.ServerCertificate; }
		}

		readonly ISslStreamProvider sslStreamProvider;

		public DotNetServer (ConnectionProvider provider, ConnectionParameters parameters, ISslStreamProvider sslStreamProvider)
			: base (provider, parameters)
		{
			this.sslStreamProvider = sslStreamProvider;
		}

		protected override bool IsServer {
			get { return true; }
		}

		protected override async Task<ISslStream> Start (TestContext ctx, Stream stream, CancellationToken cancellationToken)
		{
			var server = await sslStreamProvider.CreateServerStreamAsync (stream, Parameters, cancellationToken);

			ctx.LogMessage ("Successfully authenticated server.");

			return server;
		}
	}
}

