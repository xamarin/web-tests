//
// SettingsController.cs
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
using System.Linq;
using MonoTouch.Foundation;
using MonoTouch.UIKit;
using MonoTouch.Dialog;

namespace Xamarin.NetworkUtils.PhoneTest
{
	public class SettingsController : DialogViewController
	{
		public readonly Settings Settings;

		public SettingsController ()
			: base (new RootElement ("Settings"), true)
		{
			Settings = Settings.Instance;

			var section = new Section ();
			Root.Add (section);

			var showListening = new BooleanElement ("Show listening sockets", Settings.ShowListening);
			showListening.ValueChanged += (sender, e) => {
				Settings.ShowListening = showListening.Value;
			};
			section.Add (showListening);

			var showLocal = new BooleanElement ("Show local connections", Settings.ShowLocal);
			showLocal.ValueChanged += (sender, e) => {
				Settings.ShowLocal = showLocal.Value;
			};
			section.Add (showLocal);

			var autoRefresh = new BooleanElement ("Auto refresh", Settings.AutoRefresh);
			autoRefresh.ValueChanged += (sender, e) => {
				Settings.AutoRefresh = autoRefresh.Value;
			};
			section.Add (autoRefresh);

			var filterSection = new Section ();
			Root.Add (filterSection);

			var usePortFilter = new BooleanElement ("Use Port filter", Settings.UsePortFilter);
			usePortFilter.ValueChanged += (sender, e) => {
				Settings.UsePortFilter = usePortFilter.Value;
			};
			filterSection.Add (usePortFilter);

			var portFilter = new EntryElement ("Port filter", "<port>", Settings.PortFilter.ToString ());
			portFilter.Changed += (sender, e) => {
				int port;
				if (!int.TryParse (portFilter.Value, out port))
					portFilter.Value = Settings.PortFilter.ToString ();
				else
					Settings.PortFilter = port;
			};
			filterSection.Add (portFilter);

			var spSection = new Section ();
			Root.Add (spSection);

			var connectionLimit = new EntryElement ("Connection limit", "<number>", Settings.ConnectionLimit.ToString ());
			connectionLimit.Changed += (sender, e) => {
				int value;
				if (!int.TryParse (connectionLimit.Value, out value))
					connectionLimit.Value = Settings.ConnectionLimit.ToString ();
				else
					Settings.ConnectionLimit = value;
			};
			spSection.Add (connectionLimit);

			var spIdle = new EntryElement ("SP idle time", "<idle-time>", Settings.ServicePointIdleTime.ToString ());
			spIdle.Changed += (sender, e) => {
				int value;
				if (!int.TryParse (spIdle.Value, out value))
					spIdle.Value = Settings.ServicePointIdleTime.ToString ();
				else
					Settings.ServicePointIdleTime = value;
			};
			spSection.Add (spIdle);

			Settings.Modified += (sender, e) => InvokeOnMainThread (() => {
				var newPfValue = Settings.PortFilter.ToString ();
				if (!string.Equals (portFilter.Value, newPfValue)) {
					portFilter.Value = newPfValue;
					Root.Reload (portFilter, UITableViewRowAnimation.Automatic);
				}

				if (usePortFilter.Value != Settings.UsePortFilter) {
					usePortFilter.Value = Settings.UsePortFilter;
					Root.Reload (usePortFilter, UITableViewRowAnimation.Automatic);
				}
			});
		}
	}
}

