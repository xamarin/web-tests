//
// UISettings.cs
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
using System.Collections.Generic;
using AppKit;
using Foundation;
using Xamarin.AsyncTests;

namespace Xamarin.AsyncTests.MacUI
{
	public class UISettings : SettingsBag
	{
		NSUserDefaults defaults;
		const string Prefix = "xamarin.asynctests.";

		public UISettings ()
		{
			defaults = NSUserDefaults.StandardUserDefaults;
		}

		#region implemented abstract members of SettingsBag

		public override bool TryGetValue (string key, out string value)
		{
			value = defaults.StringForKey (Prefix + key);
			return value != null;
		}

		public override void Add (string key, string value)
		{
			defaults.SetString (value, Prefix + key);
			defaults.Synchronize ();
		}

		public override void RemoveValue (string key)
		{
			defaults.RemoveObject (Prefix + key);
			defaults.Synchronize ();
		}

		protected override void DoSetValue (string key, string value)
		{
			defaults.SetString (value ?? string.Empty, Prefix + key);
			defaults.Synchronize ();
		}

		public override IReadOnlyDictionary<string, string> Settings {
			get {
				var dict = new Dictionary<string, string> ();
				foreach (var entry in defaults.ToDictionary ()) {
					var key = (string)(NSString)entry.Key;
					if (!key.StartsWith (Prefix))
						continue;
					var value = (NSString)entry.Value;
					dict.Add (key.Substring (Prefix.Length), value);
				}
				return dict;
			}
		}

		#endregion
	}
}

