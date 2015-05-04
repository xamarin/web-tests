using System.Collections.Generic;
using Xamarin.AsyncTests;
using Xamarin.WebTests.Portable;

namespace Xamarin.WebTests.ConnectionFramework
{
	public class ServerParameters : ConnectionParameters
	{
		bool askForCert;
		bool requireCert;

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
			askForCert = other.askForCert;
			requireCert = other.requireCert;
			ClientAbortsHandshake = other.ClientAbortsHandshake;
			ExpectException = other.ExpectException;
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

		public bool AskForClientCertificate {
			get { return askForCert || requireCert; }
			set { askForCert = value; }
		}

		public bool RequireClientCertificate {
			get { return requireCert; }
			set {
				requireCert = value;
				if (value)
					askForCert = true;
			}
		}

		public bool ClientAbortsHandshake {
			get; set;
		}

		public bool ExpectException {
			get; set;
		}
	}
}

