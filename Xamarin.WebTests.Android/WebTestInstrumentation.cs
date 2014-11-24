using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Xamarin.Android.NUnitLite;
using System.Reflection;

namespace Xamarin.WebTests.Android
{
	[Instrumentation (Name = "xamarin.webtests.android.WebTestInstrumentation")]
	public class WebTestInstrumentation : TestSuiteInstrumentation
	{

		public WebTestInstrumentation (IntPtr handle, JniHandleOwnership transfer) : base (handle, transfer)
		{
		}

		protected override void AddTests ()
		{
			AddTest (Assembly.GetExecutingAssembly ());
		}
	}
}

