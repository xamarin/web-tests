using System;
using Xamarin.AsyncTests;
using Xamarin.AsyncTests.Console;
using Xamarin.WebTests.TestProvider;
using Xamarin.WebTests.ConnectionFramework;

[assembly: AsyncTestSuite (typeof (Xamarin.WebTests.WebTestFeatures), true)]

namespace Xamarin.WebTests.DotNet
{
	public class ConsoleMain
	{
		static void Main (string[] args)
		{
			var setup = new DotNetSetup ();
			DependencyInjector.RegisterDependency<IConnectionFrameworkSetup> (() => setup);

			DependencyInjector.RegisterAssembly (typeof(ConsoleMain).Assembly);
			DependencyInjector.RegisterAssembly (typeof(WebDependencyProvider).Assembly);

			Program.Run (typeof (WebTestFeatures).Assembly, args);
		}
	}
}

