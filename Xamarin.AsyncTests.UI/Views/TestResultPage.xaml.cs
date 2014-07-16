//
// TestResultPage.xaml.cs
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
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using Xamarin.Forms;

namespace Xamarin.AsyncTests.UI
{	
	using Framework;

	public partial class TestResultPage : ContentPage
	{
		public TestApp App {
			get;
			private set;
		}

		public TestResult Result {
			get { return Model.Result; }
		}

		public TestResultModel Model {
			get;
			private set;
		}

		public bool CanRun {
			get { return Result.CanRun; }
		}

		public TestResultPage (TestApp app, TestResultModel model)
		{
			App = app;
			Model = model;

			InitializeComponent ();

			BindingContext = model;
		}

		protected override void OnAppearing ()
		{
			App.TestRunner.CurrentTestResult = Model;
			base.OnAppearing ();
		}

		async void OnItemSelected (object sender, SelectedItemChangedEventArgs args)
		{
			var selected = (TestResultModel)args.SelectedItem;
			var page = new TestResultPage (selected.App, selected);
			await Navigation.PushAsync (page);
		}
	}
}

