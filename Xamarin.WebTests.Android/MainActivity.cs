using System;

using Android.App;
using Android.OS;

namespace Xamarin.WebTests.Android
{
	using Forms;
	using Forms.Platform.Android;
	using AsyncTests;
	using AsyncTests.Portable;
	using AsyncTests.Mobile;

	[Activity (Label = "Xamarin.WebTests.Android", MainLauncher = true)]
	public class MainActivity : FormsApplicationActivity
	{
		protected override void OnCreate (Bundle bundle)
		{
			base.OnCreate (bundle);

			Forms.Init (this, bundle);

			DependencyInjector.RegisterAssembly (typeof(PortableSupportImpl).Assembly);

			LoadApplication (new App ());
		}
	}
}

