//
// MobileTestApp.cs
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
using Xamarin.AsyncTests.Portable;

namespace Xamarin.AsyncTests.Mobile {
	public class MobileTestOptions {

		public SettingsBag Settings {
			get;
		}

		public MobileSessionMode SessionMode {
			get;
		}

		public IPortableEndPoint EndPoint {
			get;
		}

		public string Category {
			get;
		}

		public string Features {
			get;
		}

		public string PackageName {
			get;
		}

		public MobileTestOptions (string options)
		{
			Settings = SettingsBag.CreateDefault ();
			Settings.LocalLogLevel = -1;

			if (string.IsNullOrEmpty (options)) {
				Settings.LogLevel = 0;
				Settings.LocalLogLevel = 0;
				Settings.DisableTimeouts = false;
				SessionMode = MobileSessionMode.Local;
				return;
			}

			int? logLevel = null;
			bool debugMode = false;
			string category = null;
			string features = null;
			string packageName = null;
			string customSettings = null;

			var p = new NDesk.Options.OptionSet ();
			p.Add ("debug", v => debugMode = true);
			p.Add ("log-level=", v => logLevel = int.Parse (v));
			p.Add ("category=", v => category = v);
			p.Add ("features=", v => features = v);
			p.Add ("package-name=", v => packageName = v);
			p.Add ("set=", v => customSettings = v);

			Category = category;
			Features = features;
			PackageName = packageName;

			var optArray = options.Split (new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
			var args = p.Parse (optArray);

			if (debugMode) {
				Settings.LogLevel = -1;
				Settings.LocalLogLevel = -1;
				Settings.DisableTimeouts = true;
			} else {
				Settings.DisableTimeouts = false;
			}

			if (logLevel != null)
				Settings.LogLevel = logLevel.Value;

			if (customSettings != null)
				ParseSettings (customSettings);

			if (args.Count == 0) {
				SessionMode = MobileSessionMode.Local;
				return;
			}

			if (args[0] == "server")
				SessionMode = MobileSessionMode.Server;
			else if (args[0] == "connect") {
				SessionMode = MobileSessionMode.Connect;
			} else if (args[0] == "local") {
				SessionMode = MobileSessionMode.Local;
				if (args.Count != 1)
					throw new InvalidOperationException ("Invalid 'XAMARIN_ASYNCTESTS_OPTIONS' argument.");
				return;
			} else
				throw new InvalidOperationException ("Invalid 'XAMARIN_ASYNCTESTS_OPTIONS' argument.");

			if (args.Count == 2) {
				EndPoint = DependencyInjector.Get<IPortableEndPointSupport> ().ParseEndpoint (args[1]);
			} else if (args.Count == 1) {
				EndPoint = GetEndPoint ();
			} else {
				throw new InvalidOperationException ("Invalid 'XAMARIN_ASYNCTESTS_OPTIONS' argument.");
			}
		}

		static IPortableEndPoint GetEndPoint ()
		{
			var support = DependencyInjector.Get<IPortableEndPointSupport> ();
			return support.GetEndpoint (8888);
		}

		void ParseSettings (string arg)
		{
			var parts = arg.Split (',');
			foreach (var part in parts) {
				var pos = part.IndexOf ('=');
				if (pos > 0) {
					var key = part.Substring (0, pos);
					var value = part.Substring (pos + 1);
					if (key[0] == '-')
						throw new InvalidOperationException ();
					Settings.SetValue (key, value);
				} else if (part[0] == '-') {
					var key = part.Substring (1);
					Settings.RemoveValue (key);
				} else {
					throw new InvalidOperationException ();
				}
			}
		}

		public bool ModifyConfiguration (TestConfiguration config)
		{
			bool modified = false;

			if (Category != null) {
				if (string.Equals (Category, "all", StringComparison.OrdinalIgnoreCase))
					config.CurrentCategory = TestCategory.All;
				else if (string.Equals (Category, "global", StringComparison.OrdinalIgnoreCase))
					config.CurrentCategory = TestCategory.Global;
				else
					config.CurrentCategory = config.Categories.FirstOrDefault (c => c.Name.Equals (Category)) ?? TestCategory.All;

				modified = true;
			}

			if (Features != null) {
				modified = true;
				var parts = Features.Split (',');
				foreach (var part in parts) {
					var name = part;
					bool enable = true;
					if (part[0] == '-') {
						name = part.Substring (1);
						enable = false;
					} else if (part[0] == '+') {
						name = part.Substring (1);
						enable = true;
					}

					if (name.Equals ("all")) {
						foreach (var feature in config.Features) {
							if (feature.CanModify)
								config.SetIsEnabled (feature, enable);
						}
					} else {
						var feature = config.Features.First (f => f.Name.Equals (name));
						config.SetIsEnabled (feature, enable);
					}
				}
			}

			return modified;
		}
	}
}
