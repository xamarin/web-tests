using System;
using SD = System.Diagnostics;

using Android.App;
using Android.OS;
using Xamarin.AsyncTests;
using Xamarin.WebTests;
using Xamarin.WebTests.MonoTests;
using Xamarin.WebTests.TestProvider;
using Xamarin.WebTests.ConnectionFramework;
using Xamarin.WebTests.MonoConnectionFramework;

[assembly: AsyncTestSuite (typeof (WebTestFeatures), true)]
[assembly: AsyncTestSuite (typeof (MonoWebTestFeatures), true)]

namespace Xamarin.WebTests.BtlsAndroid
{
	using Forms;
	using Forms.Platform.Android;
	using AsyncTests;
	using AsyncTests.Framework;
	using AsyncTests.Portable;
	using AsyncTests.Mobile;

	[Activity (Label = "Xamarin.WebTests.BtlsAndroid", Name = "com.xamarin.webtests.btlsandroid.MainActivity", MainLauncher = true)]
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

			var setup = new BtlsDroidFrameworkSetup ();
			DependencyInjector.RegisterDependency<IConnectionFrameworkSetup> (() => setup);
			DependencyInjector.RegisterDependency<IMonoConnectionFrameworkSetup> (() => setup);

			DependencyInjector.RegisterAssembly (typeof (WebDependencyProvider).Assembly);
			DependencyInjector.RegisterAssembly (typeof (MainActivity).Assembly);

			Framework = TestFramework.GetLocalFramework (typeof (MainActivity).Assembly);

			var mobileTestApp = new MobileFormsTestApp (Framework, options);
			// mobileTestApp.App.FinishedEvent += (sender, e) => TerminateWithSuccess ();

			LoadApplication (mobileTestApp);
		}
	}
}

