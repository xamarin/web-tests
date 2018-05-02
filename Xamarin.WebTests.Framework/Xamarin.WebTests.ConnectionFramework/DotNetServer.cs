﻿﻿using System;
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
	using TestFramework;

	public class DotNetServer : DotNetConnection
	{
		public override ConnectionType ConnectionType => ConnectionType.Server;

		readonly ISslStreamProvider sslStreamProvider;

		public DotNetServer (ConnectionProvider provider, ConnectionParameters parameters, ISslStreamProvider sslStreamProvider)
			: base (provider, parameters)
		{
			this.sslStreamProvider = sslStreamProvider;
		}

		protected override bool IsServer {
			get { return true; }
		}

		protected override async Task Start (TestContext ctx, SslStream sslStream, CancellationToken cancellationToken)
		{
			var certificate = Parameters.ServerCertificate;
			var protocol = sslStreamProvider.GetProtocol (Parameters, IsServer);
			var askForCert = Parameters.AskForClientCertificate || Parameters.RequireClientCertificate;

			Task task;
			string function;
			if (HasFlag (SslStreamFlags.SyncAuthenticate)) {
				function = "SslStream.AuthenticateAsServer()";
				ctx.LogDebug (LogCategories.Listener, 1, $"Calling {function} synchronously.");
				task = Task.Run (() => sslStream.AuthenticateAsServer (certificate, askForCert, protocol, false));
			} else if (HasFlag (SslStreamFlags.BeginEndAuthenticate)) {
				function = "SslStream.BeginAuthenticateAsServer()";
				ctx.LogDebug (LogCategories.Listener, 1, $"Calling {function}.");
				task = Task.Factory.FromAsync (
					(callback, state) => sslStream.BeginAuthenticateAsServer (certificate, askForCert, protocol, false, callback, state),
					(result) => sslStream.EndAuthenticateAsServer (result), null);
			} else {
				function = "SslStream.AuthenticateAsServerAsync()";
				ctx.LogDebug (LogCategories.Listener, 1, $"Calling {function} async.");
				task = sslStream.AuthenticateAsServerAsync (certificate, askForCert, protocol, false);
			}

			try {
				await task.ConfigureAwait (false);
				ctx.LogDebug (LogCategories.Listener, 1, $"{function} completed successfully.");
			} catch (Exception ex) {
				if (Parameters.ExpectClientException || Parameters.ExpectServerException)
					ctx.LogDebug (LogCategories.Listener, 1, $"{function} failed (expected exception): {ex.GetType ().Name}");
				else
					ctx.LogDebug (LogCategories.Listener, 1, $"{function} failed: {ex}.");
				throw;
			}
		}
	}
}

