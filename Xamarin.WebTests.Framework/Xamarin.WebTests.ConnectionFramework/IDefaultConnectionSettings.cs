using System;
using System.Net;
using Xamarin.AsyncTests;

namespace Xamarin.WebTests.ConnectionFramework
{
	public interface IDefaultConnectionSettings : ITestDefaults
	{
		bool InstallDefaultCertificateValidator {
			get;
		}

		ISslStreamProvider DefaultSslStreamProvider {
			get;
		}

		SecurityProtocolType? SecurityProtocol {
			get;
		}

		Guid? InstallTlsProvider {
			get;
		}
	}
}

