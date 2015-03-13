using System;

using Android.App;
using Android.OS;

namespace Xamarin.WebTests.Android
{
	using Forms;
	using Forms.Platform.Android;
	using AsyncTests.Mobile;

	[Activity (Label = "Xamarin.WebTests.Android", MainLauncher = true, Icon = "@drawable/icon")]
	public class MainActivity : FormsApplicationActivity
	{
		protected override void OnCreate (Bundle bundle)
		{
			base.OnCreate (bundle);

			Forms.Init (this, bundle);

			LoadApplication (new App ());
		}
	}
}

