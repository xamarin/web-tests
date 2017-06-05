﻿﻿using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Net;
// using System.Net.Sockets;
using System.Net.Security;
using System.Diagnostics;
using System.Collections.Generic;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;

using Xamarin.AsyncTests;
using Xamarin.AsyncTests.Portable;

namespace Xamarin.WebTests.ConnectionFramework
{
	public class DotNetClient : DotNetConnection
	{
		public override ConnectionType ConnectionType => ConnectionType.Client;

		readonly ISslStreamProvider sslStreamProvider;

		public DotNetClient (ConnectionProvider provider, ConnectionParameters parameters, ISslStreamProvider sslStreamProvider)
			: base (provider, parameters)
		{
			this.sslStreamProvider = sslStreamProvider;
		}

		protected override bool IsServer {
			get { return false; }
		}

		protected override async Task Start (TestContext ctx, SslStream sslStream, CancellationToken cancellationToken)
		{
			ctx.LogDebug (1, "Connected.");

			var targetHost = Parameters.TargetHost ?? PortableEndPoint.HostName ?? PortableEndPoint.Address;
			ctx.LogDebug (1, "Using '{0}' as target host.", targetHost);

			var protocol = sslStreamProvider.GetProtocol (Parameters, IsServer);
			var clientCertificates = sslStreamProvider.GetClientCertificates (Parameters);

			Task task;
			string function;
			if (HasFlag (SslStreamFlags.SyncAuthenticate)) {
				function = "SslStream.AuthenticateAsClient()";
				ctx.LogDebug (1, "Calling {0} synchronously.", function);
				task = Task.Run (() => sslStream.AuthenticateAsClient (targetHost, clientCertificates, protocol, false));
			} else if (HasFlag (SslStreamFlags.BeginEndAuthenticate)) {
				function = "SslStream.BeginAuthenticateAsClient()";
				ctx.LogDebug (1, "Calling {0}.", function);
				task = Task.Factory.FromAsync (
					(callback, state) => sslStream.BeginAuthenticateAsClient (targetHost, clientCertificates, protocol, false, callback, state),
					(result) => sslStream.EndAuthenticateAsClient (result), null);
			} else {
				function = "SslStream.AuthenticateAsClientAsync()";
				ctx.LogDebug (1, "Calling {0} async.", function);
				task = sslStream.AuthenticateAsClientAsync (targetHost, clientCertificates, protocol, false);
			}

			try {
				await task.ConfigureAwait (false);
				ctx.LogDebug (1, "{0} completed successfully.", function);
			} catch (Exception ex) {
				if (Parameters.ExpectClientException || Parameters.ExpectServerException)
					ctx.LogDebug (1, "{0} failed (expected exception): {1}", function, ex.GetType ().Name);
				else
					ctx.LogDebug (1, "{0} failed: {1}.", function, ex);
				throw;
			}
		}
	}
}

