using System;

using Foundation;
using AppKit;
using Xamarin.AsyncTests;

namespace TestMac
{
	public partial class SettingsDialogController : NSWindowController
	{
		NSMutableArray testCategoriesArray;
		NSMutableArray testFeaturesArray;
		TestCategoryModel currentCategory;

		public static TestConfiguration Configuration {
			get { return AppDelegate.Instance.MacUI.Configuration; }
		}

		public static SettingsBag SettingsBag {
			get { return AppDelegate.Instance.MacUI.Settings; }
		}

		public SettingsDialogController (IntPtr handle) : base (handle)
		{
		}

		[Export ("initWithCoder:")]
		public SettingsDialogController (NSCoder coder) : base (coder)
		{
		}

		public SettingsDialogController () : base ("SettingsDialog")
		{
		}

		TestCategoryModel allCategory;

		public override void AwakeFromNib ()
		{
			base.AwakeFromNib ();

			var app = AppDelegate.Instance;

			testCategoriesArray = new NSMutableArray ();
			allCategory = new TestCategoryModel (TestCategory.All);

			testFeaturesArray = new NSMutableArray ();

			Initialize (app.MacUI.Configuration);
		}

		public void Initialize (TestConfiguration configuration)
		{
			var range = new NSRange (0, CategoriesController.ArrangedObjects ().Length);
			CategoriesController.Remove (NSIndexSet.FromNSRange (range));

			CategoriesController.AddObject (allCategory);
			CurrentCategory = allCategory;

			foreach (var category in configuration.Categories) {
				var model = new TestCategoryModel (category);
				CategoriesController.AddObject (model);
				if (category == Configuration.CurrentCategory)
					CurrentCategory = model;
			}

			foreach (var feature in configuration.Features) {
				var model = new TestFeatureModel (feature);
				FeaturesController.AddObject (model);
			}
		}

		public new SettingsDialog Window {
			get { return (SettingsDialog)base.Window; }
		}

		[Export ("TestCategoriesArray")]
		public NSMutableArray TestCategoriesArray {
			get { return testCategoriesArray; }
			set { testCategoriesArray = value; }
		}

		[Export ("CurrentCategory")]
		public TestCategoryModel CurrentCategory {
			get { return currentCategory; }
			set {
				WillChangeValue ("CurrentCategory");
				currentCategory = value;
				Configuration.CurrentCategory = value.Category;
				DidChangeValue ("CurrentCategory");
			}
		}

		[Export ("TestFeaturesArray")]
		public NSMutableArray TestFeaturesArray {
			get { return testFeaturesArray; }
			set { testFeaturesArray = value; }
		}

		[Export ("RepeatCount")]
		public int RepeatCount {
			get { return SettingsBag.RepeatCount; }
			set {
				WillChangeValue ("RepeatCount");
				SettingsBag.RepeatCount = value;
				DidChangeValue ("RepeatCount");
			}
		}
	}
}
