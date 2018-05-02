using System;
using Xamarin.AsyncTests;
using Xamarin.AsyncTests.Console;
using Xamarin.AsyncTests.Remoting;
using Xamarin.WebTests.TestProvider;
using Xamarin.WebTests.ConnectionFramework;

[assembly: AsyncTestSuite (typeof (Xamarin.WebTests.WebTestFeatures), true)]

namespace Xamarin.WebTests.DotNet
{
	[Serializable]
	public class ConsoleMain : IForkedDomainSetup, IForkedSupport
	{
		public bool SupportsProcessForks => true;

		static void Main (string[] args)
		{
			var main = new ConsoleMain ();
			main.Run (args);
		}

		void Run (string[] args)
		{
			DependencyInjector.RegisterDependency<IForkedProcessLauncher> (() => new ForkedProcessLauncher ());
			DependencyInjector.RegisterDependency<IForkedSupport> (() => this);
			DependencyInjector.RegisterDependency<IForkedDomainSetup> (() => this);

			Initialize ();

			Program.Run (typeof (ConsoleMain).Assembly, args);
		}

		public void Initialize ()
		{
			var setup = new DotNetSetup ();
			DependencyInjector.RegisterDependency<IForkedSupport> (() => this);
			DependencyInjector.RegisterDependency<IConnectionFrameworkSetup> (() => setup);

			DependencyInjector.RegisterAssembly (typeof (ConsoleMain).Assembly);
			DependencyInjector.RegisterAssembly (typeof (WebDependencyProvider).Assembly);
		}
	}

}

