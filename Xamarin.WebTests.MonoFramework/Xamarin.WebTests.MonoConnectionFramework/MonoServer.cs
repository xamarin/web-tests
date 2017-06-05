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
using Xamarin.WebTests.ConnectionFramework;

namespace Xamarin.WebTests.MonoConnectionFramework
{
	using MonoTestFramework;

	class MonoServer : DotNetServer, IMonoConnection
	{
		public MonoServer (MonoConnectionProvider provider, ConnectionParameters parameters)
			: base (provider, parameters, provider)
		{
		}

		public bool SupportsConnectionInfo => Provider.SupportsMonoExtensions;

		public MSI.MonoTlsConnectionInfo GetConnectionInfo ()
		{
			var monoSslStream = MSI.MonoTlsProviderFactory.GetMonoSslStream (SslStream);
			return monoSslStream.GetConnectionInfo ();
		}
	}
}
