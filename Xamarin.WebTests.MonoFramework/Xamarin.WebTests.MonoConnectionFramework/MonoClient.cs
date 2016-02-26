using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using System.Net.Security;
using System.Diagnostics;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using System.Security.Authentication;

using MSI = Mono.Security.Interface;

using Xamarin.AsyncTests;
using Xamarin.AsyncTests.Portable;
using Xamarin.WebTests.Server;
using Xamarin.WebTests.ConnectionFramework;

namespace Xamarin.WebTests.MonoConnectionFramework
{
	using MonoTestFramework;

	class MonoClient : MonoConnection, IMonoClient
	{
		public MonoConnectionParameters MonoParameters {
			get { return base.Parameters as MonoConnectionParameters; }
		}

		public MonoClient (MonoConnectionProvider provider, ConnectionParameters parameters, IMonoConnectionExtensions extensions)
			: base (provider, parameters, extensions)
		{
		}

		protected override bool IsServer {
			get { return false; }
		}

		protected override void GetSettings (TestContext ctx, MSI.MonoTlsSettings settings)
		{
			if (MonoParameters != null && MonoParameters.ClientCiphers != null)
				settings.EnabledCiphers = MonoParameters.ClientCiphers.ToArray ();

			if (MonoParameters != null) {
				#if FIXME
				settings.RequestCipherSuites = MonoParameters.ClientCiphers;
				settings.NamedCurve = MonoParameters.ClientNamedCurve;
				#endif
			}

			base.GetSettings (ctx, settings);
		}

		protected override async Task<MonoSslStream> Start (TestContext ctx, Stream stream, MSI.MonoTlsSettings settings, CancellationToken cancellationToken)
		{
			ctx.LogMessage ("Connected.");

			var targetHost = Parameters.TargetHost ?? EndPoint.HostName ?? EndPoint.Address;
			ctx.LogDebug (1, "Using '{0}' as target host.", targetHost);

			var client = await ConnectionProvider.CreateClientStreamAsync (stream, targetHost, Parameters, settings, cancellationToken);

			ctx.LogMessage ("Successfully authenticated client.");

			return client;
		}
	}
}
