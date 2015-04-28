using System;
using Xamarin.AsyncTests.Portable;
using Xamarin.WebTests.Portable;

namespace Xamarin.WebTests.ConnectionFramework
{
	public interface IConnectionParameters
	{
		bool VerifyPeerCertificate {
			get; set;
		}

		bool EnableDebugging {
			get; set;
		}

		ICertificate TrustedCA {
			get; set;
		}

		ICertificateValidator CertificateValidator {
			get; set;
		}
	}
}

