using System;

using Foundation;
using AppKit;
using Xamarin.AsyncTests;

namespace TestMac
{
	public partial class SettingsDialogController : NSWindowController
	{
		NSMutableArray testCategoriesArray;
		TestCategoryModel currentCategory;

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
				DidChangeValue ("CurrentCategory");
			}
		}
	}
}
