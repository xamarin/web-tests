using System;
using SD = System.Diagnostics;

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

			var options = Intent.GetStringExtra ("XAMARIN_ASYNCTESTS_OPTIONS");
			options = "--category=Global";

			Forms.Init (this, bundle);

			DependencyInjector.RegisterAssembly (typeof (WebDependencyProvider).Assembly);
			DependencyInjector.RegisterAssembly (typeof (MonoTestFrameworkDependencyProvider).Assembly);
			DependencyInjector.RegisterAssembly (typeof (MainActivity).Assembly);
			DependencyInjector.RegisterDependency<IMonoFrameworkSetup> (() => new DroidFrameworkSetup ());

			Framework = TestFramework.GetLocalFramework (typeof (MainActivity).Assembly);

			var mobileTestApp = new MobileFormsTestApp (Framework, options);
			// mobileTestApp.App.FinishedEvent += (sender, e) => TerminateWithSuccess ();

			LoadApplication (mobileTestApp);
		}
	}
}

