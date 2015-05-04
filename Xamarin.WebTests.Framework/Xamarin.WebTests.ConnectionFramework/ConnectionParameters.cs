using Xamarin.AsyncTests;
using Xamarin.AsyncTests.Portable;
using Xamarin.WebTests.Portable;
using Xamarin.WebTests.Providers;

namespace Xamarin.WebTests.ConnectionFramework
{
	public abstract class ConnectionParameters : ITestParameter, ICloneable
	{
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
		}

		object ICloneable.Clone ()
		{
			return DeepClone ();
		}

		public abstract ConnectionParameters DeepClone ();

		public IPortableEndPoint EndPoint {
			get; set;
		}
	}
}

