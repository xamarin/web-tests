using Xamarin.AsyncTests;
using Xamarin.AsyncTests.Portable;
using Xamarin.WebTests.Portable;

namespace Xamarin.WebTests.ConnectionFramework
{
	public abstract class ConnectionParameters : IConnectionParameters, ICommonConnectionParameters, ITestParameter, ICloneable
	{
		bool verifyPeerCertificate = true;

		public string Identifier {
			get;
			private set;
		}

		string ITestParameter.Value {
			get { return Identifier; }
		}

		public ConnectionParameters (string identifier)
		{
			Identifier = identifier;
		}

		protected ConnectionParameters (ConnectionParameters other)
		{
			Identifier = other.Identifier;
			EndPoint = other.EndPoint;
			verifyPeerCertificate = other.verifyPeerCertificate;
			EnableDebugging = other.EnableDebugging;
			TrustedCA = other.TrustedCA;
		}

		object ICloneable.Clone ()
		{
			return DeepClone ();
		}

		public abstract ConnectionParameters DeepClone ();

		IConnectionParameters ICommonConnectionParameters.ConnectionParameters {
			get { return this; }
		}

		public IPortableEndPoint EndPoint {
			get; set;
		}

		public bool VerifyPeerCertificate {
			get { return verifyPeerCertificate; }
			set { verifyPeerCertificate = value; }
		}

		public bool EnableDebugging {
			get; set;
		}

		public ICertificate TrustedCA {
			get; set;
		}

	}
}

