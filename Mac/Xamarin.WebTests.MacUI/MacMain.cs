using System;
using AppKit;
using Xamarin.AsyncTests;
using Xamarin.AsyncTests.MacUI;
using Xamarin.WebTests;
using Xamarin.WebTests.MonoTests;
using Xamarin.WebTests.MonoTestFramework;
using Xamarin.WebTests.TestProvider;

[assembly: AsyncTestSuite (typeof (WebTestFeatures), true)]
[assembly: AsyncTestSuite (typeof (MonoWebTestFeatures), true)]

namespace Xamarin.WebTests.MacUI
{
	public class MacMain : ISingletonInstance
	{
		static void Main (string[] args)
		{
			NSApplication.Init ();

			DependencyInjector.RegisterAssembly (typeof(MacMain).Assembly);
			DependencyInjector.RegisterAssembly (typeof(WebDependencyProvider).Assembly);
			DependencyInjector.RegisterAssembly (typeof(MonoTestFrameworkDependencyProvider).Assembly);
			DependencyInjector.RegisterDependency<IBuiltinTestServer> (() => new BuiltinTestServer ());

			NSApplication.Main (args);
		}
	}
}

