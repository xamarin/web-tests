namespace Xamarin.WebTests.ConnectionFramework
{
	public interface IClientAndServerParameters : IConnectionParameters
	{
		IClientParameters ClientParameters {
			get;
		}

		IServerParameters ServerParameters {
			get;
		}
	}
}

