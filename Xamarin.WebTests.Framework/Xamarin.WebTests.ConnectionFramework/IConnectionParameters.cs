using System;
using Xamarin.AsyncTests.Portable;
using Xamarin.WebTests.Portable;

namespace Xamarin.WebTests.ConnectionFramework
{
	using Providers;

	public interface IConnectionParameters
	{
		bool VerifyPeerCertificate {
			get; set;
		}

		bool EnableDebugging {
			get; set;
		}

		SslStreamFlags SslStreamFlags {
			get; set;
		}
	}
}

