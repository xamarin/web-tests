using System.Collections.Generic;
using Xamarin.WebTests.Portable;

namespace Xamarin.WebTests.ConnectionFramework
{
	public interface IClientParameters : IConnectionParameters
	{
		IClientCertificate ClientCertificate {
			get; set;
		}

		bool ExpectTrustFailure {
			get; set;
		}

		bool ExpectWebException {
			get; set;
		}
	}
}

