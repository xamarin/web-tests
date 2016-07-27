using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Reflection;
using System.Net;
using System.Net.Sockets;
using System.Net.Security;
using System.Diagnostics;
using System.Collections.Generic;

using MSI = Mono.Security.Interface;

using SSCX = System.Security.Cryptography.X509Certificates;

using Xamarin.AsyncTests;
using Xamarin.AsyncTests.Portable;
using Xamarin.WebTests.Server;
using Xamarin.WebTests.ConnectionFramework;

namespace Xamarin.WebTests.MonoConnectionFramework
{
	using MonoTestFramework;

	class MonoServer : MonoConnection, IMonoServer
	{
		public MonoConnectionParameters MonoParameters {
			get { return base.Parameters as MonoConnectionParameters; }
		}

		public MonoServer (MonoConnectionProvider provider, ConnectionParameters parameters, IMonoConnectionExtensions extensions)
			: base (provider, parameters, extensions)
		{
		}

		protected override bool IsServer {
			get { return true; }
		}

		void SetClientIssuers (MSI.MonoTlsSettings settings, string[] issuers)
		{
			var type = typeof (MSI.MonoTlsSettings).GetTypeInfo ();
			var prop = type.GetDeclaredProperty ("ClientCertificateIssuers");
			if (prop == null)
				throw new NotSupportedException ("MonoTlsSettings.ClientCertificateIssuers is not available!");
			prop.SetValue (settings, issuers);
		}

		protected override void GetSettings (TestContext ctx, MSI.MonoTlsSettings settings)
		{
			#if FIXME
			if (Parameters.RequireClientCertificate)
				settings.RequireClientCertificate = settings.AskForClientCertificate = true;
			else if (Parameters.AskForClientCertificate)
				settings.AskForClientCertificate = true;
			#endif

			if (MonoParameters != null && MonoParameters.ServerCiphers != null)
				settings.EnabledCiphers = MonoParameters.ServerCiphers.ToArray ();

			if (MonoParameters != null && MonoParameters.ClientCertificateIssuers != null)
				SetClientIssuers (settings, MonoParameters.ClientCertificateIssuers);

			if (MonoParameters != null) {
				#if FIXME
				settings.RequestCipherSuites = MonoParameters.ServerCiphers;
				settings.NamedCurve = MonoParameters.ServerNamedCurve;
				#endif
			}

			base.GetSettings (ctx, settings);
		}

		protected override async Task<MonoSslStream> Start (TestContext ctx, Stream stream, MSI.MonoTlsSettings settings, CancellationToken cancellationToken)
		{
			var server = await ConnectionProvider.CreateServerStreamAsync (stream, Parameters, settings, cancellationToken);

			ctx.LogMessage ("Successfully authenticated server.");

			return server;
		}
	}
}
