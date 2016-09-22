//
// MobileFormsTestApp.cs
//
// Author:
//       Martin Baulig <martin.baulig@xamarin.com>
//
// Copyright (c) 2015 Xamarin, Inc.
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using SD = System.Diagnostics;
using Xamarin.AsyncTests;
using Xamarin.AsyncTests.Framework;
using Xamarin.AsyncTests.Remoting;
using Xamarin.AsyncTests.Portable;
using Xamarin.Forms;

namespace Xamarin.AsyncTests.Mobile
{
	public class MobileFormsTestApp : Application, ISimpleUIController
	{
		public MobileTestApp App {
			get;
			private set;
		}

		public Label MainLabel {
			get;
			private set;
		}

		public Label StatusLabel {
			get;
			private set;
		}

		public Label StatisticsLabel {
			get;
			private set;
		}

		public StackLayout Content {
			get;
			private set;
		}

		public Button RunButton {
			get;
			private set;
		}

		public Button StopButton {
			get;
			private set;
		}

		public Picker CategoryPicker {
			get;
			private set;
		}

		public MobileFormsTestApp (TestFramework framework, string options)
		{
			App = new MobileTestApp (this, framework, options);

			MainLabel = new Label { HorizontalTextAlignment = TextAlignment.Start, Text = "Welcome to Xamarin AsyncTests!" };

			StatusLabel = new Label { HorizontalTextAlignment = TextAlignment.Start };

			StatisticsLabel = new Label { HorizontalTextAlignment = TextAlignment.Start };

			RunButton = new Button { Text = "Run", IsEnabled = false };

			StopButton = new Button { Text = "Stop", IsEnabled = false };

			var buttonLayout = new StackLayout {
				HorizontalOptions = LayoutOptions.Center, Orientation = StackOrientation.Horizontal,
				Children = { RunButton, StopButton }
			};

			CategoryPicker = new Picker {
				HorizontalOptions = LayoutOptions.FillAndExpand, VerticalOptions = LayoutOptions.Center,
			};
			CategoryPicker.SelectedIndexChanged += (sender, e) => {
				if (CategoryChangedEvent != null)
					CategoryChangedEvent (this, CategoryPicker.SelectedIndex);
			};

			Content = new StackLayout {
				VerticalOptions = LayoutOptions.Center, Orientation = StackOrientation.Vertical,
				Children = { MainLabel, StatusLabel, StatisticsLabel, buttonLayout, CategoryPicker }
			};

			MainPage = new ContentPage { Content = Content };

			RunButton.Clicked += (s, e) => App.Run ();
			StopButton.Clicked += (sender, e) => App.Stop ();
		}

		protected override void OnStart ()
		{
			OnSessionChanged ();
			App.Start ();
		}

		protected override void OnSleep ()
		{
			// Handle when your app sleeps
		}

		protected override void OnResume ()
		{
			// Handle when your app resumes
		}

		void Debug (string format, params object[] args)
		{
			Debug (string.Format (format, args));
		}

		void Debug (string message)
		{
			SD.Debug.WriteLine (message);
		}

		#region ISimpleUIController implementation

		bool isRunning;
		bool canRun;
		bool isRemote;
		IList<string> categories;
		int selectedCategory;

		public bool CanRun {
			get {
				return canRun;
			}
			set {
				Device.BeginInvokeOnMainThread (() => {
					canRun = value;
					RunButton.IsEnabled = value && !isRunning;
				});
			}
		}

		public bool IsRunning {
			get {
				return isRunning;
			}
			set {
				Device.BeginInvokeOnMainThread (() => {
					isRunning = value;
					if (value) {
						RunButton.IsEnabled = false;
						StopButton.IsEnabled = true;
					} else {
						RunButton.IsEnabled = CanRun;
						StopButton.IsEnabled = false;
					}
				});
			}
		}

		public bool IsRemote {
			get {
				return isRemote;
			}
			set {
				Device.BeginInvokeOnMainThread (() => {
					isRemote = value;
					CategoryPicker.IsEnabled = !value;
				});
			}
		}

		public void DebugMessage (string message)
		{
			SD.Debug.WriteLine (message);
		}

		public void Message (string message)
		{
			Device.BeginInvokeOnMainThread (() => MainLabel.Text = message);
		}

		public void Message (string format, params object [] args)
		{
			Message (string.Format (format, args));
		}

		public void StatusMessage (string message)
		{
			Device.BeginInvokeOnMainThread (() => StatusLabel.Text = message);
		}

		public void StatusMessage (string format, params object [] args)
		{
			StatusMessage (string.Format (format, args));
		}

		public void StatisticsMessage (string message)
		{
			Device.BeginInvokeOnMainThread (() => StatisticsLabel.Text = message);
		}

		public void StatisticsMessage (string format, params object [] args)
		{
			StatisticsMessage (string.Format (format, args));
		}

		public IList<string> Categories {
			get {
				return categories;
			}
		}

		public int SelectedCategory {
			get {
				return selectedCategory;
			}
			set {
				Device.BeginInvokeOnMainThread (() => {
					selectedCategory = value;
					CategoryPicker.SelectedIndex = selectedCategory;
				});
			}
		}

		public void SetCategories (IList<string> categories, int selected)
		{
			Device.BeginInvokeOnMainThread (() => {
				this.categories = categories;
				this.selectedCategory = selected;

				CategoryPicker.Items.Clear ();
				foreach (var item in categories)
					CategoryPicker.Items.Add (item);
				CategoryPicker.SelectedIndex = selectedCategory;
			});
		}

		void OnSessionChanged ()
		{
			if (SessionChangedEvent != null)
				SessionChangedEvent (this, EventArgs.Empty);
		}

		public event EventHandler SessionChangedEvent;
		public event EventHandler<int> CategoryChangedEvent;

		#endregion
	}
}

