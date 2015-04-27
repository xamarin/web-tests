namespace Xamarin.WebTests.ConnectionFramework
{
	public interface IClientAndServerParameters : ICommonConnectionParameters
	{
		IClientParameters ClientParameters {
			get;
		}

		IServerParameters ServerParameters {
			get;
		}
	}
}

