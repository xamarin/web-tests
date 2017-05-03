using System;
using SD = System.Diagnostics;

using Android.App;
using Android.OS;
using Xamarin.AsyncTests;
using Xamarin.WebTests.TestProvider;
using Xamarin.WebTests.ConnectionFramework;
using Xamarin.WebTests.MonoConnectionFramework;

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
    using Xamarin.WebTests.MonoTestProvider;

    [Activity (Label = "Xamarin.WebTests.Android", Name = "com.xamarin.webtests.android.MainActivity", MainLauncher = true)]
	public class MainActivity : FormsApplicationActivity
	{
		public TestFramework Framework {
			get;
			private set;
		}

		protected override void OnCreate (Bundle bundle)
		{
			base.OnCreate (bundle);

			var optionString = Intent.GetStringExtra ("XAMARIN_ASYNCTESTS_OPTIONS");

			if (string.IsNullOrEmpty (optionString))
				optionString = "--category=Global";

			Forms.Init (this, bundle);

			var setup = new MonoConnectionFrameworkSetup ("Xamarin.WebTests.Android");
			DependencyInjector.RegisterDependency<IConnectionFrameworkSetup> (() => setup);
			DependencyInjector.RegisterDependency<IMonoConnectionFrameworkSetup> (() => setup);

			DependencyInjector.RegisterAssembly (typeof (WebDependencyProvider).Assembly);
			DependencyInjector.RegisterAssembly (typeof (MainActivity).Assembly);

			var options = new MobileTestOptions (optionString);

			Framework = TestFramework.GetLocalFramework (options.PackageName, typeof (MainActivity).Assembly);

			var mobileTestApp = new MobileFormsTestApp (Framework, options);
			// mobileTestApp.App.FinishedEvent += (sender, e) => TerminateWithSuccess ();

			LoadApplication (mobileTestApp);
		}
	}
}

