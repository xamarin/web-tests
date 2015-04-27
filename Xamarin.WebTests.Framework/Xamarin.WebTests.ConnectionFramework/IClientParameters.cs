using System.Collections.Generic;
using Xamarin.WebTests.Portable;

namespace Xamarin.WebTests.ConnectionFramework
{
	public interface IClientParameters : ICommonConnectionParameters
	{
		IClientCertificate ClientCertificate {
			get; set;
		}
	}
}

