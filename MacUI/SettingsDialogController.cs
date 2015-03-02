using System;

using Foundation;
using AppKit;
using Xamarin.AsyncTests;
using Xamarin.AsyncTests.Framework;

namespace Xamarin.AsyncTests.MacUI
{
	public partial class SettingsDialogController : NSWindowController
	{
		NSMutableArray testCategoriesArray;
		NSMutableArray testFeaturesArray;
		TestCategoryModel currentCategory;

		public static MacUI MacUI {
			get { return AppDelegate.Instance.MacUI; }
		}

		public static SettingsBag SettingsBag {
			get { return AppDelegate.Instance.MacUI.Settings; }
		}

		public TestConfiguration Configuration {
			get;
			private set;
		}

		public SettingsDialogController (IntPtr handle) : base (handle)
		{
			Initialize ();
		}

		[Export ("initWithCoder:")]
		public SettingsDialogController (NSCoder coder) : base (coder)
		{
			Initialize ();
		}

		public SettingsDialogController () : base ("SettingsDialog")
		{
			Initialize ();
		}

		void Initialize ()
		{
			testCategoriesArray = new NSMutableArray ();
			allCategory = new TestCategoryModel (TestCategory.All);
			currentCategory = allCategory;
			testCategoriesArray.Add (allCategory);

			testFeaturesArray = new NSMutableArray ();
		}

		TestCategoryModel allCategory;

		public override void AwakeFromNib ()
		{
			base.AwakeFromNib ();

			var ui = AppDelegate.Instance.MacUI;
			ui.ServerManager.TestSession.PropertyChanged += (sender, e) => OnTestSessionChanged (e);
			OnTestSessionChanged (ui.ServerManager.TestSession.Value);
		}

		void OnTestSessionChanged (TestSession session)
		{
			if (session != null) {
				Configuration = session.Configuration;
				foreach (var category in Configuration.Categories) {
					var model = new TestCategoryModel (category);
					CategoriesController.AddObject (model);
					if (category == Configuration.CurrentCategory)
						CurrentCategory = model;
				}

				foreach (var feature in Configuration.Features) {
					var model = new TestFeatureModel (feature);
					FeaturesController.AddObject (model);
				}
			} else {
				Configuration = null;
				var categoryRange = new NSRange (1, CategoriesController.ArrangedObjects ().Length - 1);
				CategoriesController.Remove (NSIndexSet.FromNSRange (categoryRange));

				var featureRange = new NSRange (0, FeaturesController.ArrangedObjects ().Length);
				FeaturesController.Remove (NSIndexSet.FromNSRange (featureRange));

				CurrentCategory = allCategory;
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
				if (Configuration != null)
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

		public bool IsEnabled (TestFeature feature)
		{
			if (Configuration != null)
				return Configuration.IsEnabled (feature);
			return feature.Constant ?? feature.DefaultValue ?? false;
		}

		public void SetIsEnabled (TestFeature feature, bool value)
		{
			if (Configuration != null)
				Configuration.SetIsEnabled (feature, value);
		}
	}
}
