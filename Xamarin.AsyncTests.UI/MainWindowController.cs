using System;
using System.IO;
using System.Xml;
using System.Xml.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Foundation;
using AppKit;

using Xamarin.AsyncTests;
using Xamarin.AsyncTests.Framework;
using Xamarin.AsyncTests.Remoting;

namespace Xamarin.AsyncTests.MacUI
{
	public partial class MainWindowController : NSWindowController
	{
		public MainWindowController (IntPtr handle) : base (handle)
		{
		}

		[Export ("initWithCoder:")]
		public MainWindowController (NSCoder coder) : base (coder)
		{
		}

		public MainWindowController () : base ("MainWindow")
		{
		}

		public override void AwakeFromNib ()
		{
			base.AwakeFromNib ();

			Initialize ();
		}

		void Initialize ()
		{
			var app = MacUI.AppDelegate;

			app.MacUI.TestRunner.Run.NotifyStateChanged.StateChanged += (sender, e) => CanRun = e;
			app.MacUI.TestRunner.Stop.NotifyStateChanged.StateChanged += (sender, e) => CanStop = e;

			UIBinding.Bind (app.MacUI.ServerManager.StatusMessage, ServerStatusMessage);
			UIBinding.Bind (app.MacUI.TestRunner.StatusMessage, ServerStatusMessage);

			SplitView.AddSubview (TestResultList);
			SplitView.AddSubview (TestResultDetails);

			app.Delegate.AddObserver (this, (NSString)app.CurrentSessionKey, NSKeyValueObservingOptions.New, IntPtr.Zero);
		}

		public override void ObserveValue (NSString keyPath, NSObject ofObject, NSDictionary change, IntPtr context)
		{
			var app = MacUI.AppDelegate;
			if (string.Equals (keyPath, app.CurrentSessionKey)) {
				OnTestSuiteLoaded (app.CurrentSession);
			} else {
				// would otherwise crash in native code.
				throw new InvalidOperationException ();
			}
		}

		void OnTestSuiteLoaded (TestSessionModel session)
		{
			if (session != null) {
				TestResultController.AddObject (session);
			} else {
				var array = (NSArray)TestResultController.Content;
				var length = (int)array.Count;
				for (int i = 0; i < length; i++) {
					var index = NSIndexPath.FromIndex (0);
					TestResultController.RemoveObjectAtArrangedObjectIndexPath (index);
				}
			}
		}

		public new MainWindow Window {
			get { return (MainWindow)base.Window; }
		}

		[Export ("Run:node")]
		public async void Run (TestListNode node)
		{
			var ui = MacUI.Instance;

			var parameters = new RunParameters (node.TestCase.Session, node.TestCase.Test);
			var result = await ui.TestRunner.Run.Execute (parameters);

			var model = new TestResultModel (node.TestCase.Session, result);
			node.AddChild (model);
		}

		[Export ("Clear:node")]
		public void Clear (TestListNode node)
		{
			node.RemoveAllChildren ();
		}

		public void LoadSession (string filename)
		{
			var doc = XDocument.Load (filename);
			var result = TestSerializer.ReadTestResult (doc.Root);
			var currentSession = MacUI.AppDelegate.CurrentSession;
			var session = currentSession != null ? currentSession.Session : null;
			var model = new TestResultModel (session, result);
			TestResultController.AddObject (model);
		}

		public void SaveSession (string filename)
		{
			// Get root node from current selection
			var indexPath = TestResultController.SelectionIndexPath;
			if (indexPath == null)
				return;

			var index = indexPath.IndexAtPosition (0);

			var internalArray = (NSArray)TestResultController.Content;
			var rootNode = internalArray.GetItem<TestListNode> (index);

			var model = rootNode as TestResultModel;
			if (model == null)
				return;

			var result = model.Result;
			var element = TestSerializer.WriteTestResult (result);

			var settings = new XmlWriterSettings { Indent = true };
			using (var writer = XmlTextWriter.Create (filename, settings)) {
				element.WriteTo (writer);
			}
		}

		bool canRun;
		bool canStop;

		[Export ("CanRun")]
		public bool CanRun {
			get { return canRun; }
			set {
				WillChangeValue ("CanRun");
				canRun = value;
				DidChangeValue ("CanRun");
			}
		}

		[Export ("CanStop")]
		public bool CanStop {
			get { return canStop; }
			set {
				WillChangeValue ("CanStop");
				canStop = value;
				DidChangeValue ("CanStop");
			}
		}

		[Export ("Stop")]
		public async void Stop ()
		{
			var ui = MacUI.Instance;
			await ui.TestRunner.Stop.Execute ();
		}
	}
}
