//
// SettingsBag.cs
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
using System.Linq;
using System.ComponentModel;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Collections.ObjectModel;

namespace Xamarin.AsyncTests
{
	public abstract class SettingsBag : INotifyPropertyChanged
	{
		public bool Repeat {
			get {
				string value;
				if (TryGetValue ("Repeat", out value))
					return bool.Parse (value);
				return false;
			}
			set {
				SetValue ("Repeat", value.ToString ());
				OnPropertyChanged ("Repeat");
			}
		}

		public int RepeatCount {
			get {
				string value;
				if (TryGetValue ("RepeatCount", out value))
					return int.Parse (value);
				return 0;
			}
			set {
				SetValue ("RepeatCount", value.ToString ());
				OnPropertyChanged ("RepeatCount");
			}
		}

		public int LogLevel {
			get {
				string value;
				if (TryGetValue ("LogLevel", out value))
					return int.Parse (value);
				return 0;
			}
			set {
				SetValue ("LogLevel", value.ToString ());
				OnPropertyChanged ("LogLevel");
			}
		}

		public bool HideIgnoredTests {
			get {
				string value;
				if (TryGetValue ("HideIgnoredTests", out value))
					return bool.Parse (value);
				return false;
			}
			set {
				SetValue ("HideIgnoredTests", value.ToString ());
				OnPropertyChanged ("HideIgnoredTests");
			}
		}

		public bool HideSuccessfulTests {
			get {
				string value;
				if (TryGetValue ("HideSuccessfulTests", out value))
					return bool.Parse (value);
				return false;
			}
			set {
				SetValue ("HideSuccessfulTests", value.ToString ());
				OnPropertyChanged ("HideSuccessfulTests");
			}
		}

		public bool? IsFeatureEnabled (string name)
		{
			var key = "/Feature/" + name;
			string value;
			if (!TryGetValue (key, out value))
				return null;
			return bool.Parse (value);
		}

		public void SetIsFeatureEnabled (string name, bool? enabled)
		{
			var key = "/Feature/" + name;
			if (enabled == null)
				RemoveValue (key);
			else
				SetValue (key, enabled.ToString ());
			OnPropertyChanged ("Feature");
		}

		public string CurrentCategory {
			get {
				string value;
				if (!TryGetValue ("CurrentCategory", out value))
					return null;
				return value;
			}
			set {
				System.Diagnostics.Debug.WriteLine ("CATEGORY: {0}", value);
				SetValue ("CurrentCategory", value);
				OnPropertyChanged ("CurrentCategory");
			}
		}

		protected void OnPropertyChanged (string propertyName)
		{
			if (PropertyChanged != null)
				PropertyChanged (this, new PropertyChangedEventArgs (propertyName));
		}

		public abstract IReadOnlyDictionary<string, string> Settings {
			get;
		}

		public event PropertyChangedEventHandler PropertyChanged;

		public abstract bool TryGetValue (string key, out string value);

		public abstract void Add (string key, string value);

		public abstract void RemoveValue (string key);

		public void SetValue (string key, string value)
		{
			string existing;
			if (TryGetValue (key, out existing)) {
				if (existing.Equals (value))
					return;
			}

			DoSetValue (key, value);
			OnPropertyChanged ("Settings");
		}

		protected abstract void DoSetValue (string key, string value);

		public void Merge (SettingsBag provider)
		{
			foreach (var entry in provider.Settings) {
				DoSetValue (entry.Key, entry.Value);
			}
		}

		public static SettingsBag CreateDefault ()
		{
			return new DefaultBag ();
		}

		class DefaultBag : SettingsBag
		{
			Dictionary<string,string> settings;

			public DefaultBag ()
			{
				settings = new Dictionary<string,string> ();
			}

			public override IReadOnlyDictionary<string,string> Settings {
				get { return settings; }
			}

			#region implemented abstract members of SettingsBag

			public override bool TryGetValue (string key, out string value)
			{
				return settings.TryGetValue (key, out value);
			}

			public override void Add (string key, string value)
			{
				settings.Add (key, value);
				OnPropertyChanged ("Settings");
			}

			public override void RemoveValue (string key)
			{
				settings.Remove (key);
				OnPropertyChanged ("Settings");
			}

			protected override void DoSetValue (string key, string value)
			{
				if (settings.ContainsKey (key))
					settings [key] = value;
				else
					settings.Add (key, value);
			}

			#endregion
		}
	}
}

