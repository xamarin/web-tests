using Xamarin.AsyncTests;
using Xamarin.AsyncTests.Portable;
using Xamarin.WebTests.Portable;
using Xamarin.WebTests.Providers;

namespace Xamarin.WebTests.ConnectionFramework
{
	public class ConnectionParameters : ITestParameter, ICloneable
	{
		public string Identifier {
			get;
			private set;
		}

		string ITestParameter.Value {
			get { return Identifier; }
		}

		public ConnectionParameters (string identifier, IServerCertificate serverCertificate)
		{
			Identifier = identifier;
			ServerCertificate = serverCertificate;
		}

		protected ConnectionParameters (ConnectionParameters other)
		{
			Identifier = other.Identifier;
			EndPoint = other.EndPoint;
			ListenAddress = other.ListenAddress;
			ProtocolVersion = other.ProtocolVersion;
			UseStreamInstrumentation = other.UseStreamInstrumentation;
			TargetHost = other.TargetHost;
			ClientCertificate = other.ClientCertificate;
			ClientCertificateValidator = other.ClientCertificateValidator;
			ClientCertificateSelector = other.ClientCertificateSelector;
			ServerCertificate = other.ServerCertificate;
			ServerCertificateValidator = other.ServerCertificateValidator;
			AskForClientCertificate = other.AskForClientCertificate;
			RequireClientCertificate = other.RequireClientCertificate;
			EnableDebugging = other.EnableDebugging;
		}

		object ICloneable.Clone ()
		{
			return DeepClone ();
		}

		public virtual ConnectionParameters DeepClone ()
		{
			return new ConnectionParameters (this);
		}

		public IPortableEndPoint EndPoint {
			get; set;
		}

		public IPortableEndPoint ListenAddress {
			get; set;
		}

		public ProtocolVersions? ProtocolVersion {
			get; set;
		}

		public bool UseStreamInstrumentation {
			get; set;
		}

		public string TargetHost {
			get; set;
		}

		public IClientCertificate ClientCertificate {
			get; set;
		}

		public ICertificateValidator ClientCertificateValidator {
			get; set;
		}

		public ICertificateSelector ClientCertificateSelector {
			get; set;
		}

		public IServerCertificate ServerCertificate {
			get; set;
		}

		public ICertificateValidator ServerCertificateValidator {
			get; set;
		}

		public bool AskForClientCertificate {
			get; set;
		}

		public bool RequireClientCertificate {
			get; set;
		}

		public bool EnableDebugging {
			get;
			set;
		}
	}
}

