using System;

using Android.App;
using Android.Content;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Android.Preferences;
using Android.OS;

using Xamarin.Forms;
using Xamarin.Forms.Platform.Android;

using Xamarin.AsyncTests;
using Xamarin.AsyncTests.UI;
using Xamarin.AsyncTests.Framework;
using Xamarin.WebTests.Portable;

namespace Xamarin.WebTests.Async.Android
{
	[Activity (Label = "Xamarin.WebTests.Async.Android", MainLauncher = true)]
	public class MainActivity : AndroidActivity
	{
		ISharedPreferences preferences;

		protected override void OnCreate (Bundle bundle)
		{
			base.OnCreate (bundle);

			Xamarin.Forms.Forms.Init (this, bundle);
			PortableSupportImpl.Initialize ();

			preferences = PreferenceManager.GetDefaultSharedPreferences (this);
			var settings = new SettingsHost (preferences);
			var server = new ServerHost ();

			var test = new UITestApp (PortableSupport.Instance, settings, server, typeof(WebTestFeatures).Assembly);

			SetPage (test.Root);
		}

		#region IPortableSupport implementation

		public string GetStackTrace ()
		{
			return System.Environment.StackTrace;
		}

		#endregion
	}
}


