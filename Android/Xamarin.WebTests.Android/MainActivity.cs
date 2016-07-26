using System;

using Android.App;
using Android.OS;
using Xamarin.AsyncTests;
using Xamarin.WebTests.TestProvider;
using Xamarin.WebTests.MonoTestFramework;

[assembly: AsyncTestSuite (typeof (Xamarin.WebTests.WebTestFeatures), true)]
[assembly: AsyncTestSuite (typeof (Xamarin.WebTests.MonoTests.MonoWebTestFeatures), true)]

namespace Xamarin.WebTests.Android
{
	using Forms;
	using Forms.Platform.Android;
	using AsyncTests;
	using AsyncTests.Framework;
	using AsyncTests.Portable;
	using AsyncTests.Mobile;

	[Activity (Label = "Xamarin.WebTests.Android", MainLauncher = true)]
	public class MainActivity : FormsApplicationActivity
	{
		public TestFramework Framework {
			get;
			private set;
		}

		protected override void OnCreate (Bundle bundle)
		{
			base.OnCreate (bundle);

			Forms.Init (this, bundle);

			DependencyInjector.RegisterAssembly (typeof (WebDependencyProvider).Assembly);
			DependencyInjector.RegisterAssembly (typeof (MonoTestFrameworkDependencyProvider).Assembly);
			DependencyInjector.RegisterAssembly (typeof (MainActivity).Assembly);

			Framework = TestFramework.GetLocalFramework (typeof (MainActivity).Assembly);

			var mobileTestApp = new MobileTestApp (Framework);
			// mobileTestApp.FinishedEvent += (sender, e) => TerminateWithSuccess ();

			LoadApplication (mobileTestApp);
		}
	}
}

