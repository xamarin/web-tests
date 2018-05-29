using System;
using AppKit;
using Xamarin.AsyncTests;
using Xamarin.AsyncTests.MacUI;
using Xamarin.AsyncTests.Remoting;
using Xamarin.WebTests;
using Xamarin.WebTests.MonoTests;
using Xamarin.WebTests.MonoTestFramework;
using Xamarin.WebTests.ConnectionFramework;
using Xamarin.WebTests.MonoConnectionFramework;
using Xamarin.WebTests.TestProvider;
using Xamarin.WebTests.MonoTestProvider;

[assembly: AsyncTestSuite (typeof (WebTestFeatures), true)]
[assembly: AsyncTestSuite (typeof (MonoWebTestFeatures), true)]

namespace Xamarin.WebTests.MacUI
{
	[Serializable]
	public class MacMain : ISingletonInstance
	{
		public bool SupportsProcessForks => false;

		static void Main (string[] args)
		{
			NSApplication.Init ();

			var main = new MacMain ();
			main.Run (args);
		}

		void Run (string[] args)
		{
			Initialize ();

			NSApplication.Main (args);
		}

		public void Initialize ()
		{
			var setup = new MonoConnectionFrameworkSetup ("Xamarin.WebTests.Mac");
			DependencyInjector.RegisterDependency<IConnectionFrameworkSetup> (() => setup);
			DependencyInjector.RegisterDependency<IMonoConnectionFrameworkSetup> (() => setup);

			DependencyInjector.RegisterAssembly (typeof (MacMain).Assembly);
			DependencyInjector.RegisterAssembly (typeof (WebDependencyProvider).Assembly);
			DependencyInjector.RegisterDependency<IBuiltinTestServer> (() => new BuiltinTestServer ());
		}

	}
}

