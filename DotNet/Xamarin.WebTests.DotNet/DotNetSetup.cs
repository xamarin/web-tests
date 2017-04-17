using System;
using System.Net;
using System.Reflection;
using Xamarin.WebTests.ConnectionFramework;

namespace Xamarin.WebTests.DotNet
{
	class DotNetSetup : IConnectionFrameworkSetup
	{
		public string Name => "DotNet";

		public bool InstallDefaultCertificateValidator => true;

		public bool SupportsTls12 => true;

		public void Initialize (ConnectionProviderFactory factory)
		{
			;
		}
	}
}
