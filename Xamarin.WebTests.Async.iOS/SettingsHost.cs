//
// SettingsHost.cs
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
using MonoTouch.Foundation;
using Xamarin.AsyncTests;
using Xamarin.AsyncTests.Framework;
using Xamarin.AsyncTests.UI;

namespace Xamarin.WebTests.Async.iOS
{
	public class SettingsHost : SettingsBag, ISettingsHost
	{
		const string DictionaryKey = "AppSettings";

		NSDictionary GetDictionary ()
		{
			var defaults = NSUserDefaults.StandardUserDefaults;
			return defaults.DictionaryForKey (DictionaryKey);
		}

		#region ISettingsHost implementation
		public string GetValue (string name)
		{
			string value;
			if (!TryGetValue (name, out value))
				return null;
			return value;
		}

		public override bool TryGetValue (string name, out string value)
		{
			var dict = GetDictionary ();
			if (dict != null) {
				NSObject obj;
				if (dict.TryGetValue ((NSString)name, out obj)) {
					value = (string)(NSString)obj;
					return true;
				}
			}

			value = null;
			return false;
		}

		public override void Add (string key, string value)
		{
			SetValue (key, value);
		}

		protected override void DoSetValue (string name, string value)
		{
			var defaults = NSUserDefaults.StandardUserDefaults;
			var dict = defaults.DictionaryForKey (DictionaryKey);
			if (dict == null)
				dict = new NSDictionary ();
			var mutable = (NSMutableDictionary)dict.MutableCopy ();
			mutable.Add ((NSString)name, (NSString)value);
			defaults.SetValueForKey (mutable, (NSString)DictionaryKey);
			defaults.Synchronize ();
		}

		public override void RemoveValue (string name)
		{
			var defaults = NSUserDefaults.StandardUserDefaults;
			var dict = defaults.DictionaryForKey (DictionaryKey);
			if (dict == null)
				dict = new NSDictionary ();
			var mutable = (NSMutableDictionary)dict.MutableCopy ();
			mutable.Remove ((NSString)name);
			defaults.SetValueForKey (mutable, (NSString)DictionaryKey);
			defaults.Synchronize ();
			OnPropertyChanged ("Settings");
		}

		public override IReadOnlyDictionary<string, string> Settings {
			get {
				var retval = new Dictionary<string,string> ();
				var dict = GetDictionary ();
				if (dict == null)
					return retval;

				foreach (var entry in dict) {
					var key = (NSString)entry.Key;
					var value = (NSString)entry.Value;
					retval.Add ((string)key, (string)value);
				}

				return retval;
			}
		}

		SettingsBag ISettingsHost.GetSettings ()
		{
			return this;
		}
		#endregion
	}
}

