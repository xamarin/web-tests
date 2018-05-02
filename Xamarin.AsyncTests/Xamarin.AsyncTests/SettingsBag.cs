﻿//
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
using System.Diagnostics;

namespace Xamarin.AsyncTests
{
	public abstract class SettingsBag : INotifyPropertyChanged
	{
		public const int DisableTimeoutsAtLogLevel = 5;

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
				if (TryGetValue ("LogLevel", out string value))
					return int.Parse (value);
				return 0;
			}
			set {
				SetValue ("LogLevel", value.ToString ());
				OnPropertyChanged ("LogLevel");
			}
		}

		public int? GetLogLevel (string category)
		{
			if (TryGetValue ($"LogLevel_{category}", out string value))
				return int.Parse (value);
			return null;
		}

		public void SetLogLevel (string category, int value)
		{
			var name = $"LogLevel_{category}";
			SetValue (name, value.ToString ());
			OnPropertyChanged (name);
		}

		public int LocalLogLevel {
			get {
				string value;
				if (TryGetValue ("LocalLogLevel", out value))
					return int.Parse (value);
				return 0;
			}
			set {
				SetValue ("LocalLogLevel", value.ToString ());
				OnPropertyChanged ("LocalLogLevel");
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

		public bool DisableTimeouts {
			get {
				string value;
				if (TryGetValue ("DisableTimeouts", out value))
					return bool.Parse (value);
				if (LogLevel > DisableTimeoutsAtLogLevel)
					return true;
				return Debugger.IsAttached;
			}
			set {
				SetValue ("DisableTimeouts", value.ToString ());
				OnPropertyChanged ("DisableTimeouts");
			}
		}

		public bool DontSaveLogging {
			get {
				if (TryGetValue ("DontSaveLogging", out string value))
					return bool.Parse (value);
				return false;
			}
			set {
				SetValue ("DontSaveLogging", value.ToString ());
				OnPropertyChanged ("DontSaveLogging");
			}
		}

		public bool Debug_DumpTestPath {
			get {
				string value;
				if (TryGetValue ("Debug_DumpTestPath", out value))
					return bool.Parse (value);
				return true;
			}
			set {
				SetValue ("Debug_DumpTestPath", value.ToString ());
				OnPropertyChanged ("Debug_DumpTestPath");
			}
		}

		public string MartinTest {
			get {
				if (TryGetValue ("MartinTest", out string value))
					return value;
				return null;
			}
			set {
				SetValue ("MartinTest", value);
				OnPropertyChanged ("MartinTest");
			}
		}

		public string SelectTest {
			get {
				if (TryGetValue ("SelectTest", out string value))
					return value;
				return null;
			}
			set {
				SetValue ("SelectTest", value);
				OnPropertyChanged ("SelectTest");
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

