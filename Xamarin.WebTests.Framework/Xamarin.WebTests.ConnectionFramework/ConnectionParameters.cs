using Xamarin.AsyncTests;
using Xamarin.AsyncTests.Portable;
using Xamarin.WebTests.Portable;
using Xamarin.WebTests.Providers;

namespace Xamarin.WebTests.ConnectionFramework
{
	public abstract class ConnectionParameters : IConnectionParameters, ICommonConnectionParameters, ITestParameter, ICloneable
	{
		bool verifyPeerCertificate = true;
		SslStreamFlags sslStreamFlags = SslStreamFlags.None;

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
			ExpectTrustFailure = other.ExpectTrustFailure;
			ExpectException = other.ExpectException;
			CertificateValidator = other.CertificateValidator;
			sslStreamFlags = other.sslStreamFlags;
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

		public bool ExpectTrustFailure {
			get; set;
		}

		public bool ExpectException {
			get; set;
		}

		public ICertificateValidator CertificateValidator {
			get; set;
		}

		public SslStreamFlags SslStreamFlags {
			get { return sslStreamFlags; }
			set { sslStreamFlags = value; }
		}
	}
}

