using System;
using Xamarin.AsyncTests;

namespace Xamarin.WebTests.ConnectionFramework
{
	public interface IConnectionProviderFactoryExtension : IExtensionCollection
	{
		void Initialize (ConnectionProviderFactory factory, IDefaultConnectionSettings settings);
	}
}
