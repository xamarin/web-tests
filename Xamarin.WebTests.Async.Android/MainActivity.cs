using System;

using Android.App;
using Android.Content;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Android.OS;

using Xamarin.Forms;
using Xamarin.Forms.Platform.Android;

using Xamarin.AsyncTests;
using Xamarin.AsyncTests.UI;
using Xamarin.AsyncTests.Framework;
using Xamarin.WebTests.Async;

namespace Xamarin.WebTests.Async.Android
{
	[Activity (Label = "Xamarin.WebTests.Async.Android", MainLauncher = true)]
	public class MainActivity : AndroidActivity
	{
		protected override void OnCreate (Bundle bundle)
		{
			base.OnCreate (bundle);

			Xamarin.Forms.Forms.Init (this, bundle);

			var test = new TestApp (null, null, "Xamarin Web Tests");
			test.LoadAssembly (typeof(MainActivity).Assembly);

			SetPage (test.Root);
		}
	}
}


