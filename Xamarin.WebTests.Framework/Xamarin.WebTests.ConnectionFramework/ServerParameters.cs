using System.Collections.Generic;
using Xamarin.AsyncTests;
using Xamarin.WebTests.Portable;

namespace Xamarin.WebTests.ConnectionFramework
{
	public class ServerParameters : ConnectionParameters
	{
		public ServerParameters (string identifier, IServerCertificate certificate)
			: base (identifier)
		{
			ServerCertificate = certificate;
		}

		protected ServerParameters (ServerParameters other)
			: base (other)
		{
			ServerCertificate = other.ServerCertificate;
			ServerCertificateValidator = other.ServerCertificateValidator;
			Flags = other.Flags;
		}

		public override ConnectionParameters DeepClone ()
		{
			return new ServerParameters (this);
		}

		public IServerCertificate ServerCertificate {
			get; set;
		}

		public ICertificateValidator ServerCertificateValidator {
			get; set;
		}

		public ServerFlags Flags {
			get; set;
		}
	}
}

