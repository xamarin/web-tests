//
// MainPage.xaml.cs
//
// Author:
//       Martin Baulig <martin.baulig@xamarin.com>
//
// Copyright (c) 2014 Xamarin Inc. (http://www.xamarin.com)
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
﻿using System;
using System.Linq;
using System.ComponentModel;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using Xamarin.Forms;

namespace Xamarin.AsyncTests.UI
{	
	using Framework;

	public partial class MainPage : ContentPage
	{	
		public TestApp App {
			get;
			private set;
		}

		CancellationTokenSource cancelCts;
		TestResultCollection result;

		public MainPage (TestApp app)
		{
			App = app;

			InitializeComponent ();

			RunButton.Clicked += (sender, e) => Run ();

			StopButton.Clicked += (sender, e) => cancelCts.Cancel ();

			app.AssemblyLoadedEvent += (sender, e) => RunButton.IsEnabled = true;

			result = new TestResultCollection ();
			List.ItemsSource = result.Children;
		}

		async void Run ()
		{
			cancelCts = new CancellationTokenSource ();
			RunButton.IsEnabled = false;
			StopButton.IsEnabled = true;
			Message ("Running ...");
			try {
				result.Clear ();
				var retval = await App.Run (cancelCts.Token);
				DisplayResult (retval);
				Message ("Done.");
			} catch (TaskCanceledException) {
				Message ("Canceled!");
			} catch (OperationCanceledException) {
				Message ("Canceled!");
			} catch (Exception ex) {
				Message ("ERROR: {0}", ex.Message);
			} finally {
				StopButton.IsEnabled = false;
				cancelCts.Dispose ();
				cancelCts = null;
				RunButton.IsEnabled = true;
			}
		}

		void DisplayResult (TestResultCollection collection)
		{
			result.AddChild (collection, true);
		}

		internal void Message (string format, params object[] args)
		{
			Label.Text = string.Format (format, args);
		}
	}
}

