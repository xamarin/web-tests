//
// RootController.cs
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
using System.Net;
using System.Linq;
using System.Collections.Generic;
using MonoTouch.Foundation;
using MonoTouch.UIKit;
using MonoTouch.Dialog;

namespace Xamarin.NetworkUtils.PhoneTest
{
	public class RootController : DialogViewController
	{
		public readonly Settings Settings;
		Dictionary<NetstatEntry,DateTime> displayedEntries;
		Section section;
		NSTimer timer;

		public RootController ()
			: base (new RootElement ("Netstat"))
		{
			Settings = Settings.Instance;

			section = new Section ();
			Root.Add (section);

			displayedEntries = new Dictionary<NetstatEntry,DateTime> ();

			RefreshRequested += (sender, e) => {
				Populate ();
				ReloadComplete ();
			};

			Settings.Modified += (sender, e) => InvokeOnMainThread (() => {
				displayedEntries.Clear ();
				Populate ();
			});

			timer = NSTimer.CreateRepeatingTimer (1.0, () => {
				if (View.Hidden || !Settings.AutoRefresh)
					return;
				Populate ();
			});
			NSRunLoop.Main.AddTimer (timer, NSRunLoopMode.Default);
		}

		struct DisplayedEntry {
			public readonly NetstatEntry Entry;
			public readonly DateTime Created;
			public bool Flag;
		}

		void Populate ()
		{
			section.Clear ();
			var entries = ManagedNetstat.GetTcp ();
			foreach (var entry in entries) {
				if (!Filter (entry))
					continue;
				var text = string.Format ("{0} - {1} - {2}", entry.LocalEndpoint, entry.RemoteEndpoint, entry.State);
				var element = new StyledStringElement (text);
				element.Font = UIFont.SystemFontOfSize (12.0f);
				if (!displayedEntries.ContainsKey (entry)) {
					displayedEntries.Add (entry, DateTime.UtcNow);
					element.BackgroundColor = UIColor.Red;
				} else {
					var age = DateTime.UtcNow - displayedEntries [entry];
					if (age < TimeSpan.FromSeconds (3))
						element.BackgroundColor = UIColor.Yellow;
				}
				section.Add (element);
			}
			var oldEntries = displayedEntries.Keys.ToList ();
			foreach (var old in oldEntries) {
				if (!entries.Contains (old))
					displayedEntries.Remove (old);
			}
		}

		bool IsLocalHost (IPAddress address)
		{
			var bytes = address.GetAddressBytes ();
			if (bytes.Length != 4)
				return false;
			if (bytes [0] != 127)
				return false;
			if (bytes [1] != 0)
				return false;
			if (bytes [2] != 0)
				return false;
			if (bytes [3] != 1)
				return false;
			return true;
		}

		bool Filter (NetstatEntry entry)
		{
			if (!Settings.ShowListening && entry.State == TcpState.Listen)
				return false;
			if (!Settings.ShowLocal) {
				if (IsLocalHost (entry.LocalEndpoint.Address) && IsLocalHost (entry.RemoteEndpoint.Address))
					return false;
			}
			if (Settings.UsePortFilter && entry.RemoteEndpoint.Port != Settings.PortFilter)
				return false;
			return true;
		}

		public override void ViewDidAppear (bool animated)
		{
			Populate ();
			base.ViewDidAppear (animated);
		}
	}
}
