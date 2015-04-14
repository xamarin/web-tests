using System;
using Xamarin.WebTests.Portable;

namespace Xamarin.WebTests.Resources
{
	class SelfSignedServerCertificate : IServerCertificate
	{
		public byte[] Data {
			get;
			private set;
		}

		public string Password {
			get;
			private set;
		}

		internal SelfSignedServerCertificate ()
		{
			Data = ResourceManager.ReadResource ("CA.server-self.pfx");
			Password = "monkey";
		}
	}
}

