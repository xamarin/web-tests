using System;
using System.Net;
using System.Net.Security;
using System.Threading;
using System.Threading.Tasks;
using Xamarin.WebTests.ConnectionFramework;

namespace Xamarin.WebTests.DotNet
{
	class DotNetSetup : IConnectionFrameworkSetup
	{
		public string Name => "DotNet";

		public bool InstallDefaultCertificateValidator => true;

		public bool SupportsTls12 => true;

		public bool SupportsCleanShutdown => false;

		public bool UsingAppleTls => false;

		public bool UsingBtls => false;

		public bool HasNewWebStack => false;

		public bool UsingDotNet => true;

		public bool SupportsGZip => true;

		public bool HasNewHttpClient => true;

		public int InternalVersion => 0;

		public void Initialize (ConnectionProviderFactory factory)
		{
			;
		}

		public Task ShutdownAsync (SslStream stream)
		{
			return stream.ShutdownAsync ();
		}

		public bool SupportsRenegotiation => false;

		public bool CanRenegotiate (SslStream stream) => false;

		public Task RenegotiateAsync (SslStream stream, CancellationToken cancellationToken) => throw new NotSupportedException ();
	}
}
