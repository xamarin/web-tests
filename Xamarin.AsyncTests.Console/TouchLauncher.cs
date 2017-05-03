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
		public Program Program {
			get;
		}

		public ProgramOptions Options => Program.Options;

		public string MonoTouchRoot {
			get;
		}

		public string MLaunch {
			get;
		}

		public string DeviceName {
			get;
		}

		public TouchLauncher (Program program)
		{
			Program = program;

			MonoTouchRoot = ProgramOptions.GetEnvironmentVariable ("MONOTOUCH_ROOT", "/Library/Frameworks/Xamarin.iOS.framework/Versions/Current");

			MLaunch = Path.Combine (MonoTouchRoot, "bin", "mlaunch");

			DeviceName = string.Format (
				":v2;devicetype=com.apple.CoreSimulator.SimDeviceType.{0},runtime=com.apple.CoreSimulator.SimRuntime.{1}",
				Options.IOSDeviceType, Options.IOSRuntime);
		}

		void Install ()
		{
			var args = new StringBuilder ();
			switch (Options.Command) {
			case Command.Device:
				args.AppendFormat (" --installdev={0}", Options.Application);
				break;
			case Command.Simulator:
				args.AppendFormat (" --installsim={0}", Options.Application);
				break;
			case Command.TVOS:
				args.AppendFormat (" --installsim={0}", Options.Application);
				break;
			default:
				throw new NotSupportedException ();
			}

			args.AppendFormat (" --sdkroot={0}", Options.SdkRoot);
			args.AppendFormat ("  --device={0}", DeviceName);

			if (Options.ExtraLauncherArgs != null) {
				args.Append (" ");
				args.Append (Options.ExtraLauncherArgs);
			}

			Program.Debug ("Launching mtouch: {0} {1}", MLaunch, args);

			var psi = new ProcessStartInfo (MLaunch, args.ToString ());
			psi.UseShellExecute = false;
			psi.RedirectStandardInput = true;

			var installProcess = Process.Start (psi);

			Program.Debug ("Started: {0}", installProcess);

			installProcess.WaitForExit ();

			Program.Debug ("Process finished: {0}", installProcess.ExitCode);
			if (installProcess.ExitCode != 0)
				throw new NotSupportedException ();
		}

		public override Task<ExternalProcess> LaunchApplication (string options, CancellationToken cancellationToken)
		{
			var args = new StringBuilder ();
			switch (Options.Command) {
			case Command.Device:
				args.AppendFormat (" --launchdev={0}", Options.Application);
				break;
			case Command.Simulator:
				args.AppendFormat (" --launchsim={0}", Options.Application);
				break;
			case Command.TVOS:
				args.AppendFormat (" --launchsim={0}", Options.Application);
				break;
			default:
				throw new NotSupportedException ();
			}

			args.AppendFormat (" --setenv=\"XAMARIN_ASYNCTESTS_OPTIONS={0}\"", options);
			if (!string.IsNullOrWhiteSpace (Options.StdOut))
				args.AppendFormat (" --stdout={0}", Options.StdOut);
			if (!string.IsNullOrWhiteSpace (Options.StdErr))
				args.AppendFormat (" --stderr={0}", Options.StdErr);
			args.AppendFormat (" --sdkroot={0}", Options.SdkRoot);
			args.AppendFormat (" --device={0}", DeviceName);

			if (Options.ExtraLauncherArgs != null) {
				args.Append (" ");
				args.Append (Options.ExtraLauncherArgs);
			}

			return ProcessHelper.StartCommand (MLaunch, args.ToString (), cancellationToken);
		}
	}
}

