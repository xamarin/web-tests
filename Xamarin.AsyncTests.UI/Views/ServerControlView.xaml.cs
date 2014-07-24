//
// ServerControlView.xaml.cs
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
using System;
using System.Collections.Generic;
using System.ComponentModel;
using Xamarin.Forms;

namespace Xamarin.AsyncTests.UI
{
	public partial class ServerControlView : ContentView
	{
		public ServerControlView ()
		{
			InitializeComponent ();

			foreach (var item in Enum.GetNames (typeof (ServerManager.StartupActionKind)))
				StartupActionPicker.Items.Add (item);

			StartupActionPicker.SelectedIndexChanged += OnSelectedIndexChanged;
		}

		public ServerManager Model {
			get;
			private set;
		}

		protected override void OnBindingContextChanged ()
		{
			base.OnBindingContextChanged ();

			if (Model != null)
				Model.PropertyChanged -= OnPropertyChanged;

			Model = (ServerManager)BindingContext;
			if (Model != null) {
				Model.PropertyChanged += OnPropertyChanged;
				StartupActionPicker.SelectedIndex = (int)Model.StartupAction;
			}
		}

		void OnPropertyChanged (object sender, PropertyChangedEventArgs e)
		{
			if (Model != null)
				StartupActionPicker.SelectedIndex = (int)Model.StartupAction;
		}

		void OnSelectedIndexChanged (object sender, EventArgs e)
		{
			if (Model != null)
				Model.StartupAction = (ServerManager.StartupActionKind)StartupActionPicker.SelectedIndex;
		}
	}
}

