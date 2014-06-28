using System;

using Android.App;
using Android.Content;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Android.OS;

using Xamarin.Forms.Platform.Android;

namespace Xamarin.AsyncTests.Android
{
	using Framework;
	using Sample;
	using UI;

	[Activity (Label = "Xamarin.AsyncTests.Android", MainLauncher = true)]
	public class MainActivity : AndroidActivity
	{
		protected override void OnCreate (Bundle bundle)
		{
			base.OnCreate (bundle);

			Xamarin.Forms.Forms.Init (this, bundle);

			var test = new TestApp ("Simple Tests");
			test.LoadAssembly (typeof(SimpleTest).Assembly);

			SetPage (test.Root);
		}
	}
}

