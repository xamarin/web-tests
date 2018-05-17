﻿﻿using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Net.Security;

using Xamarin.AsyncTests;
using Xamarin.AsyncTests.Constraints;
using Xamarin.AsyncTests.Portable;

namespace Xamarin.WebTests.ConnectionFramework
{
	using TestFramework;

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
			ctx.LogDebug (LogCategories.Listener, 1, "Connected.");

			var targetHost = Parameters.TargetHost ?? PortableEndPoint.HostName ?? PortableEndPoint.Address;
			ctx.LogDebug (LogCategories.Listener, 1, "Using '{0}' as target host.", targetHost);

			var protocol = sslStreamProvider.GetProtocol (Parameters, IsServer);
			var clientCertificates = sslStreamProvider.GetClientCertificates (Parameters);

			SanityCheckParameters ();

			Task task;
			string function;

			switch (Parameters.ClientApiType) {
			case SslStreamApiType.Sync:
				function = "SslStream.AuthenticateAsClient()";
				ctx.LogDebug (LogCategories.Listener, 1, "Calling {0} synchronously.", function);
				task = Task.Run (() => sslStream.AuthenticateAsClient (targetHost, clientCertificates, protocol, false));
				break;
			case SslStreamApiType.BeginEnd:
				function = "SslStream.BeginAuthenticateAsClient()";
				ctx.LogDebug (LogCategories.Listener, 1, "Calling {0}.", function);
				task = Task.Factory.FromAsync (
					(callback, state) => sslStream.BeginAuthenticateAsClient (targetHost, clientCertificates, protocol, false, callback, state),
					(result) => sslStream.EndAuthenticateAsClient (result), null);
				break;
			case SslStreamApiType.AuthenticationOptions:
			case SslStreamApiType.AuthenticationOptionsWithCallbacks:
				function = "SslStream.AuthenticateAsClientAsync(SslClientAuthenticationOptions)";
				ctx.LogDebug (LogCategories.Listener, 1, $"Calling {function} async.");
				task = HandleAuthenticationOptions ();
				break;
			case SslStreamApiType.TaskAsync:
			case SslStreamApiType.Default:
				function = "SslStream.AuthenticateAsClientAsync()";
				ctx.LogDebug (LogCategories.Listener, 1, "Calling {0} async.", function);
				task = sslStream.AuthenticateAsClientAsync (targetHost, clientCertificates, protocol, false);
				break;
			default:
				throw ctx.AssertFail (Parameters.ClientApiType);
			}

			try {
				await task.ConfigureAwait (false);
				ctx.LogDebug (LogCategories.Listener, 1, "{0} completed successfully.", function);
			} catch (Exception ex) {
				if (Parameters.ExpectClientException || Parameters.ExpectServerException)
					ctx.LogDebug (LogCategories.Listener, 1, "{0} failed (expected exception): {1}", function, ex.GetType ().Name);
				else
					ctx.LogDebug (LogCategories.Listener, 1, "{0} failed: {1}.", function, ex);
				throw;
			}

			void SanityCheckParameters ()
			{
				if (Parameters.AllowRenegotiation != null &&
				    Parameters.ClientApiType != SslStreamApiType.AuthenticationOptions && Parameters.ClientApiType != SslStreamApiType.AuthenticationOptionsWithCallbacks)
					throw ctx.AssertFail ($"{nameof (Parameters.AllowRenegotiation)} not supported with {Parameters.ClientApiType}");
			}

			Task HandleAuthenticationOptions ()
			{
				var provider = DependencyInjector.Get<ISslAuthenticationOptionsProvider> ();
				if (!provider.IsSupported)
					throw new NotSupportedException ("SslClientAuthenticationOptions is not supported.");

				var options = provider.CreateClientOptions ();
				options.TargetHost = targetHost;
				if (Parameters.AllowRenegotiation != null)
					options.AllowRenegotiation = Parameters.AllowRenegotiation.Value;

				if (Parameters.ClientApiType == SslStreamApiType.AuthenticationOptionsWithCallbacks) {
					var validator = DotNetSslStreamProvider.GetClientValidationCallback (Parameters);
					options.RemoteCertificateValidationCallback = validator;
				}

				options.ClientCertificates = clientCertificates;
				if (Parameters.ClientCertificateSelector != null)
					options.LocalCertificateSelectionCallback = Parameters.ClientCertificateSelector.SelectionCallback;

				return provider.AuthenticateAsClientAsync (options, sslStream, cancellationToken);
			}
		}
	}
}

