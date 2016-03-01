//
// TouchLauncher.cs
//
// Author:
//       Martin Baulig <martin.baulig@xamarin.com>
//
// Copyright (c) 2016 Xamarin Inc. (http://www.xamarin.com)
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
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using System.Collections.Generic;
using System.Reflection;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace Xamarin.AsyncTests.Console
{
	using Remoting;
	using Portable;

	class TouchLauncher : ApplicationLauncher
	{
		public string MonoTouchRoot {
			get;
			private set;
		}

		public string MTouch {
			get;
			private set;
		}

		public string Application {
			get;
			private set;
		}

		public string ExtraMTouchArguments {
			get;
			private set;
		}

		Process process;

		public TouchLauncher (string app, string extraArgs)
		{
			Application = app;
			ExtraMTouchArguments = extraArgs;

			MonoTouchRoot = Environment.GetEnvironmentVariable ("MONOTOUCH_ROOT");
			if (String.IsNullOrEmpty (MonoTouchRoot))
				MonoTouchRoot = "/Library/Frameworks/Xamarin.iOS.framework/Versions/Current";

			
			MTouch = Path.Combine (MonoTouchRoot, "bin", "mtouch");
		}

		Process LaunchSimulator (IPortableEndPoint address)
		{
			var args = new StringBuilder ();
			args.AppendFormat (" --launchsim={0}", Application);
			args.AppendFormat (" --setenv=\"XAMARIN_ASYNCTESTS_OPTIONS=connect {0}:{1}\"", address.Address, address.Port);
			args.AppendFormat (" --stderr=simulator-stderr.txt");
			args.AppendFormat (" --stdout=simulator-stdout.txt");

			if (ExtraMTouchArguments != null) {
				args.Append (" ");
				args.Append (ExtraMTouchArguments);
			}

			Program.Debug ("Launching mtouch: {0} {1}", MTouch, args);

			var psi = new ProcessStartInfo (MTouch, args.ToString ());
			psi.UseShellExecute = false;
			psi.RedirectStandardInput = true;
			var process = Process.Start (psi);

			Program.Debug ("Started: {0}", process);

			return process;
		}

		public override void LaunchApplication (IPortableEndPoint address)
		{
			process = LaunchSimulator (address);
		}

		public override void StopApplication ()
		{
			try {
				if (!process.HasExited)
					process.Kill ();
			} catch {
				;
			}
		}
	}
}

