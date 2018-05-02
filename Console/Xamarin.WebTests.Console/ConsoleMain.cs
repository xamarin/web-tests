using System;
using Xamarin.AsyncTests;
using Xamarin.AsyncTests.Console;
using Xamarin.AsyncTests.Remoting;
using Mono.Btls.Tests;
using Xamarin.WebTests;
using Xamarin.WebTests.MonoTests;
using Xamarin.WebTests.MonoTestFramework;
using Xamarin.WebTests.ConnectionFramework;
using Xamarin.WebTests.MonoConnectionFramework;
using Xamarin.WebTests.TestProvider;
using Xamarin.WebTests.MonoTestProvider;

[assembly: AsyncTestSuite (typeof (WebTestFeatures), true)]
[assembly: AsyncTestSuite (typeof (MonoWebTestFeatures), true)]
[assembly: AsyncTestSuite (typeof (BoringTlsTestFeatures), true)]

namespace Xamarin.WebTests.Console
{
	[Serializable]
	public class ConsoleMain : IForkedDomainSetup
	{
		static void Main (string[] args)
		{
			var main = new ConsoleMain ();
			main.Run (args);
		}

		void Run (string[] args)
		{
			DependencyInjector.RegisterDependency<IForkedProcessLauncher> (() => new ForkedProcessLauncher ());
			DependencyInjector.RegisterDependency<IForkedDomainSetup> (() => this);

			Initialize ();

			Program.Run (typeof (ConsoleMain).Assembly, args);
		}

		public void Initialize ()
		{
			var setup = new MonoConnectionFrameworkSetup ("Xamarin.WebTests.Console");
			DependencyInjector.RegisterDependency<IConnectionFrameworkSetup> (() => setup);
			DependencyInjector.RegisterDependency<IMonoConnectionFrameworkSetup> (() => setup);

			DependencyInjector.RegisterAssembly (typeof(ConsoleMain).Assembly);
			DependencyInjector.RegisterAssembly (typeof(WebDependencyProvider).Assembly);
		}
	}
}

